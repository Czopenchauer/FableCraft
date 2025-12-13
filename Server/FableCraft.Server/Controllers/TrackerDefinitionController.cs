using System.Text.Json;

using FableCraft.Application.Model;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;

using FluentValidation;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FableCraft.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TrackerDefinitionController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IValidator<TrackerDefinitionDto> _validator;

    public TrackerDefinitionController(
        ApplicationDbContext dbContext,
        IValidator<TrackerDefinitionDto> validator)
    {
        _dbContext = dbContext;
        _validator = validator;
    }

    /// <summary>
    /// Get all tracker definitions
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<TrackerDefinitionResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<TrackerDefinitionResponseDto>>> GetAll(CancellationToken cancellationToken)
    {
        var definitions = await _dbContext
            .TrackerDefinitions
            .Select(td => new TrackerDefinitionResponseDto
            {
                Id = td.Id,
                Name = td.Name,
                Structure = td.Structure
            })
            .ToListAsync(cancellationToken);

        return Ok(definitions);
    }

    [HttpPost("visualize")]
    [ProducesResponseType(typeof(IEnumerable<TrackerDefinitionResponseDto>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<TrackerDefinitionResponseDto>> VisualizeTracker(TrackerStructure structure, CancellationToken cancellationToken)
    {
        var dictionary = new Dictionary<string, object>();
        var story = ConvertFieldsToDict(structure.Story);
        dictionary.Add(nameof(Tracker.Story), story);

        var mainCharStats = ConvertFieldsToDict(structure.MainCharacter);
        dictionary.Add(nameof(Tracker.MainCharacter), mainCharStats);

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true
        };

        var json = JsonSerializer.Serialize(dictionary, options);
        var tracker = JsonSerializer.Deserialize<Tracker>(json, options);

        return Ok(tracker);

        Dictionary<string, object> ConvertFieldsToDict(params FieldDefinition[] fields)
        {
            var dict = new Dictionary<string, object>();

            foreach (FieldDefinition field in fields)
            {
                if (field is { Type: FieldType.ForEachObject, HasNestedFields: true })
                {
                    dict[field.Name] = new object[] { ConvertFieldsToDict(field.NestedFields) };
                }
                else if (field.DefaultValue != null)
                {
                    dict[field.Name] = field.ExampleValues?.FirstOrDefault() ?? field.DefaultValue;
                }
            }

            return dict;
        }
    }

    /// <summary>
    /// Get a single tracker definition by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TrackerDefinitionResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TrackerDefinitionResponseDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var definition = await _dbContext
            .TrackerDefinitions
            .Where(td => td.Id == id)
            .Select(td => new TrackerDefinitionResponseDto
            {
                Id = td.Id,
                Name = td.Name,
                Structure = td.Structure
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (definition == null)
        {
            return NotFound();
        }

        return Ok(definition);
    }

    /// <summary>
    /// Get the default tracker structure with framework fields pre-populated
    /// </summary>
    [HttpGet("default-structure")]
    [ProducesResponseType(typeof(TrackerStructure), StatusCodes.Status200OK)]
    public ActionResult<TrackerStructure> GetDefaultStructure()
    {
        var defaultStructure = TrackerDefinitionFactory.CreateDefaultStructure();
        return Ok(defaultStructure);
    }

    /// <summary>
    /// Create a new tracker definition
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(TrackerDefinitionResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TrackerDefinitionResponseDto>> Create(
        [FromBody] TrackerDefinitionDto dto,
        CancellationToken cancellationToken)
    {
        // Validate DTO
        var validationResult = await _validator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }

            return ValidationProblem(ModelState);
        }

        // Check for duplicate name
        var nameExists = await _dbContext.TrackerDefinitions
            .AnyAsync(td => td.Name == dto.Name, cancellationToken);

        if (nameExists)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Duplicate tracker definition name",
                Detail = $"A tracker definition with the name '{dto.Name}' already exists."
            });
        }

        // Create entity
        var entity = new TrackerDefinition
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Structure = dto.Structure
        };

        _dbContext.TrackerDefinitions.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var responseDto = new TrackerDefinitionResponseDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Structure = entity.Structure
        };

        return CreatedAtAction(
            nameof(GetById),
            new { id = entity.Id },
            responseDto);
    }

    /// <summary>
    /// Update an existing tracker definition
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] TrackerDefinitionDto dto,
        CancellationToken cancellationToken)
    {
        // Validate DTO
        var validationResult = await _validator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }

            return ValidationProblem(ModelState);
        }

        // Find existing entity
        var entity = await _dbContext.TrackerDefinitions
            .FirstOrDefaultAsync(td => td.Id == id, cancellationToken);

        if (entity == null)
        {
            return NotFound();
        }

        // Check for duplicate name (excluding current entity)
        var nameExists = await _dbContext.TrackerDefinitions
            .AnyAsync(td => td.Name == dto.Name && td.Id != id, cancellationToken);

        if (nameExists)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Duplicate tracker definition name",
                Detail = $"A tracker definition with the name '{dto.Name}' already exists."
            });
        }

        // Update entity
        entity.Name = dto.Name;
        entity.Structure = dto.Structure;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Delete a tracker definition
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.TrackerDefinitions
            .FirstOrDefaultAsync(td => td.Id == id, cancellationToken);

        if (entity == null)
        {
            return NotFound();
        }

        _dbContext.TrackerDefinitions.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }
}