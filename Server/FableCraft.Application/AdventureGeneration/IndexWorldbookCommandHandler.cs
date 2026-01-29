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
            .FirstOrDefaultAsync(w => w.Id == command.WorldbookId, cancellationToken);

        if (worldbook is null)
        {
            _logger.Warning("Worldbook {WorldbookId} not found", command.WorldbookId);
            return;
        }

        if (worldbook.Lorebooks.Count == 0)
        {
            _logger.Warning("Worldbook {WorldbookId} has no lorebook entries to index", command.WorldbookId);
            return;
        }

        var entries = worldbook.Lorebooks
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

        worldbook.IndexingStatus = IndexingStatus.Indexing;
        worldbook.IndexingError = null;
        await _dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            await _worldbookRagManager.IndexWorldbook(worldbook.Id, cancellationToken);

            var chunks = await _ragChunkService.CreateChunk(entries, worldbook.Id, cancellationToken);
            var ragBuilder = await _ragClientFactory.CreateBuildClientForWorldbook(worldbook.Id, cancellationToken);
            await _ragChunkService.CommitChunksToRagAsync(ragBuilder, chunks, cancellationToken);
            await _ragChunkService.CognifyDatasetsAsync(ragBuilder, [RagClientExtensions.GetWorldDatasetName()], cancellationToken: cancellationToken);

            worldbook.IndexingStatus = IndexingStatus.Indexed;
            worldbook.IndexingError = null;
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.Information(
                "Successfully indexed worldbook {WorldbookId} with {ChunkCount} chunks",
                worldbook.Id,
                chunks.Count);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to index worldbook {WorldbookId}", worldbook.Id);

            try
            {
                worldbook.IndexingStatus = IndexingStatus.Failed;
                worldbook.IndexingError = ex.Message;
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception dbEx)
            {
                _logger.Warning(dbEx, "Failed to update indexing status for worldbook {WorldbookId}", worldbook.Id);
            }
        }

        _logger.Information("Successfully indexed worldbook {WorldbookId}", command.WorldbookId);
    }
}