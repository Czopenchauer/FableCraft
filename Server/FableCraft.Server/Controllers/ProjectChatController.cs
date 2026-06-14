using FableCraft.ProjectManagement.Models;
using FableCraft.ProjectManagement.Services;

using FluentValidation;

using Microsoft.AspNetCore.Mvc;

using ILogger = Serilog.ILogger;

namespace FableCraft.Server.Controllers;

[ApiController]
[Route("api/projects/{projectId:guid}/chat")]
public class ProjectChatController : ControllerBase
{
    private readonly IProjectChatService _chatService;
    private readonly ILogger _logger;

    public ProjectChatController(IProjectChatService chatService, ILogger logger)
    {
        _chatService = chatService;
        _logger = logger;
    }

    [HttpGet("sessions")]
    [ProducesResponseType(typeof(IEnumerable<ProjectChatSessionResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ProjectChatSessionResponseDto>>> GetAllSessions(
        Guid projectId,
        CancellationToken cancellationToken)
    {
        var sessions = await _chatService.GetAllSessionsAsync(projectId, cancellationToken);
        return Ok(sessions);
    }

    [HttpGet("sessions/{sessionId:guid}")]
    [ProducesResponseType(typeof(ProjectChatSessionResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProjectChatSessionResponseDto>> GetSession(
        Guid projectId,
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        var session = await _chatService.GetSessionAsync(projectId, sessionId, cancellationToken);
        if (session is null) return NotFound();
        return Ok(session);
    }

    [HttpPost("sessions")]
    [ProducesResponseType(typeof(ProjectChatSessionResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProjectChatSessionResponseDto>> CreateSession(
        Guid projectId,
        [FromBody] ProjectChatSessionDto dto,
        [FromServices] IValidator<ProjectChatSessionDto> validator,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(Results.ValidationProblem(validationResult.ToDictionary()));
        }

        var session = await _chatService.CreateSessionAsync(projectId, dto, cancellationToken);
        return CreatedAtAction(nameof(GetSession), new { projectId, sessionId = session.Id }, session);
    }

    [HttpDelete("sessions/{sessionId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSession(
        Guid projectId,
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        var deleted = await _chatService.DeleteSessionAsync(projectId, sessionId, cancellationToken);
        if (!deleted) return NotFound();
        return NoContent();
    }

    [HttpPost("sessions/{sessionId:guid}/messages")]
    [ProducesResponseType(typeof(ProjectChatMessageEntry), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProjectChatMessageEntry>> SendMessage(
        Guid projectId,
        Guid sessionId,
        [FromBody] ProjectChatMessageDto dto,
        [FromServices] IValidator<ProjectChatMessageDto> validator,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(Results.ValidationProblem(validationResult.ToDictionary()));
        }

        var message = await _chatService.SendMessageAsync(projectId, sessionId, dto.Content, cancellationToken);
        if (message is null) return NotFound();
        return Ok(message);
    }
}