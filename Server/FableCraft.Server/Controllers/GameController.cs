using Microsoft.AspNetCore.Mvc;

namespace FableCraft.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GameController : ControllerBase
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
    [HttpDelete("redo")]
    public async Task<ActionResult> Redo()
    {
        // TODO: Implement redo logic
        return Ok();
    }

    /// <summary>
    ///     Regenerate the last response
    /// </summary>
    [HttpPost("regenerate")]
    public async Task<ActionResult> Regenerate()
    {
        // TODO: Implement regenerate logic
        return Ok();
    }
}