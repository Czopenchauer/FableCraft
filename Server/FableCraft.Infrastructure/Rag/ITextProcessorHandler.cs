using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities;

using Microsoft.EntityFrameworkCore;

namespace FableCraft.Infrastructure.Rag;

public class ProcessingOptions
{
    public int MaxChunkSize { get; set; } = 128;
}

internal sealed class Context<TEntity> where TEntity : IKnowledgeGraphEntity
{
    public required Guid AdventureId { get; init; }

    public required ProcessingOptions ProcessingOptions { get; set; }

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
    Task Add<TEntity>(Guid adventureId, TEntity[] entities, CancellationToken cancellationToken, ProcessingOptions? options = null) where TEntity : IKnowledgeGraphEntity;
}

internal sealed class RagProcessor : IRagProcessor
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IEnumerable<ITextProcessorHandler> _handlers;

    public RagProcessor(IEnumerable<ITextProcessorHandler> handlers, ApplicationDbContext dbContext)
    {
        _handlers = handlers;
        _dbContext = dbContext;
    }

    public async Task Add<TEntity>(Guid adventureId, TEntity[] entities, CancellationToken cancellationToken, ProcessingOptions? options = null)
        where TEntity : IKnowledgeGraphEntity
    {
        await AddInternal(adventureId, entities, options, _handlers, cancellationToken);
    }

    private async Task AddInternal<TEntity>(
        Guid adventureId, TEntity[] entities,
        ProcessingOptions? options,
        IEnumerable<ITextProcessorHandler> handlers,
        CancellationToken cancellationToken) where TEntity : IKnowledgeGraphEntity
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
            Chunks = processingContexts,
            ProcessingOptions = options ?? new ProcessingOptions()
        };
        foreach (ITextProcessorHandler textProcessorHandler in handlers)
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

        failedChunks.ForEach(x => x.ProcessingStatus = ProcessingStatus.Pending);
        _dbContext.Chunks.UpdateRange(failedChunks);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}