using System.ComponentModel.DataAnnotations;

using FluentValidation;

namespace FableCraft.ProjectManagement.Models;

public class ProjectFileDto
{
    public required string Name { get; init; } = string.Empty;

    public required string Content { get; init; } = string.Empty;

    public string? Category { get; init; }
}

public class ProjectFileDtoValidator : AbstractValidator<ProjectFileDto>
{
    public ProjectFileDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(500).WithMessage("Name must not exceed 500 characters");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Content is required");
    }
}

public class ProjectFileUpdateDto
{
    public required string Content { get; init; } = string.Empty;

    public string? Category { get; init; }
}

public class ProjectFileUpdateDtoValidator : AbstractValidator<ProjectFileUpdateDto>
{
    public ProjectFileUpdateDtoValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Content is required");
    }
}

public class ProjectFileResponseDto
{
    public required Guid Id { get; init; }

    public required Guid ProjectId { get; init; }

    public required string Name { get; init; } = string.Empty;

    public required string Content { get; init; } = string.Empty;

    public string? Category { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? UpdatedAt { get; init; }
}

public class ProjectFileSummaryDto
{
    public required Guid Id { get; init; }

    public required string Name { get; init; } = string.Empty;

    public string? Category { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? UpdatedAt { get; init; }
}