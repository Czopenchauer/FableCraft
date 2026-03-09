using FableCraft.Application.Model.Worldbook;
using FableCraft.Infrastructure;
using FableCraft.Infrastructure.Clients;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FableCraft.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LorebookEntryController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IRagChunkService _ragChunkService;
    private readonly IRagClientFactory _ragClientFactory;

    public LorebookEntryController(
        ApplicationDbContext dbContext,
        IRagChunkService ragChunkService,
        IRagClientFactory ragClientFactory)
    {
        _dbContext = dbContext;
        _ragChunkService = ragChunkService;
        _ragClientFactory = ragClientFactory;
    }

    /// <summary>
    ///     Get all lorebook entries for an adventure including world settings
    /// </summary>
    [HttpGet("adventure/{adventureId:guid}")]
    [ProducesResponseType(typeof(AdventureLoreResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdventureLoreResponseDto>> GetByAdventure(
        Guid adventureId,
        CancellationToken cancellationToken)
    {
        var adventure = await _dbContext.Adventures
            .Where(a => a.Id == adventureId)
            .Select(a => new { a.PromptPath })
            .FirstOrDefaultAsync(cancellationToken);

        if (adventure == null)
        {
            return NotFound(new { error = "Adventure not found" });
        }

        var worldSettingsPath = Path.Combine(adventure.PromptPath, "WorldSettings.md");
        var worldSettings = System.IO.File.Exists(worldSettingsPath) ? await System.IO.File.ReadAllTextAsync(worldSettingsPath, cancellationToken) : null;

        var entries = await _dbContext.LorebookEntries
            .Where(e => e.AdventureId == adventureId)
            .OrderBy(e => e.Category)
            .ThenBy(e => e.Priority)
            .Select(e => new LorebookEntryResponseDto
            {
                Id = e.Id,
                AdventureId = e.AdventureId,
                SceneId = e.SceneId,
                Title = e.Title,
                Description = e.Description,
                Priority = e.Priority,
                Content = e.Content,
                Category = e.Category,
                ContentType = e.ContentType
            })
            .ToListAsync(cancellationToken);

        return Ok(new AdventureLoreResponseDto
        {
            WorldSettings = worldSettings,
            Entries = entries
        });
    }

    /// <summary>
    ///     Get a single lorebook entry by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(LorebookEntryResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LorebookEntryResponseDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var entry = await _dbContext.LorebookEntries
            .Where(e => e.Id == id)
            .Select(e => new LorebookEntryResponseDto
            {
                Id = e.Id,
                AdventureId = e.AdventureId,
                SceneId = e.SceneId,
                Title = e.Title,
                Description = e.Description,
                Priority = e.Priority,
                Content = e.Content,
                Category = e.Category,
                ContentType = e.ContentType
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (entry == null)
        {
            return NotFound();
        }

        return Ok(entry);
    }

    /// <summary>
    ///     Create a new lorebook entry for an adventure
    /// </summary>
    [HttpPost("adventure/{adventureId:guid}")]
    [ProducesResponseType(typeof(LorebookEntryResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LorebookEntryResponseDto>> Create(
        Guid adventureId,
        [FromBody] CreateLorebookEntryDto dto,
        CancellationToken cancellationToken)
    {
        var adventure = await _dbContext.Adventures
            .Where(a => a.Id == adventureId)
            .FirstOrDefaultAsync(cancellationToken);

        if (adventure == null)
        {
            return NotFound(new { error = "Adventure not found" });
        }

        var entry = new LorebookEntry
        {
            Id = Guid.NewGuid(),
            AdventureId = adventureId,
            Title = dto.Title,
            Description = dto.Description ?? dto.Title,
            Content = dto.Content,
            Category = dto.Category,
            Priority = dto.Priority,
            ContentType = dto.ContentType
        };

        _dbContext.LorebookEntries.Add(entry);

        // Index to knowledge graph
        await IndexEntryToKnowledgeGraph(adventureId, entry, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        var responseDto = new LorebookEntryResponseDto
        {
            Id = entry.Id,
            AdventureId = entry.AdventureId,
            SceneId = entry.SceneId,
            Title = entry.Title,
            Description = entry.Description,
            Priority = entry.Priority,
            Content = entry.Content,
            Category = entry.Category,
            ContentType = entry.ContentType
        };

        return CreatedAtAction(nameof(GetById), new { id = entry.Id }, responseDto);
    }

    /// <summary>
    ///     Create multiple lorebook entries for an adventure (bulk import)
    /// </summary>
    [HttpPost("adventure/{adventureId:guid}/bulk")]
    [ProducesResponseType(typeof(List<LorebookEntryResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<LorebookEntryResponseDto>>> BulkCreate(
        Guid adventureId,
        [FromBody] BulkCreateLorebookEntriesDto dto,
        CancellationToken cancellationToken)
    {
        var adventure = await _dbContext.Adventures
            .Where(a => a.Id == adventureId)
            .FirstOrDefaultAsync(cancellationToken);

        if (adventure == null)
        {
            return NotFound(new { error = "Adventure not found" });
        }

        if (dto.Entries == null || dto.Entries.Count == 0)
        {
            return BadRequest(new { error = "No entries provided" });
        }

        var entries = new List<LorebookEntry>();
        foreach (var entryDto in dto.Entries)
        {
            var entry = new LorebookEntry
            {
                Id = Guid.NewGuid(),
                AdventureId = adventureId,
                Title = entryDto.Title,
                Description = entryDto.Description ?? entryDto.Title,
                Content = entryDto.Content,
                Category = entryDto.Category,
                Priority = entryDto.Priority,
                ContentType = entryDto.ContentType
            };
            entries.Add(entry);
        }

        _dbContext.LorebookEntries.AddRange(entries);

        // Index all entries to knowledge graph
        await IndexEntriesToKnowledgeGraph(adventureId, entries, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        var responseDtos = entries.Select(e => new LorebookEntryResponseDto
        {
            Id = e.Id,
            AdventureId = e.AdventureId,
            SceneId = e.SceneId,
            Title = e.Title,
            Description = e.Description,
            Priority = e.Priority,
            Content = e.Content,
            Category = e.Category,
            ContentType = e.ContentType
        }).ToList();

        return CreatedAtAction(nameof(GetByAdventure), new { adventureId }, responseDtos);
    }

    /// <summary>
    ///     Update an existing lorebook entry
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(LorebookEntryResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LorebookEntryResponseDto>> Update(
        Guid id,
        [FromBody] UpdateLorebookEntryDto dto,
        CancellationToken cancellationToken)
    {
        var entry = await _dbContext.LorebookEntries
            .Where(e => e.Id == id)
            .FirstOrDefaultAsync(cancellationToken);

        if (entry == null)
        {
            return NotFound();
        }

        var contentChanged = entry.Content != dto.Content;
        var oldContent = entry.Content;

        // Update the entry
        entry.Title = dto.Title;
        entry.Description = dto.Description ?? dto.Title;
        entry.Content = dto.Content;
        entry.Category = dto.Category;
        entry.Priority = dto.Priority;
        entry.ContentType = dto.ContentType;

        // If content changed, update knowledge graph
        if (contentChanged)
        {
            await UpdateEntryInKnowledgeGraph(entry.AdventureId, entry, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var responseDto = new LorebookEntryResponseDto
        {
            Id = entry.Id,
            AdventureId = entry.AdventureId,
            SceneId = entry.SceneId,
            Title = entry.Title,
            Description = entry.Description,
            Priority = entry.Priority,
            Content = entry.Content,
            Category = entry.Category,
            ContentType = entry.ContentType
        };

        return Ok(responseDto);
    }

    /// <summary>
    ///     Delete a lorebook entry
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var entry = await _dbContext.LorebookEntries
            .Where(e => e.Id == id)
            .FirstOrDefaultAsync(cancellationToken);

        if (entry == null)
        {
            return NotFound();
        }

        // Delete from knowledge graph first
        await DeleteEntryFromKnowledgeGraph(entry.AdventureId, entry.Id, cancellationToken);

        _dbContext.LorebookEntries.Remove(entry);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private async Task IndexEntryToKnowledgeGraph(Guid adventureId, LorebookEntry entry, CancellationToken cancellationToken)
    {
        string[] worldDatasets = [RagClientExtensions.GetWorldDatasetName()];

        var chunkRequest = new ChunkCreationRequest(entry.Id, entry.Content, entry.ContentType, worldDatasets);
        var chunks = await _ragChunkService.CreateChunk([chunkRequest], adventureId, cancellationToken);

        if (chunks.Count > 0)
        {
            var ragBuilder = await _ragClientFactory.CreateBuildClientForAdventure(adventureId, cancellationToken);
            await _ragChunkService.CommitChunksToRagAsync(ragBuilder, chunks, cancellationToken);
            await _ragChunkService.CognifyDatasetsAsync(ragBuilder, worldDatasets, cancellationToken);
            _dbContext.Chunks.AddRange(chunks);
        }
    }

    private async Task IndexEntriesToKnowledgeGraph(Guid adventureId, List<LorebookEntry> entries, CancellationToken cancellationToken)
    {
        if (entries.Count == 0) return;

        string[] worldDatasets = [RagClientExtensions.GetWorldDatasetName()];

        var chunkRequests = entries
            .Select(e => new ChunkCreationRequest(e.Id, e.Content, e.ContentType, worldDatasets))
            .ToList();

        var chunks = await _ragChunkService.CreateChunk(chunkRequests, adventureId, cancellationToken);

        if (chunks.Count > 0)
        {
            var ragBuilder = await _ragClientFactory.CreateBuildClientForAdventure(adventureId, cancellationToken);
            await _ragChunkService.CommitChunksToRagAsync(ragBuilder, chunks, cancellationToken);
            await _ragChunkService.CognifyDatasetsAsync(ragBuilder, worldDatasets, cancellationToken);
            _dbContext.Chunks.AddRange(chunks);
        }
    }

    private async Task UpdateEntryInKnowledgeGraph(Guid adventureId, LorebookEntry entry, CancellationToken cancellationToken)
    {
        // Find and delete old chunk
        var existingChunk = await _dbContext.Chunks
            .Where(c => c.EntityId == entry.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingChunk != null)
        {
            var ragBuilder = await _ragClientFactory.CreateBuildClientForAdventure(adventureId, cancellationToken);
            await _ragChunkService.DeleteNodes(ragBuilder, [existingChunk], cancellationToken);
            _dbContext.Chunks.Remove(existingChunk);
        }

        // Create new chunk with updated content
        await IndexEntryToKnowledgeGraph(adventureId, entry, cancellationToken);
    }

    private async Task DeleteEntryFromKnowledgeGraph(Guid adventureId, Guid entryId, CancellationToken cancellationToken)
    {
        var chunks = await _dbContext.Chunks
            .Where(c => c.EntityId == entryId)
            .ToArrayAsync(cancellationToken);

        if (chunks.Length > 0)
        {
            var ragBuilder = await _ragClientFactory.CreateBuildClientForAdventure(adventureId, cancellationToken);
            await _ragChunkService.DeleteNodes(ragBuilder, chunks, cancellationToken);
            _dbContext.Chunks.RemoveRange(chunks);
        }
    }
}
