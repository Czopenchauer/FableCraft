using FluentValidation;

namespace FableCraft.Application.Model;

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