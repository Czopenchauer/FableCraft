using System.ComponentModel.DataAnnotations;

using FluentValidation;

namespace FableCraft.ProjectManagement.Models;

public class ProjectChatSessionDto
{
    public required Guid LlmPresetId { get; init; }

    public required string Title { get; init; } = string.Empty;
}

public class ProjectChatSessionDtoValidator : AbstractValidator<ProjectChatSessionDto>
{
    public ProjectChatSessionDtoValidator()
    {
        RuleFor(x => x.LlmPresetId)
            .NotEmpty().WithMessage("LLM Preset ID is required");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(500).WithMessage("Title must not exceed 500 characters");
    }
}

public class ProjectChatSessionResponseDto
{
    public required Guid Id { get; init; }

    public required Guid ProjectId { get; init; }

    public required Guid LlmPresetId { get; init; }

    public required string LlmPresetName { get; init; } = string.Empty;

    public required string Title { get; init; } = string.Empty;

    public required DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? UpdatedAt { get; init; }
}

public class ProjectChatMessageDto
{
    public required string Content { get; init; } = string.Empty;
}

public class ProjectChatMessageDtoValidator : AbstractValidator<ProjectChatMessageDto>
{
    public ProjectChatMessageDtoValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Message content is required");
    }
}

public class ProjectChatMessageEntry
{
    public required string Role { get; init; }
    public required string Content { get; init; }
}