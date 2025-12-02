using FableCraft.Application.Exceptions;
using FableCraft.Application.NarrativeEngine;

using Microsoft.AspNetCore.Mvc;

namespace FableCraft.Server.Controllers;

public record PageRequest(int Take, int? Skip);

[ApiController]
[Route("api/[controller]")]
public class PlayController : ControllerBase
{
    private readonly IGameService _gameService;

    public PlayController(IGameService gameService)
    {
        _gameService = gameService;
    }

    [HttpGet("{adventureId:guid}")]
    [ProducesResponseType(typeof(GameScene[]), StatusCodes.Status200OK)]
    public async Task<ActionResult> GetCurrentScene(Guid adventureId, [FromQuery] PageRequest pageRequest, CancellationToken cancellationToken)
    {
        var scene = await _gameService.GetScenesAsync(adventureId, pageRequest.Take, pageRequest.Skip, cancellationToken);
        return Ok(scene);
    }

    /// <summary>
    ///     Submit a player action
    /// </summary>
    [HttpPost("submit")]
    [ProducesResponseType(typeof(GameScene), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> SubmitAction([FromBody] SubmitActionRequest request, CancellationToken cancellationToken)
    {
        try
        {
            GameScene scene = await _gameService.SubmitActionAsync(request.AdventureId, request.ActionText, cancellationToken);
            return Ok(scene);
        }
        catch (AdventureNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    ///     Delete the last scene
    /// </summary>
    [HttpDelete("delete/{adventureId:guid}/scene/{sceneId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteLastScene(Guid adventureId, Guid sceneId, CancellationToken cancellationToken)
    {
        try
        {
            await _gameService.DeleteSceneAsync(adventureId, sceneId, cancellationToken);
            return NoContent();
        }
        catch (AdventureNotFoundException)
        {
            return NotFound();
        }
    }

    /// <summary>
    ///     Regenerate the last scene
    /// </summary>
    [HttpPost("regenerate/{adventureId:guid}/scene/{sceneId:guid}")]
    [ProducesResponseType(typeof(GameScene), StatusCodes.Status200OK)]
    public async Task<ActionResult> Regenerate(Guid adventureId, Guid sceneId, CancellationToken cancellationToken)
    {
        try
        {
            GameScene scene = await _gameService.RegenerateAsync(adventureId, sceneId, cancellationToken);
            return Ok(scene);
        }
        catch (AdventureNotFoundException)
        {
            return NotFound();
        }
    }
}