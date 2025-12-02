using System.Text.Json.Serialization;

using FableCraft.Infrastructure.Persistence.Entities;

using FluentValidation;

namespace FableCraft.Application.Model;

public class LorebookEntryDto
{
    public string Description { get; init; } = string.Empty;

    public string Content { get; init; } = string.Empty;

    public string Category { get; init; } = string.Empty;

    public int Order { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ContentType ContentType { get; set; }
}

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