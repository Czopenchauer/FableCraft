using FableCraft.Application.Model;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities;

using FluentValidation;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using ILogger = Serilog.ILogger;

namespace FableCraft.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GraphRagSettingsController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger _logger;

    public GraphRagSettingsController(ApplicationDbContext dbContext, ILogger logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    ///     Get all GraphRAG settings
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<GraphRagSettingsResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<GraphRagSettingsResponseDto>>> GetAll(CancellationToken cancellationToken)
    {
        var settings = await _dbContext
            .GraphRagSettings
            .Select(s => new GraphRagSettingsResponseDto
            {
                Id = s.Id,
                Name = s.Name,
                LlmProvider = s.LlmProvider,
                LlmModel = s.LlmModel,
                LlmEndpoint = s.LlmEndpoint,
                LlmApiKey = s.LlmApiKey,
                LlmApiVersion = s.LlmApiVersion,
                LlmMaxTokens = s.LlmMaxTokens,
                LlmRateLimitEnabled = s.LlmRateLimitEnabled,
                LlmRateLimitRequests = s.LlmRateLimitRequests,
                LlmRateLimitInterval = s.LlmRateLimitInterval,
                EmbeddingProvider = s.EmbeddingProvider,
                EmbeddingModel = s.EmbeddingModel,
                EmbeddingEndpoint = s.EmbeddingEndpoint,
                EmbeddingApiKey = s.EmbeddingApiKey,
                EmbeddingApiVersion = s.EmbeddingApiVersion,
                EmbeddingDimensions = s.EmbeddingDimensions,
                EmbeddingMaxTokens = s.EmbeddingMaxTokens,
                EmbeddingBatchSize = s.EmbeddingBatchSize,
                HuggingfaceTokenizer = s.HuggingfaceTokenizer,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(settings);
    }

    /// <summary>
    ///     Get all GraphRAG settings (summary only)
    /// </summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(IEnumerable<GraphRagSettingsSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<GraphRagSettingsSummaryDto>>> GetAllSummary(CancellationToken cancellationToken)
    {
        var settings = await _dbContext
            .GraphRagSettings
            .Select(s => new GraphRagSettingsSummaryDto
            {
                Id = s.Id,
                Name = s.Name,
                LlmProvider = s.LlmProvider,
                LlmModel = s.LlmModel,
                EmbeddingProvider = s.EmbeddingProvider,
                EmbeddingModel = s.EmbeddingModel
            })
            .ToListAsync(cancellationToken);

        return Ok(settings);
    }

    /// <summary>
    ///     Get a single GraphRAG settings by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(GraphRagSettingsResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GraphRagSettingsResponseDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var settings = await _dbContext
            .GraphRagSettings
            .Where(s => s.Id == id)
            .Select(s => new GraphRagSettingsResponseDto
            {
                Id = s.Id,
                Name = s.Name,
                LlmProvider = s.LlmProvider,
                LlmModel = s.LlmModel,
                LlmEndpoint = s.LlmEndpoint,
                LlmApiKey = s.LlmApiKey,
                LlmApiVersion = s.LlmApiVersion,
                LlmMaxTokens = s.LlmMaxTokens,
                LlmRateLimitEnabled = s.LlmRateLimitEnabled,
                LlmRateLimitRequests = s.LlmRateLimitRequests,
                LlmRateLimitInterval = s.LlmRateLimitInterval,
                EmbeddingProvider = s.EmbeddingProvider,
                EmbeddingModel = s.EmbeddingModel,
                EmbeddingEndpoint = s.EmbeddingEndpoint,
                EmbeddingApiKey = s.EmbeddingApiKey,
                EmbeddingApiVersion = s.EmbeddingApiVersion,
                EmbeddingDimensions = s.EmbeddingDimensions,
                EmbeddingMaxTokens = s.EmbeddingMaxTokens,
                EmbeddingBatchSize = s.EmbeddingBatchSize,
                HuggingfaceTokenizer = s.HuggingfaceTokenizer,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (settings == null)
        {
            return NotFound();
        }

        return Ok(settings);
    }

    /// <summary>
    ///     Create new GraphRAG settings
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(GraphRagSettingsResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GraphRagSettingsResponseDto>> Create(
        [FromBody] GraphRagSettingsDto dto,
        [FromServices] IValidator<GraphRagSettingsDto> validator,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(dto, cancellationToken);

        if (!validationResult.IsValid)
        {
            return BadRequest(Results.ValidationProblem(validationResult.ToDictionary()));
        }

        var settings = new GraphRagSettings
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            LlmProvider = dto.LlmProvider,
            LlmModel = dto.LlmModel,
            LlmEndpoint = dto.LlmEndpoint,
            LlmApiKey = dto.LlmApiKey,
            LlmApiVersion = dto.LlmApiVersion,
            LlmMaxTokens = dto.LlmMaxTokens,
            LlmRateLimitEnabled = dto.LlmRateLimitEnabled,
            LlmRateLimitRequests = dto.LlmRateLimitRequests,
            LlmRateLimitInterval = dto.LlmRateLimitInterval,
            EmbeddingProvider = dto.EmbeddingProvider,
            EmbeddingModel = dto.EmbeddingModel,
            EmbeddingEndpoint = dto.EmbeddingEndpoint,
            EmbeddingApiKey = dto.EmbeddingApiKey,
            EmbeddingApiVersion = dto.EmbeddingApiVersion,
            EmbeddingDimensions = dto.EmbeddingDimensions,
            EmbeddingMaxTokens = dto.EmbeddingMaxTokens,
            EmbeddingBatchSize = dto.EmbeddingBatchSize,
            HuggingfaceTokenizer = dto.HuggingfaceTokenizer,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = null
        };

        _dbContext.GraphRagSettings.Add(settings);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var response = MapToResponse(settings);

        return CreatedAtAction(nameof(GetById), new { id = settings.Id }, response);
    }

    /// <summary>
    ///     Update existing GraphRAG settings
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(GraphRagSettingsResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GraphRagSettingsResponseDto>> Update(
        Guid id,
        [FromBody] GraphRagSettingsDto dto,
        [FromServices] IValidator<GraphRagSettingsDto> validator,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(dto, cancellationToken);

        if (!validationResult.IsValid)
        {
            return BadRequest(Results.ValidationProblem(validationResult.ToDictionary()));
        }

        var settings = await _dbContext.GraphRagSettings.FindAsync([id], cancellationToken);

        if (settings == null)
        {
            return NotFound();
        }

        settings.Name = dto.Name;
        settings.LlmProvider = dto.LlmProvider;
        settings.LlmModel = dto.LlmModel;
        settings.LlmEndpoint = dto.LlmEndpoint;
        settings.LlmApiKey = dto.LlmApiKey;
        settings.LlmApiVersion = dto.LlmApiVersion;
        settings.LlmMaxTokens = dto.LlmMaxTokens;
        settings.LlmRateLimitEnabled = dto.LlmRateLimitEnabled;
        settings.LlmRateLimitRequests = dto.LlmRateLimitRequests;
        settings.LlmRateLimitInterval = dto.LlmRateLimitInterval;
        settings.EmbeddingProvider = dto.EmbeddingProvider;
        settings.EmbeddingModel = dto.EmbeddingModel;
        settings.EmbeddingEndpoint = dto.EmbeddingEndpoint;
        settings.EmbeddingApiKey = dto.EmbeddingApiKey;
        settings.EmbeddingApiVersion = dto.EmbeddingApiVersion;
        settings.EmbeddingDimensions = dto.EmbeddingDimensions;
        settings.EmbeddingMaxTokens = dto.EmbeddingMaxTokens;
        settings.EmbeddingBatchSize = dto.EmbeddingBatchSize;
        settings.HuggingfaceTokenizer = dto.HuggingfaceTokenizer;
        settings.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(MapToResponse(settings));
    }

    /// <summary>
    ///     Delete GraphRAG settings
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var settings = await _dbContext.GraphRagSettings.FindAsync([id], cancellationToken);

        if (settings == null)
        {
            return NotFound();
        }

        // Check if any worldbooks or adventures are using this settings
        var usedByWorldbooks = await _dbContext.Worldbooks
            .Where(w => w.GraphRagSettingsId == id)
            .Select(w => w.Name)
            .ToListAsync(cancellationToken);

        var usedByAdventures = await _dbContext.Adventures
            .Where(a => a.GraphRagSettingsId == id)
            .Select(a => a.Name)
            .ToListAsync(cancellationToken);

        if (usedByWorldbooks.Count > 0 || usedByAdventures.Count > 0)
        {
            return Conflict(new
            {
                error = "Cannot delete settings",
                message = "This settings profile is currently in use.",
                worldbooks = usedByWorldbooks,
                adventures = usedByAdventures
            });
        }

        _dbContext.GraphRagSettings.Remove(settings);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private static GraphRagSettingsResponseDto MapToResponse(GraphRagSettings settings) =>
        new()
        {
            Id = settings.Id,
            Name = settings.Name,
            LlmProvider = settings.LlmProvider,
            LlmModel = settings.LlmModel,
            LlmEndpoint = settings.LlmEndpoint,
            LlmApiKey = settings.LlmApiKey,
            LlmApiVersion = settings.LlmApiVersion,
            LlmMaxTokens = settings.LlmMaxTokens,
            LlmRateLimitEnabled = settings.LlmRateLimitEnabled,
            LlmRateLimitRequests = settings.LlmRateLimitRequests,
            LlmRateLimitInterval = settings.LlmRateLimitInterval,
            EmbeddingProvider = settings.EmbeddingProvider,
            EmbeddingModel = settings.EmbeddingModel,
            EmbeddingEndpoint = settings.EmbeddingEndpoint,
            EmbeddingApiKey = settings.EmbeddingApiKey,
            EmbeddingApiVersion = settings.EmbeddingApiVersion,
            EmbeddingDimensions = settings.EmbeddingDimensions,
            EmbeddingMaxTokens = settings.EmbeddingMaxTokens,
            EmbeddingBatchSize = settings.EmbeddingBatchSize,
            HuggingfaceTokenizer = settings.HuggingfaceTokenizer,
            CreatedAt = settings.CreatedAt,
            UpdatedAt = settings.UpdatedAt
        };
}
