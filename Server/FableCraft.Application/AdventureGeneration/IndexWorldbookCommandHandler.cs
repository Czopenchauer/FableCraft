using FableCraft.Infrastructure;
using FableCraft.Infrastructure.Clients;
using FableCraft.Infrastructure.Docker;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities;
using FableCraft.Infrastructure.Queue;

using Microsoft.EntityFrameworkCore;

using Serilog;

namespace FableCraft.Application.AdventureGeneration;

/// <summary>
/// Command to index a worldbook, creating a reusable template volume.
/// </summary>
public class IndexWorldbookCommand : IMessage
{
    public required Guid AdventureId { get; set; }

    /// <summary>
    /// The worldbook ID to index.
    /// Note: AdventureId from IMessage is unused, but required by interface.
    /// </summary>
    public required Guid WorldbookId { get; init; }
}

/// <summary>
/// Handles worldbook indexing by creating a template knowledge graph volume.
/// </summary>
internal sealed class IndexWorldbookCommandHandler : IMessageHandler<IndexWorldbookCommand>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IWorldbookRagManager _worldbookRagManager;
    private readonly IRagClientFactory _ragClientFactory;
    private readonly IRagChunkService _ragChunkService;
    private readonly ILogger _logger;

    public IndexWorldbookCommandHandler(
        ApplicationDbContext dbContext,
        IWorldbookRagManager worldbookRagManager,
        IRagClientFactory ragClientFactory,
        ILogger logger,
        IRagChunkService ragChunkService)
    {
        _dbContext = dbContext;
        _worldbookRagManager = worldbookRagManager;
        _ragClientFactory = ragClientFactory;
        _logger = logger;
        _ragChunkService = ragChunkService;
    }

    public async Task HandleAsync(IndexWorldbookCommand command, CancellationToken cancellationToken)
    {
        _logger.Information("Starting indexing of worldbook {WorldbookId}", command.WorldbookId);

        var worldbook = await _dbContext.Worldbooks
            .Include(w => w.Lorebooks)
            .Include(w => w.IndexedSnapshots)
            .FirstOrDefaultAsync(w => w.Id == command.WorldbookId, cancellationToken);

        if (worldbook is null)
        {
            _logger.Warning("Worldbook {WorldbookId} not found", command.WorldbookId);
            return;
        }

        worldbook.IndexingStatus = IndexingStatus.Indexing;
        worldbook.IndexingError = null;
        await _dbContext.SaveChangesAsync(cancellationToken);

        var strategy = _dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                await _worldbookRagManager.IndexWorldbook(worldbook.Id, cancellationToken);
                var now = DateTimeOffset.UtcNow;
                var softDeletedLorebooks = worldbook.Lorebooks.Where(l => l.IsDeleted).ToList();
                foreach (var lorebook in softDeletedLorebooks)
                {
                    _dbContext.Lorebooks.Remove(lorebook);
                    worldbook.Lorebooks.Remove(lorebook);
                }

                var activeLorebooks = worldbook.Lorebooks.Where(l => !l.IsDeleted).ToList();

                if (activeLorebooks.Count == 0)
                {
                    _logger.Warning("Worldbook {WorldbookId} has no lorebook entries to index", command.WorldbookId);
                    worldbook.IndexingStatus = IndexingStatus.Indexed;
                    worldbook.IndexingError = null;
                    worldbook.LastIndexedAt = now;
                    await _dbContext.SaveChangesAsync(cancellationToken);
                    return;
                }

                var entries = activeLorebooks
                    .Select(e => new ChunkCreationRequest(
                        e.Id,
                        $"""
                         {e.Title}
                         {e.Content}

                         Category: {e.Category}
                         """,
                        e.ContentType,
                        [RagClientExtensions.GetWorldDatasetName()]))
                    .ToList();

                _logger.Information(
                    "Starting worldbook indexing for {WorldbookId} with {EntryCount} entries",
                    worldbook.Id,
                    entries.Count);

                var chunks = await _ragChunkService.CreateChunk(entries, worldbook.Id, cancellationToken);
                var ragBuilder = await _ragClientFactory.CreateBuildClientForWorldbook(worldbook.Id, cancellationToken);
                var chunksToDelete = await _dbContext.Chunks.Where(x => softDeletedLorebooks.Select(y => y.Id).Contains(x.EntityId)).ToArrayAsync(cancellationToken);
                if (chunksToDelete.Any())
                {
                    _dbContext.Chunks.RemoveRange(chunksToDelete);
                    await _ragChunkService.DeleteNodes(ragBuilder, chunksToDelete, cancellationToken);
                }

                await _ragChunkService.CommitChunksToRagAsync(ragBuilder, chunks, cancellationToken);
                await _ragChunkService.CognifyDatasetsAsync(ragBuilder, [RagClientExtensions.GetWorldDatasetName()], cancellationToken: cancellationToken);

                _dbContext.Chunks.AddRange(chunks);
                _dbContext.LorebookSnapshots.RemoveRange(worldbook.IndexedSnapshots);

                foreach (var lorebook in activeLorebooks)
                {
                    worldbook.IndexedSnapshots.Add(new LorebookSnapshot
                    {
                        WorldbookId = worldbook.Id,
                        LorebookId = lorebook.Id,
                        Title = lorebook.Title,
                        Content = lorebook.Content,
                        Category = lorebook.Category,
                        ContentType = lorebook.ContentType,
                        IndexedAt = now
                    });
                }

                worldbook.IndexingStatus = IndexingStatus.Indexed;
                worldbook.IndexingError = null;
                worldbook.LastIndexedAt = now;
                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                _logger.Information(
                    "Successfully indexed worldbook {WorldbookId} with {ChunkCount} chunks",
                    worldbook.Id,
                    chunks.Count);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to index worldbook {WorldbookId}", worldbook.Id);
                await transaction.RollbackAsync(cancellationToken);
                try
                {
                    await _dbContext.Worldbooks
                        .Where(w => w.Id == command.WorldbookId)
                        .ExecuteUpdateAsync(w => w
                                .SetProperty(x => x.IndexingStatus, IndexingStatus.Failed)
                                .SetProperty(x => x.IndexingError, ex.Message),
                            cancellationToken);
                }
                catch (Exception dbEx)
                {
                    _logger.Warning(dbEx, "Failed to update indexing status for worldbook {WorldbookId}", worldbook.Id);
                }
            }
        });
    }
}