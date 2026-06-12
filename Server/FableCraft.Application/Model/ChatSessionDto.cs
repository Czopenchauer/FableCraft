using FluentValidation;

namespace FableCraft.Application.Model;

public class ChatSessionDto
{
    public required Guid AdventureId { get; init; }

    public required Guid LlmPresetId { get; init; }

    public required string Title { get; init; } = string.Empty;
}

public class ChatSessionDtoValidator : AbstractValidator<ChatSessionDto>
{
    public ChatSessionDtoValidator()
    {
        RuleFor(x => x.AdventureId)
            .NotEmpty().WithMessage("Adventure ID is required");

        RuleFor(x => x.LlmPresetId)
            .NotEmpty().WithMessage("LLM Preset ID is required");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(500).WithMessage("Title must not exceed 500 characters");
    }
}

public class ChatSessionResponseDto
{
    public required Guid Id { get; init; }

    public required Guid AdventureId { get; init; }

    public required string AdventureName { get; init; } = string.Empty;

    public required Guid LlmPresetId { get; init; }

    public required string LlmPresetName { get; init; } = string.Empty;

    public required string Title { get; init; } = string.Empty;

    public required DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? UpdatedAt { get; init; }
}

public class UpdateChatSessionPresetDto
{
    public required Guid LlmPresetId { get; init; }
}

public class UpdateChatSessionPresetDtoValidator : AbstractValidator<UpdateChatSessionPresetDto>
{
    public UpdateChatSessionPresetDtoValidator()
    {
        RuleFor(x => x.LlmPresetId)
            .NotEmpty().WithMessage("LLM Preset ID is required");
    }
}

public class ChatMessageDto
{
    public required string Content { get; init; } = string.Empty;
}

public class ChatMessageDtoValidator : AbstractValidator<ChatMessageDto>
{
    public ChatMessageDtoValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Message content is required");
    }
}