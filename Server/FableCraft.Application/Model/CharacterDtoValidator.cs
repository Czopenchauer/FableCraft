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
            .MaximumLength(20000).WithMessage("Character description must not exceed 20000 characters");

        RuleFor(x => x.Background)
            .NotEmpty().WithMessage("Character background is required")
            .MaximumLength(50000).WithMessage("Character background must not exceed 50000 characters");
    }
}