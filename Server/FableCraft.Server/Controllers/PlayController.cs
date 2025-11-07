using FableCraft.Application.Exceptions;
using FableCraft.Application.NarrativeEngine;

using Microsoft.AspNetCore.Mvc;

namespace FableCraft.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PlayController : ControllerBase
{
    private readonly IGameService _gameService;

    public PlayController(IGameService gameService)
    {
        _gameService = gameService;
    }

    [HttpGet("current-scene/{adventureId:guid}")]
    [ProducesResponseType(typeof(GameScene), StatusCodes.Status200OK)]
    public async Task<ActionResult> GetCurrentScene(Guid adventureId, CancellationToken cancellationToken)
    {
        var scene = await _gameService.GetCurrentSceneAsync(adventureId, cancellationToken);
        if (scene == null)
        {
            return NotFound();
        }
        
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
            var scene = await _gameService.SubmitActionAsync(request.AdventureId, request.ActionText, cancellationToken);
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
    [HttpDelete("delete/{adventureId:guid}")]
    public async Task<ActionResult> DeleteLastScene(Guid adventureId, CancellationToken cancellationToken)
    {
        try
        {
            await _gameService.DeleteLastSceneAsync(adventureId, cancellationToken);
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
    [HttpPost("regenerate/{adventureId:guid}")]
    [ProducesResponseType(typeof(GameScene), StatusCodes.Status200OK)]
    public async Task<ActionResult> Regenerate(Guid adventureId, CancellationToken cancellationToken)
    {
        try
        {
            var scene = await _gameService.RegenerateAsync(adventureId, cancellationToken);
            return Ok(scene);
        }
        catch (AdventureNotFoundException)
        {
            return NotFound();
        }
    }
}