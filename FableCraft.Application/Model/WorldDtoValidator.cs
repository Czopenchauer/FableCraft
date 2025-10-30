using FableCraft.Application.Validators;

using FluentValidation;

namespace FableCraft.Application.Model;

public class WorldDtoValidator : AbstractValidator<WorldDto>
{
    public WorldDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("World name is required")
            .MaximumLength(200).WithMessage("World name must not exceed 200 characters");

        RuleFor(x => x.Backstory)
            .NotEmpty().WithMessage("World backstory is required")
            .MaximumLength(5000).WithMessage("World backstory must not exceed 5000 characters");

        RuleFor(x => x.UniverseBackstory)
            .NotEmpty().WithMessage("Universe backstory is required")
            .MaximumLength(5000).WithMessage("Universe backstory must not exceed 5000 characters");

        RuleFor(x => x.Character)
            .NotNull().WithMessage("Character is required")
            .SetValidator(new CharacterDtoValidator());

        RuleFor(x => x.Lorebook)
            .NotNull().WithMessage("Lorebook cannot be null");

        RuleForEach(x => x.Lorebook)
            .SetValidator(new LorebookEntryDtoValidator());
    }
}

public class CharacterDtoValidator : AbstractValidator<CharacterDto>
{
    public CharacterDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Character name is required")
            .MaximumLength(200).WithMessage("Character name must not exceed 200 characters");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Character description is required")
            .MaximumLength(2000).WithMessage("Character description must not exceed 2000 characters");

        RuleFor(x => x.Background)
            .NotEmpty().WithMessage("Character background is required")
            .MaximumLength(5000).WithMessage("Character background must not exceed 5000 characters");
    }
}

public class LorebookEntryDtoValidator : AbstractValidator<LorebookEntryDto>
{
    public LorebookEntryDtoValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Lorebook entry title is required")
            .MaximumLength(200).WithMessage("Lorebook entry title must not exceed 200 characters");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Lorebook entry content is required")
            .MaximumLength(10000).WithMessage("Lorebook entry content must not exceed 10000 characters");

        RuleFor(x => x.Category)
            .NotEmpty().WithMessage("Lorebook entry category is required")
            .MaximumLength(100).WithMessage("Lorebook entry category must not exceed 100 characters");
    }
}