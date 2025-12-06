using FableCraft.Application.Model.Worldbook;

using FluentValidation;

namespace FableCraft.Application.Model.Adventure;

public class AdventureDto
{
    public required string Name { get; init; } = string.Empty;

    public required string FirstSceneDescription { get; init; } = string.Empty;

    public required string ReferenceTime { get; init; }

    public required string AuthorNotes { get; init; } = string.Empty;

    public required CharacterDto Character { get; init; } = null!;

    public List<LorebookEntryDto> Lorebook { get; init; } = new();

    public required string TrackerStructure { get; init; }
}

public class AdventureDtoValidator : AbstractValidator<AdventureDto>
{
    public AdventureDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Adventure name is required")
            .MaximumLength(200).WithMessage("Adventure name must not exceed 200 characters");

        RuleFor(x => x.Character)
            .NotNull().WithMessage("Character is required")
            .SetValidator(new CharacterDtoValidator());

        RuleForEach(x => x.Lorebook)
            .SetValidator(new LorebookEntryDtoValidator());

        RuleFor(x => x.Lorebook)
            .Must(HaveUniqueCategories)
            .WithMessage("Lorebook entries must have unique content");
    }

    private static bool HaveUniqueCategories(List<LorebookEntryDto>? lorebook)
    {
        if (lorebook == null || lorebook.Count == 0)
            return true;

        return lorebook.Count == lorebook.Select(x => x.Content).Distinct(StringComparer.OrdinalIgnoreCase).Count();
    }
}