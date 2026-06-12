using FableCraft.Application.Chat;
using FableCraft.Application.Model;

using FluentValidation;

using Microsoft.AspNetCore.Mvc;

using ILogger = Serilog.ILogger;

namespace FableCraft.Server.Controllers;

[ApiController]
[Route("api/chat")]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;
    private readonly ILogger _logger;

    public ChatController(IChatService chatService, ILogger logger)
    {
        _chatService = chatService;
        _logger = logger;
    }

    [HttpGet("sessions")]
    [ProducesResponseType(typeof(IEnumerable<ChatSessionResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ChatSessionResponseDto>>> GetAllSessions(CancellationToken cancellationToken)
    {
        var sessions = await _chatService.GetAllSessionsAsync(cancellationToken);
        return Ok(sessions);
    }

    [HttpGet("sessions/{id:guid}")]
    [ProducesResponseType(typeof(ChatSessionWithMessagesDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChatSessionWithMessagesDto>> GetSession(Guid id, CancellationToken cancellationToken)
    {
        var session = await _chatService.GetSessionAsync(id, cancellationToken);
        if (session == null) return NotFound();
        return Ok(session);
    }

    [HttpPost("sessions")]
    [ProducesResponseType(typeof(ChatSessionResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ChatSessionResponseDto>> CreateSession(
        [FromBody] ChatSessionDto dto,
        [FromServices] IValidator<ChatSessionDto> validator,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(Results.ValidationProblem(validationResult.ToDictionary()));
        }

        var session = await _chatService.CreateSessionAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetSession), new { id = session.Id }, session);
    }

    [HttpPut("sessions/{id:guid}/preset")]
    [ProducesResponseType(typeof(ChatSessionResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChatSessionResponseDto>> UpdatePreset(
        Guid id,
        [FromBody] UpdateChatSessionPresetDto dto,
        [FromServices] IValidator<UpdateChatSessionPresetDto> validator,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(Results.ValidationProblem(validationResult.ToDictionary()));
        }

        var session = await _chatService.UpdatePresetAsync(id, dto.LlmPresetId, cancellationToken);
        if (session == null) return NotFound();
        return Ok(session);
    }

    [HttpDelete("sessions/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSession(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _chatService.DeleteSessionAsync(id, cancellationToken);
        if (!deleted) return NotFound();
        return NoContent();
    }

    [HttpDelete("sessions/{id:guid}/messages/latest")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteLatestMessage(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _chatService.DeleteLatestMessageAsync(id, cancellationToken);
        if (!deleted) return NotFound();
        return NoContent();
    }

    [HttpPost("sessions/{id:guid}/messages")]
    [ProducesResponseType(typeof(ChatMessageEntry), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ChatMessageEntry>> SendMessage(
        Guid id,
        [FromBody] ChatMessageDto dto,
        [FromServices] IValidator<ChatMessageDto> validator,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(Results.ValidationProblem(validationResult.ToDictionary()));
        }

        var message = await _chatService.SendMessageAsync(id, dto.Content, cancellationToken);
        if (message == null) return NotFound();
        return Ok(message);
    }
}