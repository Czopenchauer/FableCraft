using System.Net;

using FableCraft.Infrastructure.Clients;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities;

using Microsoft.EntityFrameworkCore;

using Serilog;

namespace FableCraft.Infrastructure.Rag;

internal sealed class Context<TEntity> where TEntity : IKnowledgeGraphEntity
{
    public required Guid AdventureId { get; init; }

    public required IEnumerable<ProcessingContext<TEntity>> Chunks { get; init; }
}

internal sealed class ProcessingContext<TEntity> where TEntity : IKnowledgeGraphEntity
{
    public required TEntity Entity { get; init; }

    public required List<Chunk> Chunks { get; init; } = new();
}

internal interface ITextProcessorHandler
{
    Task ProcessChunkAsync<TEntity>(Context<TEntity> context, CancellationToken cancellationToken) where TEntity : IKnowledgeGraphEntity;
}

public interface IRagProcessor
{
    Task Add<TEntity>(Guid adventureId, TEntity[] entities, CancellationToken cancellationToken) where TEntity : IKnowledgeGraphEntity;
}

internal sealed class RagProcessor : IRagProcessor
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IEnumerable<ITextProcessorHandler> _handlers;
    private readonly ILogger _logger;
    private readonly IRagBuilder _ragBuilder;

    public RagProcessor(IEnumerable<ITextProcessorHandler> handlers, ApplicationDbContext dbContext, IRagBuilder ragBuilder, ILogger logger)
    {
        _handlers = handlers;
        _dbContext = dbContext;
        _ragBuilder = ragBuilder;
        _logger = logger;
    }

    public async Task Add<TEntity>(Guid adventureId, TEntity[] entities, CancellationToken cancellationToken) where TEntity : IKnowledgeGraphEntity
    {
        var existingChunks = await _dbContext.Set<Chunk>()
            .Where(x => entities.Select(y => y.Id).Contains(x.EntityId))
            .ToListAsync(cancellationToken);

        await ResetFailedChunksAsync(existingChunks, cancellationToken);

        var processingContexts = entities.Select(x => new ProcessingContext<TEntity>
        {
            Entity = x,
            Chunks = existingChunks.Where(y => y.EntityId == x.Id).ToList()
        }).ToArray();

        var context = new Context<TEntity>
        {
            AdventureId = adventureId,
            Chunks = processingContexts
        };
        foreach (ITextProcessorHandler textProcessorHandler in _handlers)
        {
            await textProcessorHandler.ProcessChunkAsync(context, cancellationToken);
        }
    }

    private async Task ResetFailedChunksAsync(List<Chunk> chunks, CancellationToken cancellationToken)
    {
        var failedChunks = chunks.Where(x => x.ProcessingStatus == ProcessingStatus.Failed).ToList();
        if (!failedChunks.Any())
        {
            return;
        }

        var committedChunks = failedChunks
            .Where(x => !string.IsNullOrEmpty(x.KnowledgeGraphNodeId))
            .Select(x => new
            {
                Chunk = x,
                EpisodeId = Task.Run(() => _ragBuilder.GetEpisodeAsync(x.KnowledgeGraphNodeId!.ToString(), cancellationToken),
                    cancellationToken)
            });

        failedChunks.Where(x => string.IsNullOrEmpty(x.KnowledgeGraphNodeId) && x.ProcessingStatus == ProcessingStatus.Failed)
            .ToList()
            .ForEach(x => x.ProcessingStatus = ProcessingStatus.Pending);

        foreach (var chunk in committedChunks)
        {
            try
            {
                EpisodeResponse chunkEpisodeId = await chunk.EpisodeId;
                chunk.Chunk.ProcessingStatus = ProcessingStatus.Completed;
                chunk.Chunk.KnowledgeGraphNodeId = chunkEpisodeId.Uuid;
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                chunk.Chunk.ProcessingStatus = ProcessingStatus.Pending;
            }
        }

        _dbContext.Chunks.UpdateRange(failedChunks);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}