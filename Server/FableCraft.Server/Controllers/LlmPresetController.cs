using FableCraft.Application.Model;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities;

using FluentValidation;
using FluentValidation.Results;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FableCraft.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LlmPresetController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;

    public LlmPresetController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Get all LLM presets
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<LlmPresetResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<LlmPresetResponseDto>>> GetAll(CancellationToken cancellationToken)
    {
        var presets = await _dbContext
            .LlmPresets
            .Select(p => new LlmPresetResponseDto
            {
                Id = p.Id,
                Name = p.Name,
                Provider = p.Provider,
                Model = p.Model,
                BaseUrl = p.BaseUrl,
                ApiKey = p.ApiKey,
                MaxTokens = p.MaxTokens,
                Temperature = p.Temperature,
                TopP = p.TopP,
                TopK = p.TopK,
                FrequencyPenalty = p.FrequencyPenalty,
                PresencePenalty = p.PresencePenalty,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(presets);
    }

    /// <summary>
    /// Get a single LLM preset by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(LlmPresetResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LlmPresetResponseDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var preset = await _dbContext
            .LlmPresets
            .Where(p => p.Id == id)
            .Select(p => new LlmPresetResponseDto
            {
                Id = p.Id,
                Name = p.Name,
                Provider = p.Provider,
                Model = p.Model,
                BaseUrl = p.BaseUrl,
                ApiKey = p.ApiKey,
                MaxTokens = p.MaxTokens,
                Temperature = p.Temperature,
                TopP = p.TopP,
                TopK = p.TopK,
                FrequencyPenalty = p.FrequencyPenalty,
                PresencePenalty = p.PresencePenalty,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (preset == null)
        {
            return NotFound();
        }

        return Ok(preset);
    }

    /// <summary>
    /// Create a new LLM preset
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(LlmPresetResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LlmPresetResponseDto>> Create(
        [FromBody] LlmPresetDto dto,
        [FromServices] IValidator<LlmPresetDto> validator,
        CancellationToken cancellationToken)
    {
        ValidationResult validationResult = await validator.ValidateAsync(dto, cancellationToken);

        if (!validationResult.IsValid)
        {
            return BadRequest(Results.ValidationProblem(validationResult.ToDictionary()));
        }

        var preset = new LlmPreset
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Provider = dto.Provider,
            Model = dto.Model,
            BaseUrl = dto.BaseUrl,
            ApiKey = dto.ApiKey,
            MaxTokens = dto.MaxTokens,
            Temperature = dto.Temperature,
            TopP = dto.TopP,
            TopK = dto.TopK,
            FrequencyPenalty = dto.FrequencyPenalty,
            PresencePenalty = dto.PresencePenalty,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = null
        };

        _dbContext.LlmPresets.Add(preset);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var response = new LlmPresetResponseDto
        {
            Id = preset.Id,
            Name = preset.Name,
            Provider = preset.Provider,
            Model = preset.Model,
            BaseUrl = preset.BaseUrl,
            ApiKey = preset.ApiKey,
            MaxTokens = preset.MaxTokens,
            Temperature = preset.Temperature,
            TopP = preset.TopP,
            TopK = preset.TopK,
            FrequencyPenalty = preset.FrequencyPenalty,
            PresencePenalty = preset.PresencePenalty,
            CreatedAt = preset.CreatedAt,
            UpdatedAt = preset.UpdatedAt
        };

        return CreatedAtAction(nameof(GetById), new { id = preset.Id }, response);
    }

    /// <summary>
    /// Update an existing LLM preset
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(LlmPresetResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LlmPresetResponseDto>> Update(
        Guid id,
        [FromBody] LlmPresetDto dto,
        [FromServices] IValidator<LlmPresetDto> validator,
        CancellationToken cancellationToken)
    {
        ValidationResult validationResult = await validator.ValidateAsync(dto, cancellationToken);

        if (!validationResult.IsValid)
        {
            return BadRequest(Results.ValidationProblem(validationResult.ToDictionary()));
        }

        var preset = await _dbContext.LlmPresets.FindAsync([id], cancellationToken);

        if (preset == null)
        {
            return NotFound();
        }

        preset.Name = dto.Name;
        preset.Provider = dto.Provider;
        preset.Model = dto.Model;
        preset.BaseUrl = dto.BaseUrl;
        preset.ApiKey = dto.ApiKey;
        preset.MaxTokens = dto.MaxTokens;
        preset.Temperature = dto.Temperature;
        preset.TopP = dto.TopP;
        preset.TopK = dto.TopK;
        preset.FrequencyPenalty = dto.FrequencyPenalty;
        preset.PresencePenalty = dto.PresencePenalty;
        preset.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        var response = new LlmPresetResponseDto
        {
            Id = preset.Id,
            Name = preset.Name,
            Provider = preset.Provider,
            Model = preset.Model,
            BaseUrl = preset.BaseUrl,
            ApiKey = preset.ApiKey,
            MaxTokens = preset.MaxTokens,
            Temperature = preset.Temperature,
            TopP = preset.TopP,
            TopK = preset.TopK,
            FrequencyPenalty = preset.FrequencyPenalty,
            PresencePenalty = preset.PresencePenalty,
            CreatedAt = preset.CreatedAt,
            UpdatedAt = preset.UpdatedAt
        };

        return Ok(response);
    }

    /// <summary>
    /// Delete an LLM preset
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var preset = await _dbContext.LlmPresets.FindAsync([id], cancellationToken);

        if (preset == null)
        {
            return NotFound();
        }

        // Check if any adventures are using this preset
        var isUsedByAdventures = await _dbContext.AdventureAgentLlmPresets
            .AnyAsync(a => a.LlmPresetId == id, cancellationToken);

        if (isUsedByAdventures)
        {
            return Conflict(new
            {
                error = "Cannot delete preset",
                message = "This preset is currently being used by one or more adventures. Please update or delete those adventures first."
            });
        }

        _dbContext.LlmPresets.Remove(preset);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }
}
