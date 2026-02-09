using FableCraft.Application.AdventureGeneration;
using FableCraft.Application.Model;
using FableCraft.Application.Model.Worldbook;
using FableCraft.Application.Worldbook;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities;
using FableCraft.Infrastructure.Queue;

using FluentValidation;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FableCraft.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WorldbookController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly WorldbookChangeService _changeService;

    public WorldbookController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
        _changeService = new WorldbookChangeService();
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<WorldbookResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<WorldbookResponseDto>>> GetAll(CancellationToken cancellationToken)
    {
        var worldbooks = await _dbContext
            .Worldbooks
            .Include(w => w.Lorebooks)
            .Include(w => w.IndexedSnapshots)
            .Include(w => w.GraphRagSettings)
            .ToListAsync(cancellationToken);

        var results = worldbooks.Select(MapToResponseDto).ToList();

        return Ok(results);
    }

    /// <summary>
    ///     Get a single worldbook by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(WorldbookResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WorldbookResponseDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var worldbook = await _dbContext
            .Worldbooks
            .Include(w => w.Lorebooks)
            .Include(w => w.IndexedSnapshots)
            .Include(w => w.GraphRagSettings)
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

        if (worldbook == null)
        {
            return NotFound();
        }

        return Ok(MapToResponseDto(worldbook));
    }

    [HttpPost]
    [ProducesResponseType(typeof(WorldbookResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<WorldbookResponseDto>> Create(
        [FromBody] WorldbookDto dto,
        [FromServices] IValidator<WorldbookDto> validator,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(dto, cancellationToken);

        if (!validationResult.IsValid)
        {
            return BadRequest(Results.ValidationProblem(validationResult.ToDictionary()));
        }

        var nameExists = await _dbContext.Worldbooks
            .AnyAsync(w => w.Name == dto.Name, cancellationToken);

        if (nameExists)
        {
            return Conflict(new
            {
                error = "Duplicate worldbook name",
                message = $"A worldbook with the name '{dto.Name}' already exists."
            });
        }

        var duplicateTitles = dto.Lorebooks
            .GroupBy(l => l.Title)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateTitles.Any())
        {
            return BadRequest(new
            {
                error = "Duplicate lorebook titles",
                message = $"Lorebook titles must be unique within a worldbook. Duplicates: {string.Join(", ", duplicateTitles)}"
            });
        }

        GraphRagSettings? graphRagSettings = null;
        if (dto.GraphRagSettingsId.HasValue)
        {
            graphRagSettings = await _dbContext.GraphRagSettings
                .FirstOrDefaultAsync(s => s.Id == dto.GraphRagSettingsId.Value, cancellationToken);

            if (graphRagSettings == null)
            {
                return BadRequest(new
                {
                    error = "Invalid GraphRagSettingsId",
                    message = $"GraphRagSettings with ID '{dto.GraphRagSettingsId}' not found."
                });
            }
        }

        var worldbook = new Worldbook
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            GraphRagSettingsId = dto.GraphRagSettingsId,
            Lorebooks = dto.Lorebooks.Select(l => new Lorebook
            {
                Id = Guid.NewGuid(),
                Title = l.Title,
                Content = l.Content,
                Category = l.Category,
                ContentType = l.ContentType
            }).ToList()
        };

        _dbContext.Worldbooks.Add(worldbook);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var response = new WorldbookResponseDto
        {
            Id = worldbook.Id,
            Name = worldbook.Name,
            GraphRagSettingsId = worldbook.GraphRagSettingsId,
            GraphRagSettings = graphRagSettings != null
                ? new GraphRagSettingsSummaryDto
                {
                    Id = graphRagSettings.Id,
                    Name = graphRagSettings.Name,
                    LlmProvider = graphRagSettings.LlmProvider,
                    LlmModel = graphRagSettings.LlmModel,
                    EmbeddingProvider = graphRagSettings.EmbeddingProvider,
                    EmbeddingModel = graphRagSettings.EmbeddingModel
                }
                : null,
            Lorebooks = worldbook.Lorebooks.Select(l => new LorebookResponseDto
            {
                Id = l.Id,
                WorldbookId = l.WorldbookId,
                Title = l.Title,
                Content = l.Content,
                Category = l.Category,
                ContentType = l.ContentType
            }).ToList()
        };

        return CreatedAtAction(nameof(GetById), new { id = worldbook.Id }, response);
    }

    /// <summary>
    ///     Update an existing worldbook
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(WorldbookResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<WorldbookResponseDto>> Update(
        Guid id,
        [FromBody] WorldbookUpdateDto dto,
        [FromServices] IValidator<WorldbookUpdateDto> validator,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(dto, cancellationToken);

        if (!validationResult.IsValid)
        {
            return BadRequest(Results.ValidationProblem(validationResult.ToDictionary()));
        }

        var worldbook = await _dbContext.Worldbooks
            .Include(w => w.Lorebooks)
            .Include(w => w.IndexedSnapshots)
            .Include(w => w.GraphRagSettings)
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

        if (worldbook == null)
        {
            return NotFound();
        }

        var nameExists = await _dbContext.Worldbooks
            .AnyAsync(w => w.Name == dto.Name && w.Id != id, cancellationToken);

        if (nameExists)
        {
            return Conflict(new
            {
                error = "Duplicate worldbook name",
                message = $"A worldbook with the name '{dto.Name}' already exists."
            });
        }

        var duplicateTitles = dto.Lorebooks
            .GroupBy(l => l.Title)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateTitles.Any())
        {
            return BadRequest(new
            {
                error = "Duplicate lorebook titles",
                message = $"Lorebook titles must be unique within a worldbook. Duplicates: {string.Join(", ", duplicateTitles)}"
            });
        }

        GraphRagSettings? graphRagSettings = null;
        if (dto.GraphRagSettingsId.HasValue)
        {
            graphRagSettings = await _dbContext.GraphRagSettings
                .FirstOrDefaultAsync(s => s.Id == dto.GraphRagSettingsId.Value, cancellationToken);

            if (graphRagSettings == null)
            {
                return BadRequest(new
                {
                    error = "Invalid GraphRagSettingsId",
                    message = $"GraphRagSettings with ID '{dto.GraphRagSettingsId}' not found."
                });
            }
        }

        var wasIndexed = worldbook.IndexingStatus == IndexingStatus.Indexed ||
                         worldbook.IndexingStatus == IndexingStatus.NeedsReindexing;

        worldbook.Name = dto.Name;
        worldbook.GraphRagSettingsId = dto.GraphRagSettingsId;

        var updatedLorebookIds = dto.Lorebooks
            .Where(l => l.Id.HasValue)
            .Select(l => l.Id!.Value)
            .ToHashSet();

        var lorebooksToRemove = worldbook.Lorebooks
            .Where(l => !updatedLorebookIds.Contains(l.Id) && !l.IsDeleted)
            .ToList();

        foreach (var lorebook in lorebooksToRemove)
        {
            if (wasIndexed)
            {
                lorebook.IsDeleted = true;
            }
            else
            {
                _dbContext.Lorebooks.Remove(lorebook);
            }
        }

        foreach (var lorebookDto in dto.Lorebooks)
        {
            if (lorebookDto.Id.HasValue)
            {
                var existingLorebook = worldbook.Lorebooks.FirstOrDefault(l => l.Id == lorebookDto.Id.Value);
                if (existingLorebook != null)
                {
                    existingLorebook.Title = lorebookDto.Title;
                    existingLorebook.Content = lorebookDto.Content;
                    existingLorebook.Category = lorebookDto.Category;
                    existingLorebook.ContentType = lorebookDto.ContentType;
                    existingLorebook.IsDeleted = false;
                    _dbContext.Lorebooks.Update(existingLorebook);
                }
            }
            else
            {
                var newLorebook = new Lorebook
                {
                    WorldbookId = worldbook.Id,
                    Title = lorebookDto.Title,
                    Content = lorebookDto.Content,
                    Category = lorebookDto.Category,
                    ContentType = lorebookDto.ContentType
                };
                worldbook.Lorebooks.Add(newLorebook);
            }
        }

        if (wasIndexed && worldbook.IndexingStatus == IndexingStatus.Indexed)
        {
            var hasPendingChanges = _changeService.HasPendingChanges(
                worldbook.Lorebooks,
                worldbook.IndexedSnapshots);

            if (hasPendingChanges)
            {
                worldbook.IndexingStatus = IndexingStatus.NeedsReindexing;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _dbContext.Entry(worldbook).Collection(w => w.Lorebooks).LoadAsync(cancellationToken);

        return Ok(MapToResponseDto(worldbook));
    }

    /// <summary>
    ///     Delete a worldbook (cascades to delete all lorebooks and chunks)
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid id,
        [FromServices] Infrastructure.Docker.IWorldbookRagManager worldbookRagManager,
        CancellationToken cancellationToken)
    {
        var worldbook = await _dbContext.Worldbooks
            .Include(w => w.Lorebooks)
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

        if (worldbook == null)
        {
            return NotFound();
        }

        await worldbookRagManager.DeleteWorldbookVolume(id, cancellationToken);

        var lorebookIds = worldbook.Lorebooks.Select(l => l.Id).ToList();
        if (lorebookIds.Count > 0)
        {
            await _dbContext.Chunks
                .Where(c => lorebookIds.Contains(c.EntityId))
                .ExecuteDeleteAsync(cancellationToken);
        }

        _dbContext.Worldbooks.Remove(worldbook);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    /// <summary>
    ///     Index a worldbook to create a reusable knowledge graph template.
    ///     This is a one-time expensive operation that processes all lorebook entries.
    ///     Adventures created from this worldbook will copy the template for isolated KG.
    /// </summary>
    [HttpPost("{id:guid}/index")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Index(
        Guid id,
        [FromServices] IMessageDispatcher messageDispatcher,
        CancellationToken cancellationToken)
    {
        var worldbook = await _dbContext.Worldbooks
            .Include(w => w.Lorebooks)
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

        if (worldbook == null)
        {
            return NotFound();
        }

        if (worldbook.Lorebooks.Count == 0)
        {
            return BadRequest(new
            {
                error = "Empty worldbook",
                message = "Cannot index a worldbook with no lorebook entries."
            });
        }

        try
        {
            worldbook.IndexingStatus = IndexingStatus.Indexing;
            worldbook.IndexingError = null;
            await _dbContext.SaveChangesAsync(cancellationToken);

            await messageDispatcher.PublishAsync(new IndexWorldbookCommand
                {
                    AdventureId = Guid.Empty,
                    WorldbookId = id
                },
                cancellationToken);
        }
        catch (Exception)
        {
            worldbook.IndexingStatus = IndexingStatus.Failed;
            worldbook.IndexingError = null;
            await _dbContext.SaveChangesAsync(cancellationToken);
            throw;
        }

        return Accepted(new
        {
            worldbookId = id,
            message = "Indexing started. This may take several minutes depending on worldbook size.",
            lorebookCount = worldbook.Lorebooks.Count
        });
    }

    /// <summary>
    ///     Check if a worldbook has been indexed.
    /// </summary>
    [HttpGet("{id:guid}/index/status")]
    [ProducesResponseType(typeof(IndexStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IndexStatusResponse>> GetIndexStatus(
        Guid id,
        CancellationToken cancellationToken)
    {
        var worldbook = await _dbContext.Worldbooks
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

        if (worldbook == null)
        {
            return NotFound();
        }

        return Ok(new IndexStatusResponse
        {
            WorldbookId = id,
            Status = worldbook.IndexingStatus.ToString(),
            Error = worldbook.IndexingError
        });
    }

    /// <summary>
    ///     Get pending changes for a worldbook
    /// </summary>
    [HttpGet("{id:guid}/pending-changes")]
    [ProducesResponseType(typeof(PendingChangesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PendingChangesResponse>> GetPendingChanges(
        Guid id,
        CancellationToken cancellationToken)
    {
        var worldbook = await _dbContext.Worldbooks
            .Include(w => w.Lorebooks)
            .Include(w => w.IndexedSnapshots)
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

        if (worldbook == null)
        {
            return NotFound();
        }

        var snapshotByLorebookId = worldbook.IndexedSnapshots.ToDictionary(s => s.LorebookId);
        var changes = new List<LorebookChangeDto>();

        foreach (var lorebook in worldbook.Lorebooks)
        {
            snapshotByLorebookId.TryGetValue(lorebook.Id, out var snapshot);
            var status = _changeService.GetChangeStatus(lorebook, snapshot);

            if (status != LorebookChangeStatus.None)
            {
                changes.Add(new LorebookChangeDto
                {
                    LorebookId = lorebook.Id,
                    Title = lorebook.Title,
                    ChangeStatus = status
                });
            }
        }

        var summary = _changeService.CalculatePendingChangeSummary(
            worldbook.Lorebooks,
            worldbook.IndexedSnapshots);

        return Ok(new PendingChangesResponse
        {
            WorldbookId = id,
            Changes = changes,
            Summary = summary
        });
    }

    /// <summary>
    ///     Revert all pending changes for a worldbook
    /// </summary>
    [HttpPost("{id:guid}/revert")]
    [ProducesResponseType(typeof(WorldbookResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WorldbookResponseDto>> RevertAllChanges(
        Guid id,
        CancellationToken cancellationToken)
    {
        var worldbook = await _dbContext.Worldbooks
            .Include(w => w.Lorebooks)
            .Include(w => w.IndexedSnapshots)
            .Include(w => w.GraphRagSettings)
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

        if (worldbook == null)
        {
            return NotFound();
        }

        var snapshotByLorebookId = worldbook.IndexedSnapshots.ToDictionary(s => s.LorebookId);

        var lorebooksToRemove = worldbook.Lorebooks
            .Where(l => !snapshotByLorebookId.ContainsKey(l.Id))
            .ToList();
        foreach (var lorebook in lorebooksToRemove)
        {
            _dbContext.Lorebooks.Remove(lorebook);
        }

        foreach (var lorebook in worldbook.Lorebooks)
        {
            if (snapshotByLorebookId.TryGetValue(lorebook.Id, out var snapshot))
            {
                lorebook.Title = snapshot.Title;
                lorebook.Content = snapshot.Content;
                lorebook.Category = snapshot.Category;
                lorebook.ContentType = snapshot.ContentType;
                lorebook.IsDeleted = false;
            }
        }

        if (worldbook.IndexedSnapshots.Count > 0)
        {
            worldbook.IndexingStatus = IndexingStatus.Indexed;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(MapToResponseDto(worldbook));
    }

    /// <summary>
    ///     Revert a single lorebook to its indexed state
    /// </summary>
    [HttpPost("{id:guid}/lorebooks/{lorebookId:guid}/revert")]
    [ProducesResponseType(typeof(LorebookResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LorebookResponseDto>> RevertLorebook(
        Guid id,
        Guid lorebookId,
        CancellationToken cancellationToken)
    {
        var worldbook = await _dbContext.Worldbooks
            .Include(w => w.Lorebooks)
            .Include(w => w.IndexedSnapshots)
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

        if (worldbook == null)
        {
            return NotFound();
        }

        var lorebook = worldbook.Lorebooks.FirstOrDefault(l => l.Id == lorebookId);
        var snapshot = worldbook.IndexedSnapshots.FirstOrDefault(s => s.LorebookId == lorebookId);

        if (lorebook == null && snapshot == null)
        {
            return NotFound();
        }

        if (lorebook != null && snapshot == null)
        {
            _dbContext.Lorebooks.Remove(lorebook);
            await _dbContext.SaveChangesAsync(cancellationToken);

            await UpdateIndexingStatusAfterRevert(worldbook, cancellationToken);

            return Ok(new LorebookResponseDto
            {
                Id = lorebookId,
                WorldbookId = id,
                Title = lorebook.Title,
                Content = lorebook.Content,
                Category = lorebook.Category,
                ContentType = lorebook.ContentType,
                IsDeleted = true,
                ChangeStatus = LorebookChangeStatus.None
            });
        }

        if (lorebook != null && snapshot != null)
        {
            lorebook.Title = snapshot.Title;
            lorebook.Content = snapshot.Content;
            lorebook.Category = snapshot.Category;
            lorebook.ContentType = snapshot.ContentType;
            lorebook.IsDeleted = false;

            await _dbContext.SaveChangesAsync(cancellationToken);

            await UpdateIndexingStatusAfterRevert(worldbook, cancellationToken);

            return Ok(_changeService.ToResponseDto(lorebook, snapshot));
        }

        return NotFound();
    }

    /// <summary>
    ///     Copy a worldbook with optional indexed volume
    /// </summary>
    [HttpPost("{id:guid}/copy")]
    [ProducesResponseType(typeof(WorldbookResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<WorldbookResponseDto>> CopyWorldbook(
        Guid id,
        [FromBody] CopyWorldbookDto dto,
        [FromServices] Infrastructure.Docker.IWorldbookRagManager worldbookRagManager,
        CancellationToken cancellationToken)
    {
        var sourceWorldbook = await _dbContext.Worldbooks
            .Include(w => w.Lorebooks)
            .Include(w => w.IndexedSnapshots)
            .Include(w => w.GraphRagSettings)
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

        if (sourceWorldbook == null)
        {
            return NotFound();
        }

        var nameExists = await _dbContext.Worldbooks
            .AnyAsync(w => w.Name == dto.Name, cancellationToken);

        if (nameExists)
        {
            return Conflict(new
            {
                error = "Duplicate worldbook name",
                message = $"A worldbook with the name '{dto.Name}' already exists."
            });
        }

        var newWorldbook = new Worldbook
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            GraphRagSettingsId = sourceWorldbook.GraphRagSettingsId,
            IndexingStatus = IndexingStatus.NotIndexed
        };

        var lorebookIdMapping = new Dictionary<Guid, Guid>();
        foreach (var lorebook in sourceWorldbook.Lorebooks.Where(l => !l.IsDeleted))
        {
            var newLorebookId = Guid.NewGuid();
            lorebookIdMapping[lorebook.Id] = newLorebookId;

            newWorldbook.Lorebooks.Add(new Lorebook
            {
                Id = newLorebookId,
                WorldbookId = newWorldbook.Id,
                Title = lorebook.Title,
                Content = lorebook.Content,
                Category = lorebook.Category,
                ContentType = lorebook.ContentType,
                IsDeleted = false
            });
        }

        if (dto.CopyIndexedVolume && sourceWorldbook.IndexingStatus == IndexingStatus.Indexed)
        {
            try
            {
                await worldbookRagManager.CopyWorldbookVolume(sourceWorldbook.Id, newWorldbook.Id, cancellationToken);

                foreach (var snapshot in sourceWorldbook.IndexedSnapshots)
                {
                    if (lorebookIdMapping.TryGetValue(snapshot.LorebookId, out var newLorebookId))
                    {
                        newWorldbook.IndexedSnapshots.Add(new LorebookSnapshot
                        {
                            Id = Guid.NewGuid(),
                            WorldbookId = newWorldbook.Id,
                            LorebookId = newLorebookId,
                            Title = snapshot.Title,
                            Content = snapshot.Content,
                            Category = snapshot.Category,
                            ContentType = snapshot.ContentType,
                            IndexedAt = snapshot.IndexedAt
                        });
                    }
                }

                newWorldbook.IndexingStatus = IndexingStatus.Indexed;
                newWorldbook.LastIndexedAt = sourceWorldbook.LastIndexedAt;
            }
            catch (Exception)
            {
                newWorldbook.IndexingStatus = IndexingStatus.NotIndexed;
            }
        }

        _dbContext.Worldbooks.Add(newWorldbook);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = newWorldbook.Id }, MapToResponseDto(newWorldbook));
    }

    /// <summary>
    ///     Get the visualization URL for an indexed worldbook
    /// </summary>
    [HttpGet("{id:guid}/visualization")]
    [ProducesResponseType(typeof(VisualizationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<VisualizationResponse>> GetVisualization(
        Guid id,
        CancellationToken cancellationToken)
    {
        var worldbook = await _dbContext.Worldbooks
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

        if (worldbook == null)
        {
            return NotFound();
        }

        if (worldbook.IndexingStatus != IndexingStatus.Indexed &&
            worldbook.IndexingStatus != IndexingStatus.NeedsReindexing)
        {
            return BadRequest(new
            {
                error = "Worldbook not indexed",
                message = "The worldbook must be indexed before viewing the graph visualization."
            });
        }

        var visualizationUrl = $"/visualization/worldbook-{id}/world/cognify_graph_visualization.html";

        return Ok(new VisualizationResponse
        {
            WorldbookId = id,
            VisualizationUrl = visualizationUrl
        });
    }

    private async Task UpdateIndexingStatusAfterRevert(
        Worldbook worldbook,
        CancellationToken cancellationToken)
    {
        var hasPendingChanges = _changeService.HasPendingChanges(
            worldbook.Lorebooks,
            worldbook.IndexedSnapshots);

        if (!hasPendingChanges && worldbook.IndexingStatus == IndexingStatus.NeedsReindexing)
        {
            worldbook.IndexingStatus = IndexingStatus.Indexed;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private WorldbookResponseDto MapToResponseDto(Worldbook worldbook)
    {
        var snapshotByLorebookId = worldbook.IndexedSnapshots.ToDictionary(s => s.LorebookId);

        var lorebooks = worldbook.Lorebooks
            .Select(l =>
            {
                snapshotByLorebookId.TryGetValue(l.Id, out var snapshot);
                return _changeService.ToResponseDto(l, snapshot);
            })
            .ToList();

        var summary = _changeService.CalculatePendingChangeSummary(
            worldbook.Lorebooks,
            worldbook.IndexedSnapshots);

        var hasPendingChanges = summary.AddedCount > 0 || summary.ModifiedCount > 0 || summary.DeletedCount > 0;

        return new WorldbookResponseDto
        {
            Id = worldbook.Id,
            Name = worldbook.Name,
            GraphRagSettingsId = worldbook.GraphRagSettingsId,
            GraphRagSettings = worldbook.GraphRagSettings != null
                ? new GraphRagSettingsSummaryDto
                {
                    Id = worldbook.GraphRagSettings.Id,
                    Name = worldbook.GraphRagSettings.Name,
                    LlmProvider = worldbook.GraphRagSettings.LlmProvider,
                    LlmModel = worldbook.GraphRagSettings.LlmModel,
                    EmbeddingProvider = worldbook.GraphRagSettings.EmbeddingProvider,
                    EmbeddingModel = worldbook.GraphRagSettings.EmbeddingModel
                }
                : null,
            Lorebooks = lorebooks,
            IndexingStatus = worldbook.IndexingStatus,
            LastIndexedAt = worldbook.LastIndexedAt,
            HasPendingChanges = hasPendingChanges,
            PendingChangeSummary = hasPendingChanges ? summary : null
        };
    }
}

public record IndexStatusResponse
{
    public Guid WorldbookId { get; init; }
    public required string Status { get; init; }
    public string? Error { get; init; }
}

public record PendingChangesResponse
{
    public Guid WorldbookId { get; init; }
    public List<LorebookChangeDto> Changes { get; init; } = new();
    public PendingChangeSummaryDto Summary { get; init; } = null!;
}

public record LorebookChangeDto
{
    public Guid LorebookId { get; init; }
    public string Title { get; init; } = string.Empty;
    public LorebookChangeStatus ChangeStatus { get; init; }
}

public record VisualizationResponse
{
    public Guid WorldbookId { get; init; }
    public required string VisualizationUrl { get; init; }
}