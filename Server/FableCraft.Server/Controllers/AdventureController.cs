using FableCraft.Application.AdventureGeneration;
using FableCraft.Application.AdventureImport;
using FableCraft.Application.Exceptions;
using FableCraft.Application.Model;
using FableCraft.Infrastructure.Queue;

using FluentValidation;

using Microsoft.AspNetCore.Mvc;

namespace FableCraft.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdventureController : ControllerBase
{
    private readonly IAdventureCreationService _adventureCreationService;
    private readonly AdventureImportService _adventureImportService;
    private readonly IMessageDispatcher _messageDispatcher;

    public AdventureController(
        IAdventureCreationService adventureCreationService,
        AdventureImportService adventureImportService,
        IMessageDispatcher messageDispatcher)
    {
        _adventureCreationService = adventureCreationService;
        _adventureImportService = adventureImportService;
        _messageDispatcher = messageDispatcher;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<AdventureListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var adventures = await _adventureCreationService.GetAllAdventuresAsync(cancellationToken);

        return Ok(adventures);
    }

    [HttpGet("lorebook")]
    [ProducesResponseType(typeof(AvailableLorebookDto[]), StatusCodes.Status200OK)]
    public IActionResult GetSupportedLorebooks()
    {
        var result = _adventureCreationService.GetSupportedLorebook();

        return Ok(result);
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

    [HttpPost("generate-lorebook")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GenerateLorebook([FromBody] GenerateLorebookDto dto,
        [FromServices] IValidator<GenerateLorebookDto> validator, CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(dto, cancellationToken);

        if (!validationResult.IsValid)
        {
            return BadRequest(Results.ValidationProblem(validationResult.ToDictionary()));
        }

        var result = await _adventureCreationService.GenerateLorebookAsync(
            dto.Lorebooks,
            dto.Category,
            cancellationToken,
            dto.AdditionalInstruction);

        return Ok(new GeneratedLorebookDto
        {
            Content = result
        });
    }

    [HttpPost("retry-knowledge-graph/{adventure:guid}")]
    [ProducesResponseType(typeof(AdventureCreationStatus), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RetryKnowledgeGraphProcessing(Guid adventure, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _adventureCreationService.RetryKnowledgeGraphProcessingAsync(adventure, cancellationToken);

            return RedirectToAction(nameof(GetGenerationStatus), new { adventure = result.AdventureId });
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

    [HttpPost("import")]
    [ProducesResponseType(typeof(AdventureCreationStatus), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [RequestSizeLimit(100_000_000)] // 100MB limit for file uploads
    [RequestFormLimits(MultipartBodyLengthLimit = 100_000_000)]
    public async Task<IActionResult> ImportAdventure(
        [FromForm] IFormFile lorebookFile,
        [FromForm] IFormFile adventureFile,
        [FromForm] IFormFile characterFile,
        [FromForm] string adventureName,
        CancellationToken cancellationToken)
    {
        if (lorebookFile == null || adventureFile == null || characterFile == null)
        {
            return BadRequest(new ValidationProblemDetails
            {
                Errors = new Dictionary<string, string[]>
                {
                    ["files"] = new[] { "All three files (lorebook, adventure, character) must be provided" }
                }
            });
        }

        if (string.IsNullOrWhiteSpace(adventureName))
        {
            return BadRequest(new ValidationProblemDetails
            {
                Errors = new Dictionary<string, string[]>
                {
                    ["adventureName"] = new[] { "Adventure name is required" }
                }
            });
        }

        try
        {
            string lorebookJson;
            using (var reader = new StreamReader(lorebookFile.OpenReadStream()))
            {
                lorebookJson = await reader.ReadToEndAsync(cancellationToken);
            }

            string adventureJson;
            using (var reader = new StreamReader(adventureFile.OpenReadStream()))
            {
                adventureJson = await reader.ReadToEndAsync(cancellationToken);
            }

            string characterJson;
            using (var reader = new StreamReader(characterFile.OpenReadStream()))
            {
                characterJson = await reader.ReadToEndAsync(cancellationToken);
            }

            var adventure = await _adventureImportService.ImportAdventureAsync(
                lorebookJson,
                adventureJson,
                characterJson,
                adventureName,
                cancellationToken);

            return Ok(adventure);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ValidationProblemDetails
            {
                Errors = new Dictionary<string, string[]>
                {
                    ["import"] = new[] { ex.Message }
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "An error occurred while importing the adventure",
                Detail = ex.Message
            });
        }
    }
}