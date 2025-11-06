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
            await _gameService.RegenerateAsync(adventureId, cancellationToken);
            return Accepted();
        }
        catch (AdventureNotFoundException)
        {
            return NotFound();
        }
    }
}