using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Queue;

using Microsoft.EntityFrameworkCore;

using Serilog;

namespace FableCraft.Application.KnowledgeGraph.Handlers;

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
    private readonly IKnowledgeGraphContextService _contextService;
    private readonly ILogger _logger;

    public IndexWorldbookCommandHandler(
        ApplicationDbContext dbContext,
        IKnowledgeGraphContextService contextService,
        ILogger logger)
    {
        _dbContext = dbContext;
        _contextService = contextService;
        _logger = logger;
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
            .Select(l => new LorebookIndexEntry(l.Id, l.Content, l.ContentType.ToString()))
            .ToList();

        var result = await _contextService.IndexWorldbookAsync(
            command.WorldbookId,
            entries,
            cancellationToken);

        if (!result.Success)
        {
            _logger.Error("Failed to index worldbook {WorldbookId}: {Error}",
                command.WorldbookId, result.Error);
            throw new InvalidOperationException($"Worldbook indexing failed: {result.Error}");
        }

        _logger.Information("Successfully indexed worldbook {WorldbookId}", command.WorldbookId);
    }
}
