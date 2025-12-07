using FluentValidation;

namespace FableCraft.Application.Model.Adventure;

public class AdventureDto
{
    public required string Name { get; init; } = string.Empty;

    public required string FirstSceneDescription { get; init; } = string.Empty;

    public required string ReferenceTime { get; init; }

    public required string AuthorNotes { get; init; } = string.Empty;

    public required CharacterDto Character { get; init; } = null!;

    public Guid? WorldbookId { get; init; }

    public required Guid TrackerDefinitionId { get; init; }

    public required Guid FastLlmConfig { get; init; }

    public required Guid ComplexLlmConfig { get; init; }
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
    }
}