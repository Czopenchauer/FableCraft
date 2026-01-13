using FableCraft.Application.NarrativeEngine;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Queue;

using Microsoft.AspNetCore.Mvc;

namespace FableCraft.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class MaintenanceController(
    IMessageDispatcher messageDispatcher,
    ApplicationDbContext dbContext,
    ContentGenerationService contentGenerationService) : ControllerBase
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
}