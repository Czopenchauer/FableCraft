using FableCraft.Application.AdventureGeneration;
using FableCraft.Application.NarrativeEngine;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Queue;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FableCraft.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class MaintenanceController(
    IMessageDispatcher messageDispatcher,
    ApplicationDbContext dbContext,
    ContentGenerationService contentGenerationService,
    WorldInfoExtractionMaintenanceService worldInfoExtractionService,
    CoLocationMaintenanceService coLocationMaintenanceService) : ControllerBase
{
    [HttpPost("resend-scene-generated-messages/{adventureId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResendSceneGeneratedMessages(Guid adventureId)
    {
        var lastScene = dbContext.Scenes.Where(x => x.AdventureId == adventureId).OrderByDescending(x => x.SequenceNumber).FirstOrDefault();
        if (lastScene is null)
        {
            return NotFound();
        }

        await messageDispatcher.PublishAsync(new SceneGeneratedEvent
        {
            AdventureId = adventureId,
            SceneId = lastScene.Id
        });

        return Ok();
    }

    [HttpPost("generate-content/{adventureId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GenerateContent(Guid adventureId, CancellationToken cancellationToken)
    {
        var result = await contentGenerationService.GenerateContentForAdventureAsync(adventureId, cancellationToken);

        if (result is null)
        {
            return NotFound("No scenes found for this adventure or scene has no narrative metadata");
        }

        return Ok(result);
    }

    /// <summary>
    ///     Recovers a corrupted GraphRAG container by recreating its Docker volume from the worldbook template
    ///     and recommitting all adventure data (main character, lorebook, scenes, character rewrites).
    /// </summary>
    /// <param name="adventureId">The ID of the adventure to recover.</param>
    /// <param name="newWorldbookId">Optional: reassign the adventure to a different worldbook before recovery.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("recover-graphrag/{adventureId:guid}")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RecoverGraphRag(
        Guid adventureId,
        [FromQuery] Guid? newWorldbookId = null,
        CancellationToken cancellationToken = default)
    {
        var adventureExists = await dbContext.Adventures.AnyAsync(x => x.Id == adventureId, cancellationToken);
        if (!adventureExists)
        {
            return NotFound("Adventure not found");
        }

        if (newWorldbookId.HasValue)
        {
            var worldbookExists = await dbContext.Worldbooks.AnyAsync(x => x.Id == newWorldbookId.Value, cancellationToken);
            if (!worldbookExists)
            {
                return NotFound("Worldbook not found");
            }
        }

        await messageDispatcher.PublishAsync(new RecoverGraphRagCommand
        {
            AdventureId = adventureId,
            NewWorldbookId = newWorldbookId
        }, cancellationToken);

        return Accepted();
    }

    /// <summary>
    ///     Extracts world info (activities and world facts) for ALL scenes in an adventure.
    ///     Processes all scenes in parallel and links all entries to the last scene.
    /// </summary>
    [HttpPost("extract-world-info/{adventureId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExtractWorldInfo(
        Guid adventureId,
        CancellationToken cancellationToken = default)
    {
        var result = await worldInfoExtractionService.ExtractAllWorldInfoForAdventureAsync(
            adventureId, cancellationToken);

        if (result is null)
        {
            return NotFound("Adventure not found");
        }

        return Ok(result);
    }

    /// <summary>
    ///     Commits Activity lorebooks to the World Knowledge Graph.
    ///     Activities are extracted after scene commit, so they need to be committed separately.
    ///     This operation is idempotent - already committed activities will be skipped.
    /// </summary>
    [HttpPost("commit-activities-to-kg/{adventureId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CommitActivitiesToKnowledgeGraph(
        Guid adventureId,
        CancellationToken cancellationToken = default)
    {
        var result = await worldInfoExtractionService.CommitActivitiesToKnowledgeGraphAsync(
            adventureId, cancellationToken);

        if (result is null)
        {
            return NotFound("Adventure not found");
        }

        return Ok(result);
    }

    /// <summary>
    ///     Populates co-location data for the current (last) scene of an adventure.
    ///     Runs the CoLocationAgent to determine which characters are at the scene location.
    /// </summary>
    [HttpPost("populate-colocation/{adventureId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PopulateCoLocation(
        Guid adventureId,
        CancellationToken cancellationToken = default)
    {
        var result = await coLocationMaintenanceService.PopulateCoLocationForCurrentSceneAsync(
            adventureId, cancellationToken);

        if (result is null)
        {
            return NotFound("No scenes found for this adventure");
        }

        return Ok(result);
    }
}
