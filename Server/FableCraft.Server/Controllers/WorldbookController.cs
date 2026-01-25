using FableCraft.Application.KnowledgeGraph;
using FableCraft.Application.KnowledgeGraph.Handlers;
using FableCraft.Application.Model.Worldbook;
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

    public WorldbookController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    ///     Get all worldbooks
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
                    Category = l.Category,
                    ContentType = l.ContentType
                }).ToList()
            })
            .ToListAsync(cancellationToken);

        return Ok(worldbooks);
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
                    Category = l.Category,
                    ContentType = l.ContentType
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
    ///     Create a new worldbook
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

        var worldbook = new Worldbook
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
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

        worldbook.Name = dto.Name;

        var updatedLorebookIds = dto.Lorebooks
            .Where(l => l.Id.HasValue)
            .Select(l => l.Id!.Value)
            .ToHashSet();

        var lorebooksToRemove = worldbook.Lorebooks
            .Where(l => !updatedLorebookIds.Contains(l.Id))
            .ToList();

        foreach (var lorebook in lorebooksToRemove)
        {
            _dbContext.Lorebooks.Remove(lorebook);
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
                _dbContext.Worldbooks.Update(worldbook);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _dbContext.Entry(worldbook).Collection(w => w.Lorebooks).LoadAsync(cancellationToken);

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
                Category = l.Category,
                ContentType = l.ContentType
            }).ToList()
        };

        return Ok(response);
    }

    /// <summary>
    ///     Delete a worldbook (cascades to delete all lorebooks)
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

        await messageDispatcher.PublishAsync(new IndexWorldbookCommand
        {
            AdventureId = Guid.Empty,
            WorldbookId = id
        }, cancellationToken);

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
        [FromServices] IKnowledgeGraphContextService contextService,
        CancellationToken cancellationToken)
    {
        var worldbookExists = await _dbContext.Worldbooks.AnyAsync(w => w.Id == id, cancellationToken);
        if (!worldbookExists)
        {
            return NotFound();
        }

        var isIndexed = await contextService.IsWorldbookIndexedAsync(id, cancellationToken);

        return Ok(new IndexStatusResponse
        {
            WorldbookId = id,
            IsIndexed = isIndexed
        });
    }
}

public record IndexStatusResponse
{
    public Guid WorldbookId { get; init; }
    public bool IsIndexed { get; init; }
}