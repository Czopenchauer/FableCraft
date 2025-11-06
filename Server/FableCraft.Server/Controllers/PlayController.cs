using Microsoft.AspNetCore.Mvc;

namespace FableCraft.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PlayController : ControllerBase
{
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
    [HttpDelete("delete/{adventureId:guid}")]
    public async Task<ActionResult> DeleteLastScene(Guid adventureId, CancellationToken cancellationToken)
    {
        // TODO: Implement redo logic
        return Ok();
    }

    /// <summary>
    ///     Regenerate the last response
    /// </summary>
    [HttpPost("regenerate/{adventureId:guid}")]
    public async Task<ActionResult> Regenerate(Guid adventureId, CancellationToken cancellationToken)
    {
        // TODO: Implement regenerate logic
        return Ok();
    }
}