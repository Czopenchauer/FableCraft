using FableCraft.Infrastructure.Clients;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities;
using FableCraft.Infrastructure.Queue;

using Microsoft.EntityFrameworkCore;

using Polly;
using Polly.Retry;

using Serilog;

namespace FableCraft.Application.AdventureGeneration;

public class AddAdventureToKnowledgeGraphCommand : IMessage
{
    public Guid AdventureId { get; init; }
}

internal class AddAdventureToKnowledgeGraphCommandHandler(
    ApplicationDbContext dbContext,
    IRagBuilder ragBuilder,
    ILogger logger)
    : IMessageHandler<AddAdventureToKnowledgeGraphCommand>
{
    private const int MaxRetryAttempts = 3;

    public async Task HandleAsync(AddAdventureToKnowledgeGraphCommand message, CancellationToken cancellationToken)
    {
        var adventure = await dbContext.Adventures.Include(x => x.Character).Include(x => x.Lorebook)
            .SingleAsync(x => x.Id == message.AdventureId, cancellationToken: cancellationToken);

        // Reset all failed entities to pending before processing
        await ResetFailedToPendingAsync(adventure, cancellationToken);

        if (adventure.Character.ProcessingStatus is ProcessingStatus.Pending or ProcessingStatus.Failed)
        {
            await ProcessEntityAsync(
                adventure.Character,
                async () => await ragBuilder.AddDataAsync(new AddDataRequest
                {
                    // TODO: structure character data in a more detailed way
                    Content = $"""
                               Character name: {adventure.Character.Name}
                               Character description: {adventure.Character.Description}
                               Character background: {adventure.Character.Background}
                               """,
                    EpisodeType = nameof(DataType.Text),
                    Description = "Main Character description",
                    GroupId = adventure.Id.ToString(),
                    ReferenceTime = DateTime.UtcNow
                }));
        }

        var lorebookToProcess = adventure.Lorebook
            .Where(x => x.ProcessingStatus is ProcessingStatus.Pending or ProcessingStatus.Failed);

        foreach (var entry in lorebookToProcess)
        {
            await ProcessEntityAsync(
                entry,
                async () => await ragBuilder.AddDataAsync(new AddDataRequest
                {
                    Content = entry.Content,
                    EpisodeType = nameof(DataType.Text),
                    Description = entry.Description,
                    GroupId = adventure.Id.ToString(),
                    ReferenceTime = DateTime.UtcNow
                }));
        }
    }

    private async Task ResetFailedToPendingAsync(Adventure adventure, CancellationToken cancellationToken)
    {
        // Reset character if failed
        if (adventure.Character.ProcessingStatus == ProcessingStatus.Failed)
        {
            await dbContext.Characters
                .Where(c => c.Id == adventure.Character.Id)
                .ExecuteUpdateAsync(
                    x => x.SetProperty(e => e.ProcessingStatus, ProcessingStatus.Pending),
                    cancellationToken);
            logger.Information("Reset Character {CharacterId} from Failed to Pending", adventure.Character.Id);
        }

        // Reset failed lorebook entries
        var failedLorebookIds = adventure.Lorebook
            .Where(x => x.ProcessingStatus == ProcessingStatus.Failed)
            .Select(x => x.Id)
            .ToList();

        if (failedLorebookIds.Any())
        {
            await dbContext.LorebookEntries
                .Where(l => failedLorebookIds.Contains(l.Id))
                .ExecuteUpdateAsync(
                    x => x.SetProperty(e => e.ProcessingStatus, ProcessingStatus.Pending),
                    cancellationToken);
            logger.Information("Reset {Count} Lorebook entries from Failed to Pending", failedLorebookIds.Count);
        }
    }

    private async Task ProcessEntityAsync<TEntity>(
        TEntity entity,
        Func<Task<string>> addDataAction)
    where TEntity : class, IKnowledgeGraphEntity, IEntity
    {
        var pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder()
                    .Handle<HttpRequestException>()
                    .Handle<TaskCanceledException>()
                    .Handle<TimeoutException>(),
                MaxRetryAttempts = MaxRetryAttempts,
                Delay = TimeSpan.FromSeconds(2),
                BackoffType = DelayBackoffType.Exponential,
                OnRetry = args =>
                {
                    logger.Warning(
                        "Retry {AttemptNumber} of {MaxRetryAttempts} for {EntityType} {EntityId} due to {ExceptionType}: {ExceptionMessage}",
                        args.AttemptNumber + 1,
                        MaxRetryAttempts,
                        typeof(TEntity).Name,
                        entity.Id,
                        args.Outcome.Exception?.GetType().Name,
                        args.Outcome.Exception?.Message);
                    return ValueTask.CompletedTask;
                }
            })
            .Build();

        try
        {
            var knowledgeGraphId = await pipeline.ExecuteAsync(async token => await addDataAction());
            await SetAsProcessed(entity, knowledgeGraphId);
            logger.Information("Successfully added {EntityType} {EntityId} to knowledge graph with ID {KnowledgeGraphId}",
                typeof(TEntity).Name, entity.Id, knowledgeGraphId);
        }
        catch (Exception ex)
        {
            logger.Error(ex,
                "Failed to add {EntityType} {EntityId} to knowledge graph after {MaxRetryAttempts} attempts",
                typeof(TEntity).Name, entity.Id, MaxRetryAttempts);
            await SetAsFailed(entity);
        }
    }

    private async Task SetAsProcessed<TEntity>(
        TEntity entity,
        string knowledgeGraphNode)
        where TEntity : class, IKnowledgeGraphEntity, IEntity
    {
        var dbSet = dbContext.Set<TEntity>();
        try
        {
            await dbSet.Where(e => e.Id == entity.Id)
                .ExecuteUpdateAsync(
                    x => x.SetProperty(e => e.ProcessingStatus, ProcessingStatus.Completed)
                        .SetProperty(e => e.KnowledgeGraphNodeId, knowledgeGraphNode));
        }
        catch (Exception ex)
        {
            logger.Error(ex,
                "Failed to update {EntityType} {EntityId} with knowledge graph node {KnowledgeGraphNode}",
                typeof(TEntity).Name, entity.Id, knowledgeGraphNode);
            throw;
        }
    }

    private async Task SetAsFailed<TEntity>(
        TEntity entity)
        where TEntity : class, IKnowledgeGraphEntity, IEntity
    {
        var dbSet = dbContext.Set<TEntity>();
        try
        {
            await dbSet.Where(e => e.Id == entity.Id)
                .ExecuteUpdateAsync(
                    x => x.SetProperty(e => e.ProcessingStatus, ProcessingStatus.Failed));
        }
        catch (Exception ex)
        {
            logger.Error(ex,
                "Failed to update {EntityType} {EntityId} as failed",
                typeof(TEntity).Name, entity.Id);
            throw;
        }
    }
}