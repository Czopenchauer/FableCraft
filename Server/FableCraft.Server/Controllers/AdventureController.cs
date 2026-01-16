using FableCraft.Application.AdventureGeneration;
using FableCraft.Application.Exceptions;
using FableCraft.Application.Model.Adventure;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;
using FableCraft.Infrastructure.Queue;

using FluentValidation;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using ILogger = Serilog.ILogger;

namespace FableCraft.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdventureController : ControllerBase
{
    private readonly IAdventureCreationService _adventureCreationService;
    private readonly ApplicationDbContext _dbContext;
    private readonly IMessageDispatcher _messageDispatcher;
    private readonly ILogger  _logger;

    public AdventureController(
        IAdventureCreationService adventureCreationService,
        IMessageDispatcher messageDispatcher,
        ApplicationDbContext dbContext, ILogger logger)
    {
        _adventureCreationService = adventureCreationService;
        _messageDispatcher = messageDispatcher;
        _dbContext = dbContext;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<AdventureListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var adventures = await _adventureCreationService.GetAllAdventuresAsync(cancellationToken);

        return Ok(adventures);
    }

    [HttpPost("create-adventure")]
    [ProducesResponseType(typeof(AdventureCreationStatus), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] AdventureDto adventure,
        [FromServices] IValidator<AdventureDto> validator, CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(adventure, cancellationToken);

        if (!validationResult.IsValid)
        {
            return BadRequest(Results.ValidationProblem(validationResult.ToDictionary()));
        }

        var errors = new Dictionary<string, string[]>();
        foreach (var agentName in Enum.GetValues<AgentName>())
        {
            var exists = System.IO.File.Exists(Path.Combine(
                adventure.PromptPath,
                $"{agentName}.md"));
            if (!exists)
            {
                errors.Add($"{agentName}.md", [$"Prompt file for agent '{agentName}' not found in path '{adventure.PromptPath}'."]);
            }
        }

        if (errors.Count > 0)
        {
            _logger.Error("There were {ErrorsCount} validation errors: {StringsMap}", errors.Count, string.Join("\n", string.Join("\n", errors.Values)));
            return BadRequest(Results.ValidationProblem(errors));
        }

        var result = await _adventureCreationService.CreateAdventureAsync(adventure, cancellationToken);

        return Ok(result);
    }

