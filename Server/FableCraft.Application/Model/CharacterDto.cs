using FluentValidation;

namespace FableCraft.Application.Model;

public class CharacterDto
{
    public string Name { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;
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
            .MaximumLength(20000).WithMessage("Character description must not exceed 20000 characters");
    }
}