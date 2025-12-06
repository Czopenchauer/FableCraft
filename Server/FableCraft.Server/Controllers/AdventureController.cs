using FableCraft.Application.AdventureGeneration;
using FableCraft.Application.Exceptions;
using FableCraft.Application.Model.Adventure;
using FableCraft.Infrastructure.Queue;

using FluentValidation;
using FluentValidation.Results;

using Microsoft.AspNetCore.Mvc;

namespace FableCraft.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdventureController : ControllerBase
{
    private readonly IAdventureCreationService _adventureCreationService;
    private readonly IMessageDispatcher _messageDispatcher;

    public AdventureController(
        IAdventureCreationService adventureCreationService,
        IMessageDispatcher messageDispatcher)
    {
        _adventureCreationService = adventureCreationService;
        _messageDispatcher = messageDispatcher;
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
        ValidationResult? validationResult = await validator.ValidateAsync(adventure, cancellationToken);

        if (!validationResult.IsValid)
        {
            return BadRequest(Results.ValidationProblem(validationResult.ToDictionary()));
        }

        AdventureCreationStatus result = await _adventureCreationService.CreateAdventureAsync(adventure, cancellationToken);

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
            AdventureCreationStatus result = await _adventureCreationService.GetAdventureCreationStatusAsync(adventure, cancellationToken);

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
}