using System.Text.Json;

using FableCraft.Application.Exceptions;
using FableCraft.Application.NarrativeEngine;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;
using FableCraft.Server.Models;

using Microsoft.AspNetCore.Mvc;

namespace FableCraft.Server.Controllers;

[ApiController]
[Route("api/[controller]/{adventureId:guid}")]
public class PlayController : ControllerBase
{
    private readonly IGameService _gameService;

    public PlayController(IGameService gameService)
    {
        _gameService = gameService;
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
            var scene = await _gameService.RegenerateAsync(adventureId, cancellationToken);
            return Ok(scene);
        }
        catch (AdventureNotFoundException)
        {
            return NotFound();
        }
    }

    /// <summary>
    ///     Update scene narrative text
    /// </summary>
    [HttpPatch("scene/{sceneId:guid}/narrative")]
    [ProducesResponseType(typeof(GameScene), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> UpdateSceneNarrative(
        Guid adventureId,
        Guid sceneId,
        [FromBody] UpdateSceneNarrativeRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var scene = await _gameService.UpdateSceneNarrativeAsync(
                adventureId, sceneId, request.NarrativeText, cancellationToken);
            return Ok(scene);
        }
        catch (SceneNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    ///     Update scene tracker (time, location, weather, characters present)
    /// </summary>
    [HttpPatch("scene/{sceneId:guid}/scene-tracker")]
    [ProducesResponseType(typeof(GameScene), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> UpdateSceneTracker(
        Guid adventureId,
        Guid sceneId,
        [FromBody] UpdateSceneTrackerRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var additionalProperties = request.AdditionalProperties.HasValue
                ? JsonSerializer.Deserialize<Dictionary<string, object>>(request.AdditionalProperties.Value.GetRawText())
                : new Dictionary<string, object>();

            var tracker = new SceneTracker
            {
                Time = request.Time,
                Location = request.Location,
                Weather = request.Weather,
                CharactersPresent = request.CharactersPresent,
                AdditionalProperties = additionalProperties!
            };

            var scene = await _gameService.UpdateSceneTrackerAsync(
                adventureId, sceneId, tracker, cancellationToken);
            return Ok(scene);
        }
        catch (SceneNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    ///     Update main character tracker and description
    /// </summary>
    [HttpPatch("scene/{sceneId:guid}/main-character")]
    [ProducesResponseType(typeof(GameScene), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> UpdateMainCharacterTracker(
        Guid adventureId,
        Guid sceneId,
        [FromBody] UpdateMainCharacterTrackerRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var tracker = JsonSerializer.Deserialize<MainCharacterTracker>(request.Tracker.GetRawText());
            if (tracker == null)
            {
                return BadRequest(new { error = "Invalid tracker format" });
            }

            var state = new MainCharacterState
            {
                MainCharacter = tracker,
                MainCharacterDescription = request.Description
            };

            var scene = await _gameService.UpdateMainCharacterTrackerAsync(
                adventureId, sceneId, state, cancellationToken);
            return Ok(scene);
        }
        catch (SceneNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (JsonException ex)
        {
            return BadRequest(new { error = $"Invalid JSON format: {ex.Message}" });
        }
    }

    /// <summary>
    ///     Update a specific character's tracker
    /// </summary>
    [HttpPatch("scene/{sceneId:guid}/character/{characterStateId:guid}")]
    [ProducesResponseType(typeof(GameScene), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> UpdateCharacterState(
        Guid adventureId,
        Guid sceneId,
        Guid characterStateId,
        [FromBody] UpdateCharacterStateRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var tracker = JsonSerializer.Deserialize<CharacterTracker>(request.Tracker.GetRawText());
            if (tracker == null)
            {
                return BadRequest(new { error = "Invalid tracker format" });
            }

            var scene = await _gameService.UpdateCharacterStateAsync(
                adventureId, sceneId, characterStateId, tracker, cancellationToken);
            return Ok(scene);
        }
        catch (SceneNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (JsonException ex)
        {
            return BadRequest(new { error = $"Invalid JSON format: {ex.Message}" });
        }
    }
}