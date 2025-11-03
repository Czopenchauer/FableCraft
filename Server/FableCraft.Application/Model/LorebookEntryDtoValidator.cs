using FluentValidation;

namespace FableCraft.Application.Model;

public class LorebookEntryDtoValidator : AbstractValidator<LorebookEntryDto>
{
    public LorebookEntryDtoValidator()
    {
        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Lorebook entry title is required")
            .MaximumLength(200).WithMessage("Lorebook entry title must not exceed 200 characters");

        RuleFor(x => x.Category)
            .NotEmpty().WithMessage("Lorebook entry category is required")
            .MaximumLength(100).WithMessage("Lorebook entry category must not exceed 100 characters");
    }
}