using FluentValidation;

namespace FableCraft.Application.Model;

public class LlmPresetDto
{
    public required string Name { get; init; } = string.Empty;

    public required string Provider { get; init; } = string.Empty;

    public required string Model { get; init; } = string.Empty;

    public string? BaseUrl { get; init; }

    public required string ApiKey { get; init; } = string.Empty;

    public int MaxTokens { get; init; }

    public double? Temperature { get; init; }

    public double? TopP { get; init; }

    public int? TopK { get; init; }

    public double? FrequencyPenalty { get; init; }

    public double? PresencePenalty { get; init; }
}

public class LlmPresetResponseDto
{
    public required Guid Id { get; init; }

    public required string Name { get; init; } = string.Empty;

    public required string Provider { get; init; } = string.Empty;

    public required string Model { get; init; } = string.Empty;

    public string? BaseUrl { get; init; }

    public required string ApiKey { get; init; } = string.Empty;

    public int MaxTokens { get; init; }

    public double? Temperature { get; init; }

    public double? TopP { get; init; }

    public int? TopK { get; init; }

    public double? FrequencyPenalty { get; init; }

    public double? PresencePenalty { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? UpdatedAt { get; init; }
}

public class LlmPresetDtoValidator : AbstractValidator<LlmPresetDto>
{
    public LlmPresetDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Preset name is required")
            .MaximumLength(100).WithMessage("Preset name must not exceed 100 characters");

        RuleFor(x => x.Provider)
            .NotEmpty().WithMessage("Provider is required")
            .MaximumLength(50).WithMessage("Provider must not exceed 50 characters");

        RuleFor(x => x.Model)
            .NotEmpty().WithMessage("Model is required")
            .MaximumLength(200).WithMessage("Model must not exceed 200 characters");

        RuleFor(x => x.BaseUrl)
            .MaximumLength(500).WithMessage("Base URL must not exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.BaseUrl));

        RuleFor(x => x.ApiKey)
            .NotEmpty().WithMessage("API key is required")
            .MaximumLength(500).WithMessage("API key must not exceed 500 characters");

        RuleFor(x => x.MaxTokens)
            .GreaterThan(0).WithMessage("Max tokens must be greater than 0");

        RuleFor(x => x.Temperature)
            .InclusiveBetween(0.0, 2.0).WithMessage("Temperature must be between 0 and 2")
            .When(x => x.Temperature.HasValue);

        RuleFor(x => x.TopP)
            .InclusiveBetween(0.0, 1.0).WithMessage("TopP must be between 0 and 1")
            .When(x => x.TopP.HasValue);

        RuleFor(x => x.TopK)
            .GreaterThan(0).WithMessage("TopK must be greater than 0")
            .When(x => x.TopK.HasValue);

        RuleFor(x => x.FrequencyPenalty)
            .InclusiveBetween(-2.0, 2.0).WithMessage("Frequency penalty must be between -2 and 2")
            .When(x => x.FrequencyPenalty.HasValue);

        RuleFor(x => x.PresencePenalty)
            .InclusiveBetween(-2.0, 2.0).WithMessage("Presence penalty must be between -2 and 2")
            .When(x => x.PresencePenalty.HasValue);
    }
}