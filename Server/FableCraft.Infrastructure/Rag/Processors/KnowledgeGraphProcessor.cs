using System.Net;

using FableCraft.Infrastructure.Clients;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities;

using Microsoft.EntityFrameworkCore;

using Serilog;

namespace FableCraft.Infrastructure.Rag.Processors;

internal sealed class KnowledgeGraphProcessor : ITextProcessorHandler
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger _logger;
    private readonly IRagBuilder _ragBuilder;

    public KnowledgeGraphProcessor(ApplicationDbContext dbContext, IRagBuilder ragBuilder, ILogger logger)
    {
        _dbContext = dbContext;
        _ragBuilder = ragBuilder;
        _logger = logger;
    }

    public async Task ProcessChunkAsync<TEntity>(Context<TEntity> context, CancellationToken cancellationToken) where TEntity : IKnowledgeGraphEntity
    {
        foreach (var processingContext in context.Chunks)
        {
            foreach (Chunk chunk in processingContext.Chunks.OrderBy(x => x.Order))
            {
                if (chunk.ProcessingStatus == ProcessingStatus.Completed)
                {
                    continue;
                }

                try
                {
                    await SetAsInProgressAsync(chunk, CancellationToken.None);

                    var response = await _ragBuilder.AddDataAsync(chunk.GetContent(), context.AdventureId.ToString(), cancellationToken);

                    var dataId = response.GetDataId();
                    await SetAsProcessedAsync(chunk, dataId, cancellationToken);
                }
                catch (System.Exception ex)
                {
                    _logger.Error(ex,
                        "Failed to process {EntityType} {EntityId}",
                        nameof(Chunk),
                        chunk.Id);
                    await SetAsFailedAsync(chunk, cancellationToken);
                    throw;
                }
            }
        }
    }

    private async Task SetAsInProgressAsync(Chunk chunk, CancellationToken cancellationToken)
    {
        var dbSet = _dbContext.Set<Chunk>();
        try
        {
            await dbSet.Where(e => e.Id == chunk.Id)
                .ExecuteUpdateAsync(
                    x => x.SetProperty(e => e.ProcessingStatus, ProcessingStatus.InProgress),
                    cancellationToken);
        }
        catch (System.Exception ex)
        {
            _logger.Error(ex,
                "Failed to update {EntityType} {EntityId} to InProgress",
                nameof(Chunk),
                chunk.Id);
            throw;
        }
    }

    private async Task SetAsProcessedAsync(
        Chunk chunk,
        string knowledgeGraphNode,
        CancellationToken cancellationToken)
    {
        var dbSet = _dbContext.Set<Chunk>();
        try
        {
            await dbSet.Where(e => e.Id == chunk.Id)
                .ExecuteUpdateAsync(
                    x => x.SetProperty(e => e.ProcessingStatus, ProcessingStatus.Completed)
                        .SetProperty(e => e.KnowledgeGraphNodeId, knowledgeGraphNode),
                    cancellationToken);
        }
        catch (System.Exception ex)
        {
            _logger.Error(ex,
                "Failed to update {EntityType} {EntityId} with knowledge graph node {KnowledgeGraphNode}",
                nameof(Chunk),
                chunk.Id,
                knowledgeGraphNode);
            throw;
        }
    }

    private async Task SetAsFailedAsync(Chunk chunk, CancellationToken cancellationToken)
    {
        var dbSet = _dbContext.Set<Chunk>();
        try
        {
            await dbSet.Where(e => e.Id == chunk.Id)
                .ExecuteUpdateAsync(
                    x => x.SetProperty(e => e.ProcessingStatus, ProcessingStatus.Failed),
                    cancellationToken);
        }
        catch (System.Exception ex)
        {
            _logger.Error(ex,
                "Failed to update {EntityType} {EntityId} as failed",
                nameof(Chunk),
                chunk.Id);
            throw;
        }
    }
}