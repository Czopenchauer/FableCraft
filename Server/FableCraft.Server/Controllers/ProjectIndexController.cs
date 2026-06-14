using FableCraft.ProjectManagement.Models;
using FableCraft.ProjectManagement.Services;

using Microsoft.AspNetCore.Mvc;

using ILogger = Serilog.ILogger;

namespace FableCraft.Server.Controllers;

[ApiController]
[Route("api/projects/{projectId:guid}/index")]
public class ProjectIndexController : ControllerBase
{
    private readonly IProjectFileService _fileService;
    private readonly ILogger _logger;

    public ProjectIndexController(IProjectFileService fileService, ILogger logger)
    {
        _fileService = fileService;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> IndexProject(Guid projectId, CancellationToken cancellationToken)
    {
        try
        {
            await _fileService.IndexProjectAsync(projectId, cancellationToken);
            return Ok(new { message = "Indexing completed successfully" });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already being indexed"))
        {
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpGet("status")]
    [ProducesResponseType(typeof(IndexingStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IndexingStatusResponse>> GetStatus(Guid projectId, CancellationToken cancellationToken)
    {
        try
        {
            var status = await _fileService.GetIndexingStatusAsync(projectId, cancellationToken);
            return Ok(status);
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }
}