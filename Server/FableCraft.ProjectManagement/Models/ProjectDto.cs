using System.ComponentModel.DataAnnotations;

using FluentValidation;

namespace FableCraft.ProjectManagement.Models;

public enum IndexingStatusDto
{
    NotIndexed,
    Indexing,
    Indexed,
    NeedsReindexing
}

public class ProjectDto
{
    public required string Name { get; init; } = string.Empty;

    public string? Description { get; init; }

    public Guid? GraphRagSettingsId { get; init; }

    public Guid? LlmPresetId { get; init; }
}

public class ProjectDtoValidator : AbstractValidator<ProjectDto>
{
    public ProjectDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters");
    }
}

public class ProjectResponseDto
{
    public required Guid Id { get; init; }

    public required string Name { get; init; } = string.Empty;

    public string? Description { get; init; }

    public Guid? GraphRagSettingsId { get; init; }

    public string? GraphRagSettingsName { get; init; }

    public Guid? LlmPresetId { get; init; }

    public string? LlmPresetName { get; init; }

    public required IndexingStatusDto IndexingStatus { get; init; }

    public DateTimeOffset? LastIndexedAt { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? UpdatedAt { get; init; }

    public List<ProjectFileSummaryDto> Files { get; init; } = [];
}

public class ProjectUpdateDto
{
    public string? Name { get; init; }

    public string? Description { get; init; }

    public Guid? GraphRagSettingsId { get; init; }

    public Guid? LlmPresetId { get; init; }
}

public class ProjectUpdateDtoValidator : AbstractValidator<ProjectUpdateDto>
{
    public ProjectUpdateDtoValidator()
    {
        RuleFor(x => x.Name)
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters")
            .When(x => x.Name is not null);
    }
}