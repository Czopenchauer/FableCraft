using FableCraft.Application;
using FableCraft.Application.Validators;

using FluentValidation;

using Microsoft.AspNetCore.Mvc;

namespace FableCraft.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GameController : ControllerBase
{
    private readonly WorldCreationService _worldCreationService;

    public GameController(WorldCreationService worldCreationService)
    {
        _worldCreationService = worldCreationService;
    }

    [HttpPost("create-world")]
    public async Task<ActionResult> SubmitAction([FromBody] WorldDto world, [FromServices] IValidator<WorldDto> validator, CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(world, cancellationToken);

        if (!validationResult.IsValid) 
        {
            return BadRequest(Results.ValidationProblem(validationResult.ToDictionary()));
        }

        return Ok();
    }

    /// <summary>
    ///     Submit a player action
    /// </summary>
    [HttpPost("submit")]
    public async Task<ActionResult> SubmitAction()
    {
        // TODO: Implement action submission logic
        return Ok();
    }

    /// <summary>
    ///     Redo the last action
    /// </summary>
    [HttpDelete("redo")]
    public async Task<ActionResult> Redo()
    {
        // TODO: Implement redo logic
        return Ok();
    }

    /// <summary>
    ///     Regenerate the last response
    /// </summary>
    [HttpPost("regenerate")]
    public async Task<ActionResult> Regenerate()
    {
        // TODO: Implement regenerate logic
        return Ok();
    }
}