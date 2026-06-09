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
    private readonly ManualContentService _manualContentService;

    public PlayController(IGameService gameService, ManualContentService manualContentService)
    {
        _gameService = gameService;
        _manualContentService = manualContentService;
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

    /// <summary>
    ///     Generate a draft of canon content (character, location, item, or lore) without persisting.
    ///     The draft is returned as editable JSON for the user to review and modify before confirming.
    /// </summary>
    [HttpPost("scene/{sceneId:guid}/create-content/draft")]
    [ProducesResponseType(typeof(ManualContentDraftResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ManualContentDraftResult>> DraftContent(
        Guid adventureId,
        Guid sceneId,
        [FromBody] ManualCreateContentRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Details))
        {
            return BadRequest(new { error = "Name and details are required" });
        }

        var input = MapToManualContentInput(request);

        try
        {
            var result = await _manualContentService.DraftAsync(adventureId, input, cancellationToken);
            return Ok(new ManualContentDraftResult(result.Kind, result.Name, result.Summary, result.RawJson));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    ///     Confirm and persist a previously drafted canon content after user review/edit.
    ///     The RawJson contains the (potentially edited) drafted content to be saved.
    /// </summary>
    [HttpPost("scene/{sceneId:guid}/create-content/confirm")]
    [ProducesResponseType(typeof(ManualCreateContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ManualCreateContentResult>> ConfirmContent(
        Guid adventureId,
        Guid sceneId,
        [FromBody] ManualContentConfirmRequest request,
        CancellationToken cancellationToken)
    {
        var input = new ManualContentConfirmInput(
            Kind: request.Type switch
            {
                ManualContentType.Character => ManualContentKind.Character,
                ManualContentType.Location => ManualContentKind.Location,
                ManualContentType.Item => ManualContentKind.Item,
                ManualContentType.Lore => ManualContentKind.Lore,
                _ => throw new ArgumentOutOfRangeException()
            },
            RawJson: request.RawJson);

        try
        {
            var result = await _manualContentService.ConfirmAsync(adventureId, input, cancellationToken);
            return Ok(new ManualCreateContentResult(result.Kind, result.Id, result.Name, result.Summary));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (JsonException ex)
        {
            return BadRequest(new { error = $"Invalid draft JSON: {ex.Message}" });
        }
    }

    /// <summary>
    ///     Manually create canon (character, location, item, or lore) from player-supplied input.
    ///     This is the legacy endpoint that generates and saves in one step.
    ///     Prefer using /draft followed by /confirm for the two-phase flow.
    /// </summary>
    [HttpPost("scene/{sceneId:guid}/create-content")]
    [ProducesResponseType(typeof(ManualCreateContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ManualCreateContentResult>> CreateContent(
        Guid adventureId,
        Guid sceneId,
        [FromBody] ManualCreateContentRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Details))
        {
            return BadRequest(new { error = "Name and details are required" });
        }

        var input = MapToManualContentInput(request);

        try
        {
            var result = await _manualContentService.CreateAsync(adventureId, input, cancellationToken);
            return Ok(new ManualCreateContentResult(result.Kind, result.Id, result.Name, result.Summary));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    private static ManualContentInput MapToManualContentInput(ManualCreateContentRequest request) => new(
        Kind: request.Type switch
        {
            ManualContentType.Character => ManualContentKind.Character,
            ManualContentType.Location => ManualContentKind.Location,
            ManualContentType.Item => ManualContentKind.Item,
            ManualContentType.Lore => ManualContentKind.Lore,
            _ => throw new ArgumentOutOfRangeException()
        },
        Name: request.Name,
        Details: request.Details,
        Importance: request.Importance,
        PowerLevel: request.PowerLevel,
        Category: request.Category);

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

    [HttpDelete("scene")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(424)]
    public async Task<ActionResult> DeleteLastScene(
        Guid adventureId,
        [FromQuery] bool force = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _gameService.DeleteSceneAsync(adventureId, force, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("committed") || ex.Message.Contains("force"))
        {
            return BadRequest(new
            {
                error = "Cannot delete committed scene",
                message = ex.Message,
                hint = "Add ?force=true to the request"
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (HttpRequestException)
        {
            return StatusCode(424, new
            {
                error = "Knowledge graph deletion failed",
                message = "Scene marked for deletion. Retry the request to complete.",
                retryable = true
            });
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