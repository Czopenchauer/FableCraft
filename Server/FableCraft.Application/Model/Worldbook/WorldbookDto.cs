using FluentValidation;

namespace FableCraft.Application.Model.Worldbook;

public class WorldbookDto
{
    public required string Name { get; init; } = string.Empty;

    public List<CreateLorebookDto> Lorebooks { get; init; } = new();
}

public class WorldbookUpdateDto
{
    public required string Name { get; init; } = string.Empty;

    public List<UpdateLorebookDto> Lorebooks { get; init; } = new();
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

        RuleForEach(x => x.Lorebooks)
            .SetValidator(new CreateLorebookDtoValidator());
    }
}

public class WorldbookUpdateDtoValidator : AbstractValidator<WorldbookUpdateDto>
{
    public WorldbookUpdateDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Worldbook name is required")
            .MaximumLength(200).WithMessage("Worldbook name must not exceed 200 characters");

        RuleForEach(x => x.Lorebooks)
            .SetValidator(new UpdateLorebookDtoValidator());
    }
}

