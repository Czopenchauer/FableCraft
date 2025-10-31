using FableCraft.Infrastructure.Clients;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities;
using FableCraft.Infrastructure.Queue;

using Microsoft.EntityFrameworkCore;

using Serilog;

namespace FableCraft.Application.AdventureGeneration;

internal class AddAdventureToKnowledgeGraphCommand : IMessage
{
    public Guid AdventureId { get; init; }
}

internal class AddAdventureToKnowledgeGraphCommandHandler(
    ApplicationDbContext dbContext,
    IRagBuilder ragBuilder,
    ILogger logger)
    : IMessageHandler<AddAdventureToKnowledgeGraphCommand>
{
    public async Task HandleAsync(AddAdventureToKnowledgeGraphCommand message, CancellationToken cancellationToken)
    {
        var adventure = await dbContext.Adventures.Include(x => x.Character).Include(x => x.Lorebook)
            .SingleAsync(x => x.Id == message.AdventureId, cancellationToken: cancellationToken);

        if (adventure.ProcessingStatus is ProcessingStatus.Pending or ProcessingStatus.Failed)
        {
            var universeKnowledgeGraphId = await ragBuilder.AddDataAsync(new AddDataRequest
                {
                    Content = adventure.WorldDescription,
                    EpisodeType = nameof(DataType.Text),
                    Description = "World Description",
                    GroupId = adventure.Id.ToString(),
                    ReferenceTime = DateTime.UtcNow
                },
                cancellationToken);

            await SetAsProcessed<Adventure>(universeKnowledgeGraphId, cancellationToken);
        }

        if (adventure.Character.ProcessingStatus is ProcessingStatus.Pending or ProcessingStatus.Failed)
        {
            var characterKnowledgeGraphId = await ragBuilder.AddDataAsync(new AddDataRequest
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
                },
                cancellationToken);

            await SetAsProcessed<Character>(characterKnowledgeGraphId, cancellationToken);
        }

        var lorebookToProcess = adventure.Lorebook
            .Where(x => x.ProcessingStatus is ProcessingStatus.Pending or ProcessingStatus.Failed);

        foreach (var entry in lorebookToProcess)
        {
            var lorebookEntryKnowledgeGraphId = await ragBuilder.AddDataAsync(new AddDataRequest
                {
                    Content = entry.Content,
                    EpisodeType = nameof(DataType.Text),
                    Description = entry.Description,
                    GroupId = adventure.Id.ToString(),
                    ReferenceTime = DateTime.UtcNow
                },
                cancellationToken);

            await SetAsProcessed<LorebookEntry>(lorebookEntryKnowledgeGraphId, cancellationToken);
        }
    }

    private async Task SetAsProcessed<TEntity>(
        string knowledgeGraphNode,
        CancellationToken cancellationToken)
        where TEntity : class, IKnowledgeGraphEntity
    {
        var dbSet = dbContext.Set<TEntity>();
        try
        {
            await dbSet.ExecuteUpdateAsync(
                x => x.SetProperty(e => e.ProcessingStatus, ProcessingStatus.Completed)
                    .SetProperty(e => e.KnowledgeGraphNodeId, knowledgeGraphNode),
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.Error(ex,
                "Failed to update universe backstory with knowledge graph node {knowledgeGraphNode}",
                knowledgeGraphNode);
            // Normally, there should compensation/ self-healing logic e.g. removing the added data from the knowledge graph, or scheduling message on a retry queue
            // but for simplicity, we just rethrow the exception here.
            throw;
        }
    }

    private async Task SetAsFailed<TEntity>(
        CancellationToken cancellationToken)
        where TEntity : class, IKnowledgeGraphEntity
    {
        var dbSet = dbContext.Set<TEntity>();
        try
        {
            await dbSet.ExecuteUpdateAsync(
                x => x.SetProperty(e => e.ProcessingStatus, ProcessingStatus.Failed),
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.Error(ex,
                "Failed to update entity of type {EntityType} as failed",
                typeof(TEntity).Name);
            throw;
        }
    }
}