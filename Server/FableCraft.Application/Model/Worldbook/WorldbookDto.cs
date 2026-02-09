using System.Text.Json.Serialization;

using FableCraft.Infrastructure.Persistence.Entities;

using FluentValidation;

namespace FableCraft.Application.Model.Worldbook;

public class WorldbookDto
{
    public required string Name { get; init; } = string.Empty;

    public List<CreateLorebookDto> Lorebooks { get; init; } = new();

    public Guid? GraphRagSettingsId { get; init; }
}

public class WorldbookUpdateDto
{
    public required string Name { get; init; } = string.Empty;

    public List<UpdateLorebookDto> Lorebooks { get; init; } = new();

    public Guid? GraphRagSettingsId { get; init; }
}

public class WorldbookResponseDto
{
    public required Guid Id { get; init; }

    public required string Name { get; init; } = string.Empty;

    public List<LorebookResponseDto> Lorebooks { get; init; } = new();

    public Guid? GraphRagSettingsId { get; init; }

    public GraphRagSettingsSummaryDto? GraphRagSettings { get; init; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public IndexingStatus IndexingStatus { get; init; }

    public DateTimeOffset? LastIndexedAt { get; init; }

    public bool HasPendingChanges { get; init; }

    public PendingChangeSummaryDto? PendingChangeSummary { get; init; }
}

public class PendingChangeSummaryDto
{
    public int AddedCount { get; init; }
    public int ModifiedCount { get; init; }
    public int DeletedCount { get; init; }
}

public class CopyWorldbookDto
{
    public required string Name { get; init; }
    public bool CopyIndexedVolume { get; init; }
}

public class WorldbookDtoValidator : AbstractValidator<WorldbookDto>
{
    public WorldbookDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Worldbook name is required")
            .MaximumLength(200).WithMessage("Worldbook name must not exceed 200 characters");

        RuleForEach(x => x.Lorebooks)
            .SetValidator(new CreateLorebookDtoValidator());
    }
}

public class WorldbookUpdateDtoValidator : AbstractValidator<WorldbookUpdateDto>
{
    public WorldbookUpdateDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Worldbook name is required")
            .MaximumLength(200).WithMessage("Worldbook name must not exceed 200 characters");

        RuleForEach(x => x.Lorebooks)
            .SetValidator(new UpdateLorebookDtoValidator());
    }
}