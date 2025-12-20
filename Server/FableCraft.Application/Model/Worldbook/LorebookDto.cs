using System.Text.Json.Serialization;

using FableCraft.Infrastructure.Persistence.Entities;

using FluentValidation;

namespace FableCraft.Application.Model.Worldbook;

public class LorebookDto
{
    public required Guid WorldbookId { get; init; }

    public required string Title { get; init; } = string.Empty;

    public required string Content { get; init; } = string.Empty;

    public required string Category { get; init; } = string.Empty;
    
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required ContentType ContentType { get; set; }
}

public class CreateLorebookDto
{
    public required string Title { get; init; } = string.Empty;

    public required string Content { get; init; } = string.Empty;

    public required string Category { get; init; } = string.Empty;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required ContentType ContentType { get; init; }
}

public class UpdateLorebookDto
{
    public Guid? Id { get; init; }

    public required string Title { get; init; } = string.Empty;

    public required string Content { get; init; } = string.Empty;

    public required string Category { get; init; } = string.Empty;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required ContentType ContentType { get; init; }
}

public class LorebookResponseDto
{
    public required Guid Id { get; init; }

    public required Guid WorldbookId { get; init; }

    public required string Title { get; init; } = string.Empty;

    public required string Content { get; init; } = string.Empty;

    public required string Category { get; init; } = string.Empty;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required ContentType ContentType { get; init; }
}

public class LorebookDtoValidator : AbstractValidator<LorebookDto>
{
    public LorebookDtoValidator()
    {
        RuleFor(x => x.WorldbookId)
            .NotEmpty().WithMessage("Worldbook ID is required");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Lorebook title is required")
            .MaximumLength(200).WithMessage("Lorebook title must not exceed 200 characters");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Lorebook content is required");

        RuleFor(x => x.Category)
            .NotEmpty().WithMessage("Lorebook category is required")
            .MaximumLength(100).WithMessage("Lorebook category must not exceed 100 characters");
    }
}

public class CreateLorebookDtoValidator : AbstractValidator<CreateLorebookDto>
{
    public CreateLorebookDtoValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Lorebook title is required")
            .MaximumLength(200).WithMessage("Lorebook title must not exceed 200 characters");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Lorebook content is required");

        RuleFor(x => x.Category)
            .NotEmpty().WithMessage("Lorebook category is required")
            .MaximumLength(100).WithMessage("Lorebook category must not exceed 100 characters");
    }
}

public class UpdateLorebookDtoValidator : AbstractValidator<UpdateLorebookDto>
{
    public UpdateLorebookDtoValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Lorebook title is required")
            .MaximumLength(200).WithMessage("Lorebook title must not exceed 200 characters");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Lorebook content is required");

        RuleFor(x => x.Category)
            .NotEmpty().WithMessage("Lorebook category is required")
            .MaximumLength(100).WithMessage("Lorebook category must not exceed 100 characters");
    }
}