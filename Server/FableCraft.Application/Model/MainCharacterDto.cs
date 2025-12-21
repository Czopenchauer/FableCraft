using FluentValidation;

namespace FableCraft.Application.Model;

public class MainCharacterDto
{
    public string Name { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;
}

public class CharacterDtoValidator : AbstractValidator<MainCharacterDto>
{
    public CharacterDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("MainCharacter name is required")
            .MaximumLength(200).WithMessage("MainCharacter name must not exceed 200 characters");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("MainCharacter description is required")
            .MaximumLength(20000).WithMessage("MainCharacter description must not exceed 20000 characters");
    }
}