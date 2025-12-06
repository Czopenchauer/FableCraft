using FableCraft.Application.Model.Worldbook;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities;

using FluentValidation;
using FluentValidation.Results;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FableCraft.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LorebookController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;

    public LorebookController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Get all lorebooks for a worldbook
    /// </summary>
    [HttpGet("worldbook/{worldbookId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<LorebookResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<LorebookResponseDto>>> GetByWorldbook(
        Guid worldbookId,
        CancellationToken cancellationToken)
    {
        bool worldbookExists = await _dbContext.Worldbooks
            .AnyAsync(w => w.Id == worldbookId, cancellationToken);

        if (!worldbookExists)
        {
            return NotFound(new { error = "Worldbook not found" });
        }

        var lorebooks = await _dbContext
            .Lorebooks
            .Where(l => l.WorldbookId == worldbookId)
            .Select(l => new LorebookResponseDto
            {
                Id = l.Id,
                WorldbookId = l.WorldbookId,
                Title = l.Title,
                Content = l.Content,
                Category = l.Category
            })
            .ToListAsync(cancellationToken);

        return Ok(lorebooks);
    }

    /// <summary>
    /// Get a single lorebook by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(LorebookResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LorebookResponseDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var lorebook = await _dbContext
            .Lorebooks
            .Where(l => l.Id == id)
            .Select(l => new LorebookResponseDto
            {
                Id = l.Id,
                WorldbookId = l.WorldbookId,
                Title = l.Title,
                Content = l.Content,
                Category = l.Category
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (lorebook == null)
        {
            return NotFound();
        }

        return Ok(lorebook);
    }

    /// <summary>
    /// Create a new lorebook
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(LorebookResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<LorebookResponseDto>> Create(
        [FromBody] LorebookDto dto,
        [FromServices] IValidator<LorebookDto> validator,
        CancellationToken cancellationToken)
    {
        ValidationResult validationResult = await validator.ValidateAsync(dto, cancellationToken);

        if (!validationResult.IsValid)
        {
            return BadRequest(Results.ValidationProblem(validationResult.ToDictionary()));
        }

        // Check if worldbook exists
        bool worldbookExists = await _dbContext.Worldbooks
            .AnyAsync(w => w.Id == dto.WorldbookId, cancellationToken);

        if (!worldbookExists)
        {
            return NotFound(new { error = "Worldbook not found" });
        }

        // Check for duplicate title within worldbook
        bool titleExists = await _dbContext.Lorebooks
            .AnyAsync(l => l.WorldbookId == dto.WorldbookId && l.Title == dto.Title, cancellationToken);

        if (titleExists)
        {
            return Conflict(new
            {
                error = "Duplicate lorebook title",
                message = $"A lorebook with the title '{dto.Title}' already exists in this worldbook."
            });
        }

        var lorebook = new Lorebook
        {
            Id = Guid.NewGuid(),
            WorldbookId = dto.WorldbookId,
            Title = dto.Title,
            Content = dto.Content,
            Category = dto.Category
        };

        _dbContext.Lorebooks.Add(lorebook);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var response = new LorebookResponseDto
        {
            Id = lorebook.Id,
            WorldbookId = lorebook.WorldbookId,
            Title = lorebook.Title,
            Content = lorebook.Content,
            Category = lorebook.Category
        };

        return CreatedAtAction(nameof(GetById), new { id = lorebook.Id }, response);
    }

    /// <summary>
    /// Update an existing lorebook
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(LorebookResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<LorebookResponseDto>> Update(
        Guid id,
        [FromBody] LorebookDto dto,
        [FromServices] IValidator<LorebookDto> validator,
        CancellationToken cancellationToken)
    {
        ValidationResult validationResult = await validator.ValidateAsync(dto, cancellationToken);

        if (!validationResult.IsValid)
        {
            return BadRequest(Results.ValidationProblem(validationResult.ToDictionary()));
        }

        var lorebook = await _dbContext.Lorebooks.FindAsync([id], cancellationToken);

        if (lorebook == null)
        {
            return NotFound();
        }

        // Check if worldbook exists (in case WorldbookId is being changed)
        if (lorebook.WorldbookId != dto.WorldbookId)
        {
            bool worldbookExists = await _dbContext.Worldbooks
                .AnyAsync(w => w.Id == dto.WorldbookId, cancellationToken);

            if (!worldbookExists)
            {
                return NotFound(new { error = "Worldbook not found" });
            }
        }

        // Check for duplicate title within worldbook (excluding current lorebook)
        bool titleExists = await _dbContext.Lorebooks
            .AnyAsync(l => l.WorldbookId == dto.WorldbookId && l.Title == dto.Title && l.Id != id, cancellationToken);

        if (titleExists)
        {
            return Conflict(new
            {
                error = "Duplicate lorebook title",
                message = $"A lorebook with the title '{dto.Title}' already exists in this worldbook."
            });
        }

        lorebook.WorldbookId = dto.WorldbookId;
        lorebook.Title = dto.Title;
        lorebook.Content = dto.Content;
        lorebook.Category = dto.Category;

        await _dbContext.SaveChangesAsync(cancellationToken);

        var response = new LorebookResponseDto
        {
            Id = lorebook.Id,
            WorldbookId = lorebook.WorldbookId,
            Title = lorebook.Title,
            Content = lorebook.Content,
            Category = lorebook.Category
        };

        return Ok(response);
    }

    /// <summary>
    /// Delete a lorebook
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var lorebook = await _dbContext.Lorebooks.FindAsync([id], cancellationToken);

        if (lorebook == null)
        {
            return NotFound();
        }

        _dbContext.Lorebooks.Remove(lorebook);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }
}

