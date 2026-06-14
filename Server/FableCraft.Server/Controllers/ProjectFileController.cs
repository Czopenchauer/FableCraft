using FableCraft.ProjectManagement.Models;
using FableCraft.ProjectManagement.Services;

using FluentValidation;

using Microsoft.AspNetCore.Mvc;

using ILogger = Serilog.ILogger;

namespace FableCraft.Server.Controllers;

[ApiController]
[Route("api/projects/{projectId:guid}/files")]
public class ProjectFileController : ControllerBase
{
    private readonly IProjectFileService _fileService;
    private readonly ILogger _logger;

    public ProjectFileController(IProjectFileService fileService, ILogger logger)
    {
        _fileService = fileService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ProjectFileSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ProjectFileSummaryDto>>> List(
        Guid projectId,
        [FromQuery] string? category,
        CancellationToken cancellationToken)
    {
        var files = await _fileService.ListFilesAsync(projectId, category, cancellationToken);
        return Ok(files);
    }

    [HttpGet("{fileId:guid}")]
    [ProducesResponseType(typeof(ProjectFileResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProjectFileResponseDto>> Get(
        Guid projectId,
        Guid fileId,
        CancellationToken cancellationToken)
    {
        var file = await _fileService.GetFileAsync(projectId, fileId, cancellationToken);
        if (file is null) return NotFound();
        return Ok(file);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ProjectFileResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProjectFileResponseDto>> Create(
        Guid projectId,
        [FromBody] ProjectFileDto dto,
        [FromServices] IValidator<ProjectFileDto> validator,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(Results.ValidationProblem(validationResult.ToDictionary()));
        }

        var file = await _fileService.CreateFileAsync(projectId, dto, cancellationToken);
        return CreatedAtAction(nameof(Get), new { projectId, fileId = file.Id }, file);
    }

    [HttpPut("{fileId:guid}")]
    [ProducesResponseType(typeof(ProjectFileResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProjectFileResponseDto>> Update(
        Guid projectId,
        Guid fileId,
        [FromBody] ProjectFileUpdateDto dto,
        [FromServices] IValidator<ProjectFileUpdateDto> validator,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(Results.ValidationProblem(validationResult.ToDictionary()));
        }

        var file = await _fileService.UpdateFileAsync(projectId, fileId, dto, cancellationToken);
        if (file is null) return NotFound();
        return Ok(file);
    }

    [HttpDelete("{fileId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid projectId,
        Guid fileId,
        CancellationToken cancellationToken)
    {
        var deleted = await _fileService.DeleteFileAsync(projectId, fileId, cancellationToken);
        if (!deleted) return NotFound();
        return NoContent();
    }
}