using FluentValidation;

namespace FableCraft.Application.Model.Worldbook;

public class WorldbookDto
{
    public required string Name { get; init; } = string.Empty;
}

public class WorldbookResponseDto
{
    public required Guid Id { get; init; }

    public required string Name { get; init; } = string.Empty;

    public List<LorebookResponseDto> Lorebooks { get; init; } = new();
}

public class WorldbookDtoValidator : AbstractValidator<WorldbookDto>
{
    public WorldbookDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Worldbook name is required")
            .MaximumLength(200).WithMessage("Worldbook name must not exceed 200 characters");
    }
}

