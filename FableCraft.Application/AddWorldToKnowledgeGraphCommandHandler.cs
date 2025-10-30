using FableCraft.Infrastructure.Clients;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities;
using FableCraft.Infrastructure.Queue;

using Microsoft.EntityFrameworkCore;

using Serilog;

namespace FableCraft.Application;

internal class AddWorldToKnowledgeGraphCommand : IMessage
{
    public Guid WorldId { get; init; }
}

internal class AddWorldToKnowledgeGraphCommandHandler(
    ApplicationDbContext dbContext,
    IRagBuilder ragBuilder,
    ILogger logger)
    : IMessageHandler<AddWorldToKnowledgeGraphCommand>
{
    public async Task HandleAsync(AddWorldToKnowledgeGraphCommand message, CancellationToken cancellationToken)
    {
        var world = await dbContext.Worlds.Include(x => x.Character).Include(x => x.Lorebook)
            .SingleAsync(x => x.WorldId == message.WorldId, cancellationToken: cancellationToken);

        if (world.ProcessingStatus != ProcessingStatus.Completed)
        {
            var universeId = await ragBuilder.AddDataAsync(new AddDataRequest
            {
                Content = world.UniverseBackstory,
                EpisodeType = nameof(DataType.Text),
                Description = "Universe Backstory",
                GroupId = world.WorldId.ToString(),
                ReferenceTime = DateTime.UtcNow
            }, cancellationToken);

            await SetAsProcessed(world, universeId, cancellationToken);
        }
    }

    private async Task SetAsProcessed<TEntity>(TEntity entity, string knowledgeGraphNode,
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
            logger.Error(ex, "Failed to update universe backstory with knowledge graph node {knowledgeGraphNode}",
                knowledgeGraphNode);
            // Normally, there should compensation/ self-healing logic e.g. removing the added data from the knowledge graph, or scheduling message on a retry queue
            // but for simplicity, we just rethrow the exception here.
            throw;
        }
    }
}