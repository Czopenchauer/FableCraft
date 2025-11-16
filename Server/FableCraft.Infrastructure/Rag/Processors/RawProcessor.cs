using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities;

namespace FableCraft.Infrastructure.Rag.Processors;

internal sealed class RawProcessor : ITextProcessorHandler
{
    private readonly ApplicationDbContext _dbContext;

    public RawProcessor(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task ProcessChunkAsync<TEntity>(Context<TEntity> context, CancellationToken cancellationToken) where TEntity : IKnowledgeGraphEntity
    {
        var chunks = context.Chunks.Where(x => x.Chunks.Count == 0).Select((ctx, idx) =>
        {
            var content = ctx.Entity.GetContent();
            var chunk = new Chunk
            {
                RawChunk = content.Text,
                EntityId = ctx.Entity.Id,
                ProcessingStatus = ProcessingStatus.Pending,
                Description = content
                    .Description,
                Order = idx,
                ContentType = content
                    .ContentType,
                ReferenceTime = content.ReferenceTime
            };
            ctx.Chunks.Add(chunk);
            return chunk;
        }).ToList();

        if (chunks.Count == 0)
        {
            return;
        }

        await _dbContext.Chunks.AddRangeAsync(chunks, cancellationToken);
        await _dbContext.SaveChangesAsync(CancellationToken.None);
    }
}