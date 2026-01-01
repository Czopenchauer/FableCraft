using FableCraft.Application.Model.Worldbook;
using FableCraft.Infrastructure.Persistence;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FableCraft.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LorebookEntryController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;

    public LorebookEntryController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Get all lorebook entries for an adventure
    /// </summary>
    [HttpGet("adventure/{adventureId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<LorebookEntryResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<LorebookEntryResponseDto>>> GetByAdventure(
        Guid adventureId,
        CancellationToken cancellationToken)
    {
        bool adventureExists = await _dbContext.Adventures
            .AnyAsync(a => a.Id == adventureId, cancellationToken);

        if (!adventureExists)
        {
            return NotFound(new { error = "Adventure not found" });
        }

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

        return Ok(entries);
    }

    /// <summary>
    /// Get a single lorebook entry by ID
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
}
