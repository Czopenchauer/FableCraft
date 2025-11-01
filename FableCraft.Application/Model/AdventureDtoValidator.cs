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

        RuleFor(x => x.Lorebook)
            .NotNull().WithMessage("Lorebook cannot be null");

        RuleForEach(x => x.Lorebook)
            .SetValidator(new LorebookEntryDtoValidator());

        RuleFor(x => x.Lorebook)
            .Must(HaveUniqueCategories)
            .WithMessage("Lorebook entries must have unique categories");
    }

    private static bool HaveUniqueCategories(List<LorebookEntryDto>? lorebook)
    {
        if (lorebook == null || lorebook.Count == 0)
            return true;

        return lorebook.Count == lorebook.Select(x => x.Category).Distinct(StringComparer.OrdinalIgnoreCase).Count();
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
        RuleFor(x => x.Description)
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

public class GenerateLorebookDtoValidator : AbstractValidator<GenerateLorebookDto>
{
    private readonly AvailableLorebookDto[] _supportedCategories;

    public GenerateLorebookDtoValidator(AdventureGeneration.IAdventureCreationService adventureCreationService)
    {
        _supportedCategories = adventureCreationService.GetSupportedLorebook();

        RuleFor(x => x.Category)
            .NotEmpty().WithMessage("Category is required")
            .Must(BeSupportedCategory)
            .WithMessage(x => $"Category '{x.Category}' is not supported. Supported categories are: {string.Join(", ", _supportedCategories.Select(y => y.Category))}");

        RuleFor(x => x.Lorebooks)
            .NotNull().WithMessage("Lorebooks cannot be null");

        RuleForEach(x => x.Lorebooks)
            .SetValidator(new LorebookEntryDtoValidator())
            .ChildRules(lorebook =>
            {
                lorebook.RuleFor(x => x.Category)
                    .Must((_, category) => BeSupportedCategory(category))
                    .WithMessage((_, category) => $"Lorebook category '{category}' is not supported. Supported categories are: {string.Join(", ", _supportedCategories.Select(y => y.Category))}");
            });

        RuleFor(x => x.Lorebooks)
            .Must(HaveUniqueCategories)
            .WithMessage("Lorebook entries must have unique categories");

        RuleFor(x => x.AdditionalInstruction)
            .MaximumLength(5000).WithMessage("Additional instruction must not exceed 5000 characters")
            .When(x => !string.IsNullOrEmpty(x.AdditionalInstruction));
    }

    private bool BeSupportedCategory(string category)
    {
        return _supportedCategories.Any(x => x.Category == category);
    }

    private static bool HaveUniqueCategories(LorebookEntryDto[]? lorebooks)
    {
        if (lorebooks == null || lorebooks.Length == 0)
            return true;

        return lorebooks.Length == lorebooks.Select(x => x.Category).Distinct(StringComparer.OrdinalIgnoreCase).Count();
    }
}