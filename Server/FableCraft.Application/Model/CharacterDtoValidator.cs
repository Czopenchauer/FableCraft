using FluentValidation;

namespace FableCraft.Application.Model;

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