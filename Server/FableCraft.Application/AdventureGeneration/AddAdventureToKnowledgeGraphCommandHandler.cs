using System.Net;

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

        await ResetFailedToPendingAsync(adventure, cancellationToken);

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
                TaskId = adventure.Character.Id.ToString(),
                ReferenceTime = DateTime.UtcNow
            }),
            cancellationToken);

        var lorebookToProcess = adventure.Lorebook.OrderBy(x => x.Priority);

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
                        TaskId = entry.Id.ToString(),
                        ReferenceTime = DateTime.UtcNow
                    }),
                cancellationToken);
        }
    }

    private async Task ResetFailedToPendingAsync(Adventure adventure, CancellationToken cancellationToken)
    {
        if (adventure.Character.ProcessingStatus == ProcessingStatus.Failed)
        {
            adventure.Character.ProcessingStatus = ProcessingStatus.Pending;
            logger.Information("Reset Character {CharacterId} from Failed to Pending", adventure.Character.Id);
        }

        adventure.Lorebook
            .Where(x => x.ProcessingStatus == ProcessingStatus.Failed)
            .ToList()
            .ForEach(x => x.ProcessingStatus = ProcessingStatus.Pending);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task ProcessEntityAsync<TEntity>(
        TEntity entity,
        Func<Task<AddDataResponse>> addDataAction,
        CancellationToken cancellationToken)
        where TEntity : class, IKnowledgeGraphEntity, IEntity
    {
        if (entity.ProcessingStatus == ProcessingStatus.Completed)
        {
            return;
        }

        if (entity.ProcessingStatus == ProcessingStatus.InProgress)
        {
            try
            {
                var knowledgeGraphId = await WaitForTaskCompletionAsync(entity.Id.ToString(), cancellationToken);

                await SetAsProcessed(entity, knowledgeGraphId, cancellationToken);
                logger.Information("Successfully added {EntityType} {EntityId} to knowledge graph with ID {KnowledgeGraphId}",
                    typeof(TEntity).Name,
                    entity.Id,
                    knowledgeGraphId);
            }
            catch (HttpRequestException e) when (e.StatusCode == HttpStatusCode.NotFound)
            {
                return;
            }
            catch (Exception e)
            {
                logger.Error(e,
                    "Failed to resume processing of {EntityType} {EntityId} which was InProgress",
                    typeof(TEntity).Name,
                    entity.Id);
                await SetAsFailed(entity, cancellationToken);
                throw;
            }
        }

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
            var response = await pipeline.ExecuteAsync(async token => await addDataAction());
            logger.Information("Task {TaskId} queued for {EntityType} {EntityId}", response.TaskId, typeof(TEntity).Name, entity.Id);

            await SetAsInProgress(entity, cancellationToken);

            var knowledgeGraphId = await WaitForTaskCompletionAsync(response.TaskId, cancellationToken);

            await SetAsProcessed(entity, knowledgeGraphId, cancellationToken);
            logger.Information("Successfully added {EntityType} {EntityId} to knowledge graph with ID {KnowledgeGraphId}",
                typeof(TEntity).Name,
                entity.Id,
                knowledgeGraphId);
        }
        catch (Exception ex)
        {
            logger.Error(ex,
                "Failed to add {EntityType} {EntityId} to knowledge graph after {MaxRetryAttempts} attempts",
                typeof(TEntity).Name,
                entity.Id,
                MaxRetryAttempts);
            await SetAsFailed(entity, cancellationToken);
        }
    }

    private async Task<string> WaitForTaskCompletionAsync(string taskId, CancellationToken cancellationToken)
    {
        const int maxPollingAttempts = 60;
        const int pollingIntervalSeconds = 5;

        for (int attempt = 0; attempt < maxPollingAttempts; attempt++)
        {
            var status = await ragBuilder.GetTaskStatusAsync(taskId, cancellationToken);

            switch (status.Status)
            {
                case Infrastructure.Clients.TaskStatus.Completed:
                    logger.Information("Task {TaskId} completed successfully", taskId);
                    return status.EpisodeId;

                case Infrastructure.Clients.TaskStatus.Failed:
                    throw new InvalidOperationException($"Task {taskId} failed: {status.Error}");

                case Infrastructure.Clients.TaskStatus.Pending:
                case Infrastructure.Clients.TaskStatus.Processing:
                    logger.Debug("Task {TaskId} is {Status}, waiting...", taskId, status.Status);
                    await Task.Delay(TimeSpan.FromSeconds(pollingIntervalSeconds), cancellationToken);
                    break;
            }
        }

        throw new TimeoutException($"Task {taskId} did not complete within the expected time");
    }

    private async Task SetAsInProgress<TEntity>(
        TEntity entity,
        CancellationToken cancellationToken)
        where TEntity : class, IKnowledgeGraphEntity, IEntity
    {
        var dbSet = dbContext.Set<TEntity>();
        try
        {
            await dbSet.Where(e => e.Id == entity.Id)
                .ExecuteUpdateAsync(
                    x => x.SetProperty(e => e.ProcessingStatus, ProcessingStatus.InProgress),
                    cancellationToken);
            logger.Information("Set {EntityType} {EntityId} to InProgress", typeof(TEntity).Name, entity.Id);
        }
        catch (Exception ex)
        {
            logger.Error(ex,
                "Failed to update {EntityType} {EntityId} to InProgress",
                typeof(TEntity).Name,
                entity.Id);
            throw;
        }
    }

    private async Task SetAsProcessed<TEntity>(
        TEntity entity,
        string knowledgeGraphNode,
        CancellationToken cancellationToken)
        where TEntity : class, IKnowledgeGraphEntity, IEntity
    {
        var dbSet = dbContext.Set<TEntity>();
        try
        {
            await dbSet.Where(e => e.Id == entity.Id)
                .ExecuteUpdateAsync(
                    x => x.SetProperty(e => e.ProcessingStatus, ProcessingStatus.Completed)
                        .SetProperty(e => e.KnowledgeGraphNodeId, knowledgeGraphNode),
                    cancellationToken);
        }
        catch (Exception ex)
        {
            logger.Error(ex,
                "Failed to update {EntityType} {EntityId} with knowledge graph node {KnowledgeGraphNode}",
                typeof(TEntity).Name,
                entity.Id,
                knowledgeGraphNode);
            throw;
        }
    }

    private async Task SetAsFailed<TEntity>(
        TEntity entity,
        CancellationToken cancellationToken)
        where TEntity : class, IKnowledgeGraphEntity, IEntity
    {
        var dbSet = dbContext.Set<TEntity>();
        try
        {
            await dbSet.Where(e => e.Id == entity.Id)
                .ExecuteUpdateAsync(
                    x => x.SetProperty(e => e.ProcessingStatus, ProcessingStatus.Failed),
                    cancellationToken);
        }
        catch (Exception ex)
        {
            logger.Error(ex,
                "Failed to update {EntityType} {EntityId} as failed",
                typeof(TEntity).Name,
                entity.Id);
            throw;
        }
    }
}