    [HttpPost("retry-create-adventure/{adventureId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Retry(Guid adventureId, CancellationToken cancellationToken)
    {
        await _messageDispatcher.PublishAsync(new AddAdventureToKnowledgeGraphCommand
            {
                AdventureId = adventureId
            },
            cancellationToken);

        return Ok();
    }

    [HttpGet("status/{adventure:guid}")]
    [ProducesResponseType(typeof(AdventureCreationStatus), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGenerationStatus(Guid adventure, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _adventureCreationService.GetAdventureCreationStatusAsync(adventure, cancellationToken);

            return Ok(result);
        }
        catch (AdventureNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpDelete("{adventure:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAdventure(Guid adventure, CancellationToken cancellationToken)
    {
        try
        {
            await _adventureCreationService.DeleteAdventureAsync(adventure, cancellationToken);

            return NoContent();
        }
        catch (AdventureNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpGet("defaults")]
    [ProducesResponseType(typeof(AdventureDefaultsDto), StatusCodes.Status200OK)]
    public IActionResult GetDefaults()
    {
        var defaultPromptPath = Environment.GetEnvironmentVariable("DEFAULT_PROMPT_PATH") ?? "";

        // Get all agent names from the enum
        var availableAgents = Enum.GetNames<AgentName>();

        return Ok(new AdventureDefaultsDto
        {
            DefaultPromptPath = defaultPromptPath,
            AvailableAgents = availableAgents
        });
    }

    [HttpGet("prompt-directories")]
    [ProducesResponseType(typeof(DirectoryListingDto), StatusCodes.Status200OK)]
    public IActionResult GetPromptDirectories([FromQuery] string? path)
    {
        var basePath = path;
        if (string.IsNullOrEmpty(basePath))
        {
            var defaultPromptPath = Environment.GetEnvironmentVariable("DEFAULT_PROMPT_PATH") ?? "";
            // Trim trailing slashes before getting parent directory
            defaultPromptPath = defaultPromptPath.TrimEnd('/', '\\');
            basePath = Path.GetDirectoryName(defaultPromptPath) ?? "";
        }

        if (string.IsNullOrEmpty(basePath) || !Directory.Exists(basePath))
        {
            return Ok(new DirectoryListingDto
            {
                CurrentPath = basePath ?? "",
                ParentPath = null,
                Directories = []
            });
        }

        try
        {
            var parentPath = Path.GetDirectoryName(basePath);
            var directories = Directory.GetDirectories(basePath)
                .Select(d => new DirectoryEntryDto
                {
                    FullPath = d.Replace('\\', '/'),
                    Name = Path.GetFileName(d)
                })
                .OrderBy(d => d.Name)
                .ToArray();

            return Ok(new DirectoryListingDto
            {
                CurrentPath = basePath.Replace('\\', '/'),
                ParentPath = parentPath?.Replace('\\', '/'),
                Directories = directories
            });
        }
        catch (Exception)
        {
            return Ok(new DirectoryListingDto
            {
                CurrentPath = basePath.Replace('\\', '/'),
                ParentPath = null,
                Directories = []
            });
        }
    }

    [HttpGet("{adventureId:guid}/settings")]
    [ProducesResponseType(typeof(AdventureSettingsResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAdventureSettings(Guid adventureId, CancellationToken cancellationToken)
    {
        var adventure = await _dbContext.Adventures
            .Include(a => a.AgentLlmPresets)
            .ThenInclude(p => p.LlmPreset)
            .FirstOrDefaultAsync(a => a.Id == adventureId, cancellationToken);

        if (adventure == null)
        {
            return NotFound();
        }

        var allAgentNames = Enum.GetValues<AgentName>();
        var agentPresets = allAgentNames.Select(agentName =>
        {
            var existingPreset = adventure.AgentLlmPresets.FirstOrDefault(p => p.AgentName == agentName);
            return new AgentLlmPresetDto
            {
                Id = existingPreset?.Id,
                AgentName = agentName.ToString(),
                LlmPresetId = existingPreset?.LlmPresetId,
                LlmPresetName = existingPreset?.LlmPreset?.Name
            };
        }).ToList();

        var response = new AdventureSettingsResponseDto
        {
            AdventureId = adventure.Id,
            Name = adventure.Name,
            PromptPath = adventure.PromptPath,
            AgentLlmPresets = agentPresets
        };

        return Ok(response);
    }

    [HttpPut("{adventureId:guid}/settings")]
    [ProducesResponseType(typeof(AdventureSettingsResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAdventureSettings(
        Guid adventureId,
        [FromBody] UpdateAdventureSettingsDto dto,
        [FromServices] IValidator<UpdateAdventureSettingsDto> validator,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(dto, cancellationToken);

        if (!validationResult.IsValid)
        {
            return BadRequest(Results.ValidationProblem(validationResult.ToDictionary()));
        }

        var adventure = await _dbContext.Adventures
            .Include(a => a.AgentLlmPresets)
            .ThenInclude(p => p.LlmPreset)
            .FirstOrDefaultAsync(a => a.Id == adventureId, cancellationToken);

        if (adventure == null)
        {
            return NotFound();
        }

        adventure.PromptPath = dto.PromptPath;

        foreach (var presetDto in dto.AgentLlmPresets)
        {
            if (!Enum.TryParse(presetDto.AgentName, out AgentName agentName))
            {
                continue;
            }

            var existingPreset = adventure.AgentLlmPresets.FirstOrDefault(p => p.AgentName == agentName);

            if (presetDto.LlmPresetId.HasValue)
            {
                var presetExists = await _dbContext.LlmPresets.AnyAsync(p => p.Id == presetDto.LlmPresetId.Value, cancellationToken);
                if (!presetExists)
                {
                    return BadRequest(Results.ValidationProblem(new Dictionary<string, string[]>
                    {
                        { presetDto.AgentName, [$"LLM preset with ID '{presetDto.LlmPresetId}' not found."] }
                    }));
                }

                if (existingPreset != null)
                {
                    existingPreset.LlmPresetId = presetDto.LlmPresetId.Value;
                }
                else
                {
                    adventure.AgentLlmPresets.Add(new AdventureAgentLlmPreset
                    {
                        Id = Guid.NewGuid(),
                        AdventureId = adventureId,
                        AgentName = agentName,
                        LlmPresetId = presetDto.LlmPresetId.Value
                    });
                }
            }
            else if (existingPreset != null)
            {
                _dbContext.AdventureAgentLlmPresets.Remove(existingPreset);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _dbContext.Entry(adventure).ReloadAsync(cancellationToken);
        await _dbContext.Entry(adventure).Collection(a => a.AgentLlmPresets).LoadAsync(cancellationToken);
        foreach (var preset in adventure.AgentLlmPresets)
        {
            await _dbContext.Entry(preset).Reference(p => p.LlmPreset).LoadAsync(cancellationToken);
        }

        var allAgentNames = Enum.GetValues<AgentName>();
        var agentPresets = allAgentNames.Select(agentName =>
        {
            var existingPreset = adventure.AgentLlmPresets.FirstOrDefault(p => p.AgentName == agentName);
            return new AgentLlmPresetDto
            {
                Id = existingPreset?.Id,
                AgentName = agentName.ToString(),
                LlmPresetId = existingPreset?.LlmPresetId,
                LlmPresetName = existingPreset?.LlmPreset?.Name
            };
        }).ToList();

        var response = new AdventureSettingsResponseDto
        {
            AdventureId = adventure.Id,
            Name = adventure.Name,
            PromptPath = adventure.PromptPath,
            AgentLlmPresets = agentPresets
        };

        return Ok(response);
    }
}