using FableCraft.Application.Exceptions;
using FableCraft.Application.NarrativeEngine;
using FableCraft.Application.NarrativeEngine.Agents;
using FableCraft.Infrastructure;

using Microsoft.AspNetCore.Mvc;

namespace FableCraft.Server.Controllers;

[ApiController]
[Route("api/[controller]/{adventureId:guid}")]
public class PlayController : ControllerBase
{
    private readonly IGameService _gameService;
    private readonly MainCharacterEmulatorAgent _mainCharacterEmulatorAgent;

    public PlayController(IGameService gameService, MainCharacterEmulatorAgent mainCharacterEmulatorAgent)
    {
        _gameService = gameService;
        _mainCharacterEmulatorAgent = mainCharacterEmulatorAgent;
    }

    [HttpGet("current-scene")]
    [ProducesResponseType(typeof(GameScene), StatusCodes.Status200OK)]
    public async Task<ActionResult> GetCurrentScene(Guid adventureId, CancellationToken cancellationToken)
    {
        var scene = await _gameService.GetCurrentSceneAsync(adventureId, cancellationToken);
        return Ok(scene);
    }

    [HttpGet("scene/{sceneId:guid}")]
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

    [HttpPost("scene/{sceneId:guid}/enrich")]
    [ProducesResponseType(typeof(SceneEnrichmentOutput), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> EnrichScene(
        Guid adventureId,
        Guid sceneId,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _gameService.EnrichSceneAsync(
                adventureId,
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

    [HttpPost("scene/{sceneId:guid}/enrich/regenerate")]
    [ProducesResponseType(typeof(SceneEnrichmentOutput), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> RegenerateSceneEnrichment(
        Guid adventureId,
        Guid sceneId,
        [FromBody] RegenerateEnrichmentRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _gameService.RegenerateEnrichmentAsync(
                adventureId,
                sceneId,
                request.AgentsToRegenerate,
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

    [HttpDelete("scene/{sceneId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteLastScene(Guid adventureId, Guid sceneId, CancellationToken cancellationToken)
    {
        try
        {
            await _gameService.DeleteSceneAsync(adventureId, cancellationToken);
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
    [HttpPost("scene/{sceneId:guid}/regenerate")]
    [ProducesResponseType(typeof(GameScene), StatusCodes.Status200OK)]
    public async Task<ActionResult> Regenerate(Guid adventureId, Guid sceneId, CancellationToken cancellationToken)
    {
        try
        {
            GameScene scene = await _gameService.RegenerateAsync(adventureId, cancellationToken);
            return Ok(scene);
        }
        catch (AdventureNotFoundException)
        {
            return NotFound();
        }
    }

    /// <summary>
    ///     Emulate the main character to generate text from their perspective
    /// </summary>
    [HttpPost("emulate-character")]
    [ProducesResponseType(typeof(MainCharacterEmulationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MainCharacterEmulationResponse>> EmulateMainCharacter(
        Guid adventureId,
        [FromBody] EmulateMainCharacterRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mainCharacterEmulatorAgent.InvokeAsync(
                new MainCharacterEmulationRequest
                {
                    AdventureId = adventureId,
                    Instruction = request.Instruction
                },
                cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}

public class EmulateMainCharacterRequest
{
    public required string Instruction { get; init; }
}