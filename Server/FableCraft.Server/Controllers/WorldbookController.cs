using FableCraft.Application.Model.Adventure;
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
public class WorldbookController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;

    public WorldbookController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Get all worldbooks
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<WorldbookResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<WorldbookResponseDto>>> GetAll(CancellationToken cancellationToken)
    {
        var worldbooks = await _dbContext
            .Worldbooks
            .Include(w => w.Lorebooks)
            .Select(w => new WorldbookResponseDto
            {
                Id = w.Id,
                Name = w.Name,
                Lorebooks = w.Lorebooks.Select(l => new LorebookResponseDto
                {
                    Id = l.Id,
                    WorldbookId = l.WorldbookId,
                    Title = l.Title,
                    Content = l.Content,
                    Category = l.Category
                }).ToList()
            })
            .ToListAsync(cancellationToken);

        return Ok(worldbooks);
    }

    /// <summary>
    /// Get a single worldbook by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(WorldbookResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WorldbookResponseDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var worldbook = await _dbContext
            .Worldbooks
            .Include(w => w.Lorebooks)
            .Where(w => w.Id == id)
            .Select(w => new WorldbookResponseDto
            {
                Id = w.Id,
                Name = w.Name,
                Lorebooks = w.Lorebooks.Select(l => new LorebookResponseDto
                {
                    Id = l.Id,
                    WorldbookId = l.WorldbookId,
                    Title = l.Title,
                    Content = l.Content,
                    Category = l.Category
                }).ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (worldbook == null)
        {
            return NotFound();
        }

        return Ok(worldbook);
    }

    /// <summary>
    /// Create a new worldbook
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(WorldbookResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<WorldbookResponseDto>> Create(
        [FromBody] WorldbookDto dto,
        [FromServices] IValidator<WorldbookDto> validator,
        CancellationToken cancellationToken)
    {
        ValidationResult validationResult = await validator.ValidateAsync(dto, cancellationToken);

        if (!validationResult.IsValid)
        {
            return BadRequest(Results.ValidationProblem(validationResult.ToDictionary()));
        }

        // Check for duplicate name
        bool nameExists = await _dbContext.Worldbooks
            .AnyAsync(w => w.Name == dto.Name, cancellationToken);

        if (nameExists)
        {
            return Conflict(new
            {
                error = "Duplicate worldbook name",
                message = $"A worldbook with the name '{dto.Name}' already exists."
            });
        }

        var worldbook = new Worldbook
        {
            Id = Guid.NewGuid(),
            Name = dto.Name
        };

        _dbContext.Worldbooks.Add(worldbook);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var response = new WorldbookResponseDto
        {
            Id = worldbook.Id,
            Name = worldbook.Name,
            Lorebooks = new List<LorebookResponseDto>()
        };

        return CreatedAtAction(nameof(GetById), new { id = worldbook.Id }, response);
    }

    /// <summary>
    /// Update an existing worldbook
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(WorldbookResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<WorldbookResponseDto>> Update(
        Guid id,
        [FromBody] WorldbookDto dto,
        [FromServices] IValidator<WorldbookDto> validator,
        CancellationToken cancellationToken)
    {
        ValidationResult validationResult = await validator.ValidateAsync(dto, cancellationToken);

        if (!validationResult.IsValid)
        {
            return BadRequest(Results.ValidationProblem(validationResult.ToDictionary()));
        }

        var worldbook = await _dbContext.Worldbooks
            .Include(w => w.Lorebooks)
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

        if (worldbook == null)
        {
            return NotFound();
        }

        // Check for duplicate name (excluding current worldbook)
        bool nameExists = await _dbContext.Worldbooks
            .AnyAsync(w => w.Name == dto.Name && w.Id != id, cancellationToken);

        if (nameExists)
        {
            return Conflict(new
            {
                error = "Duplicate worldbook name",
                message = $"A worldbook with the name '{dto.Name}' already exists."
            });
        }

        worldbook.Name = dto.Name;

        await _dbContext.SaveChangesAsync(cancellationToken);

        var response = new WorldbookResponseDto
        {
            Id = worldbook.Id,
            Name = worldbook.Name,
            Lorebooks = worldbook.Lorebooks.Select(l => new LorebookResponseDto
            {
                Id = l.Id,
                WorldbookId = l.WorldbookId,
                Title = l.Title,
                Content = l.Content,
                Category = l.Category
            }).ToList()
        };

        return Ok(response);
    }

    /// <summary>
    /// Delete a worldbook (cascades to delete all lorebooks)
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var worldbook = await _dbContext.Worldbooks.FindAsync([id], cancellationToken);

        if (worldbook == null)
        {
            return NotFound();
        }

        _dbContext.Worldbooks.Remove(worldbook);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }
}

