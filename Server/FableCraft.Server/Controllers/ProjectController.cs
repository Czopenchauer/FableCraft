using FableCraft.ProjectManagement.Models;
using FableCraft.ProjectManagement.Services;

using FluentValidation;

using Microsoft.AspNetCore.Mvc;

using ILogger = Serilog.ILogger;

namespace FableCraft.Server.Controllers;

[ApiController]
[Route("api/projects")]
public class ProjectController : ControllerBase
{
    private readonly IProjectService _projectService;
    private readonly ILogger _logger;

    public ProjectController(IProjectService projectService, ILogger logger)
    {
        _projectService = projectService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ProjectResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ProjectResponseDto>>> GetAll(CancellationToken cancellationToken)
    {
        var projects = await _projectService.GetAllProjectsAsync(cancellationToken);
        return Ok(projects);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ProjectResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProjectResponseDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var project = await _projectService.GetProjectAsync(id, cancellationToken);
        if (project is null) return NotFound();
        return Ok(project);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ProjectResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProjectResponseDto>> Create(
        [FromBody] ProjectDto dto,
        [FromServices] IValidator<ProjectDto> validator,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(Results.ValidationProblem(validationResult.ToDictionary()));
        }

        var project = await _projectService.CreateProjectAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = project.Id }, project);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ProjectResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProjectResponseDto>> Update(
        Guid id,
        [FromBody] ProjectUpdateDto dto,
        [FromServices] IValidator<ProjectUpdateDto> validator,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(Results.ValidationProblem(validationResult.ToDictionary()));
        }

        var project = await _projectService.UpdateProjectAsync(id, dto, cancellationToken);
        if (project is null) return NotFound();
        return Ok(project);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _projectService.DeleteProjectAsync(id, cancellationToken);
        if (!deleted) return NotFound();
        return NoContent();
    }
}