using FableCraft.Application;
using FableCraft.Application.AdventureGeneration;
using FableCraft.Application.Exceptions;
using FableCraft.Application.Model;

using FluentValidation;

using Microsoft.AspNetCore.Mvc;

namespace FableCraft.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdventureController : ControllerBase
{
    private readonly IAdventureCreationService _adventureCreationService;

    public AdventureController(IAdventureCreationService adventureCreationService)
    {
        _adventureCreationService = adventureCreationService;
    }
    
    [HttpGet("lorebook")]
    [ProducesResponseType(typeof(AdventureCreationStatus), StatusCodes.Status200OK)]
    public IActionResult GetSupportedLorebooks()
    {
        var result = _adventureCreationService.GetSupportedLorebook();

        return Ok(result);
    }

    [HttpPost("create-adventure")]
    [ProducesResponseType(typeof(AdventureCreationStatus), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SubmitAction([FromBody] AdventureDto adventure,
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
    public async Task<IActionResult> GenerateLorebook(string instruction, CancellationToken cancellationToken)
    {
        var result = await _adventureCreationService.GenerateLorebookAsync([], "PhysicalWorld", cancellationToken, instruction);

        return Ok(result);
    }
}