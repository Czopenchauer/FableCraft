using FableCraft.Application.Exceptions;
using FableCraft.Application.NarrativeEngine;
using FableCraft.Infrastructure;

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

    [HttpGet("{adventureId:guid}/current-scene")]
    [ProducesResponseType(typeof(GameScene), StatusCodes.Status200OK)]
    public async Task<ActionResult> GetCurrentScene(Guid adventureId, CancellationToken cancellationToken)
    {
        var scene = await _gameService.GetCurrentSceneAsync(adventureId, cancellationToken);
        return Ok(scene);
    }

    [HttpGet("{adventureId:guid}/scene/{sceneId:guid}")]
    [ProducesResponseType(typeof(GameScene), StatusCodes.Status200OK)]
    public async Task<ActionResult> GetScene(Guid adventureId, Guid sceneId, CancellationToken cancellationToken)
    {
        var scene = await _gameService.GetSceneAsync(adventureId, sceneId, cancellationToken);
        return Ok(scene);
    }

    [HttpPost("submit")]
    [ProducesResponseType(typeof(GameScene), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> SubmitAction([FromBody] SubmitActionRequest request, CancellationToken cancellationToken)
    {
        try
        {
            ProcessExecutionContext.AdventureId.Value = request.AdventureId;
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

    [HttpPost("{adventureId:guid}/scenes/{sceneId:guid}/enrich")]
    [ProducesResponseType(typeof(SceneEnrichmentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> EnrichScene(
        Guid adventureId,
        Guid sceneId,
        CancellationToken cancellationToken)
    {
        try
        {
            ProcessExecutionContext.AdventureId.Value = adventureId;
            SceneEnrichmentResult result = await _gameService.EnrichSceneAsync(
                adventureId,
                sceneId,
                cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (AdventureNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpDelete("delete/{adventureId:guid}/scene/{sceneId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteLastScene(Guid adventureId, Guid sceneId, CancellationToken cancellationToken)
    {
        try
        {
            ProcessExecutionContext.AdventureId.Value = adventureId;
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
            ProcessExecutionContext.AdventureId.Value = adventureId;

            GameScene scene = await _gameService.RegenerateAsync(adventureId, sceneId, cancellationToken);
            return Ok(scene);
        }
        catch (AdventureNotFoundException)
        {
            return NotFound();
        }
    }
}