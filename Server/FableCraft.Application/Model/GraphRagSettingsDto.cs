using FluentValidation;

namespace FableCraft.Application.Model;

public class GraphRagSettingsDto
{
    public required string Name { get; init; } = string.Empty;

    // LLM Configuration
    public required string LlmProvider { get; init; } = string.Empty;
    public required string LlmModel { get; init; } = string.Empty;
    public string? LlmEndpoint { get; init; }
    public required string LlmApiKey { get; init; } = string.Empty;
    public string? LlmApiVersion { get; init; }
    public int LlmMaxTokens { get; init; } = 4096;
    public bool LlmRateLimitEnabled { get; init; }
    public int LlmRateLimitRequests { get; init; }
    public int LlmRateLimitInterval { get; init; }

    // Embedding Configuration
    public required string EmbeddingProvider { get; init; } = string.Empty;
    public required string EmbeddingModel { get; init; } = string.Empty;
    public string? EmbeddingEndpoint { get; init; }
    public string? EmbeddingApiKey { get; init; }
    public string? EmbeddingApiVersion { get; init; }
    public int EmbeddingDimensions { get; init; } = 1536;
    public int EmbeddingMaxTokens { get; init; } = 8191;
    public int EmbeddingBatchSize { get; init; } = 100;
    public string? HuggingfaceTokenizer { get; init; }
}

public class GraphRagSettingsResponseDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; } = string.Empty;

    // LLM Configuration
    public required string LlmProvider { get; init; } = string.Empty;
    public required string LlmModel { get; init; } = string.Empty;
    public string? LlmEndpoint { get; init; }
    public required string LlmApiKey { get; init; } = string.Empty;
    public string? LlmApiVersion { get; init; }
    public int LlmMaxTokens { get; init; }
    public bool LlmRateLimitEnabled { get; init; }
    public int LlmRateLimitRequests { get; init; }
    public int LlmRateLimitInterval { get; init; }

    // Embedding Configuration
    public required string EmbeddingProvider { get; init; } = string.Empty;
    public required string EmbeddingModel { get; init; } = string.Empty;
    public string? EmbeddingEndpoint { get; init; }
    public string? EmbeddingApiKey { get; init; }
    public string? EmbeddingApiVersion { get; init; }
    public int EmbeddingDimensions { get; init; }
    public int EmbeddingMaxTokens { get; init; }
    public int EmbeddingBatchSize { get; init; }
    public string? HuggingfaceTokenizer { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
}

public class GraphRagSettingsSummaryDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; } = string.Empty;
    public required string LlmProvider { get; init; } = string.Empty;
    public required string LlmModel { get; init; } = string.Empty;
    public required string EmbeddingProvider { get; init; } = string.Empty;
    public required string EmbeddingModel { get; init; } = string.Empty;
}

public class GraphRagSettingsDtoValidator : AbstractValidator<GraphRagSettingsDto>
{
    public GraphRagSettingsDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters");

        // LLM Validation
        RuleFor(x => x.LlmProvider)
            .NotEmpty().WithMessage("LLM Provider is required")
            .MaximumLength(50).WithMessage("LLM Provider must not exceed 50 characters");

        RuleFor(x => x.LlmModel)
            .NotEmpty().WithMessage("LLM Model is required")
            .MaximumLength(200).WithMessage("LLM Model must not exceed 200 characters");

        RuleFor(x => x.LlmEndpoint)
            .MaximumLength(500).WithMessage("LLM Endpoint must not exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.LlmEndpoint));

        RuleFor(x => x.LlmApiKey)
            .NotEmpty().WithMessage("LLM API Key is required")
            .MaximumLength(500).WithMessage("LLM API Key must not exceed 500 characters");

        RuleFor(x => x.LlmApiVersion)
            .MaximumLength(50).WithMessage("LLM API Version must not exceed 50 characters")
            .When(x => !string.IsNullOrEmpty(x.LlmApiVersion));

        RuleFor(x => x.LlmMaxTokens)
            .GreaterThan(0).WithMessage("LLM Max Tokens must be greater than 0");

        RuleFor(x => x.LlmRateLimitRequests)
            .GreaterThanOrEqualTo(0).WithMessage("LLM Rate Limit Requests must be non-negative");

        RuleFor(x => x.LlmRateLimitInterval)
            .GreaterThanOrEqualTo(0).WithMessage("LLM Rate Limit Interval must be non-negative");

        // Embedding Validation
        RuleFor(x => x.EmbeddingProvider)
            .NotEmpty().WithMessage("Embedding Provider is required")
            .MaximumLength(50).WithMessage("Embedding Provider must not exceed 50 characters");

        RuleFor(x => x.EmbeddingModel)
            .NotEmpty().WithMessage("Embedding Model is required")
            .MaximumLength(200).WithMessage("Embedding Model must not exceed 200 characters");

        RuleFor(x => x.EmbeddingEndpoint)
            .MaximumLength(500).WithMessage("Embedding Endpoint must not exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.EmbeddingEndpoint));

        RuleFor(x => x.EmbeddingApiKey)
            .MaximumLength(500).WithMessage("Embedding API Key must not exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.EmbeddingApiKey));

        RuleFor(x => x.EmbeddingApiVersion)
            .MaximumLength(50).WithMessage("Embedding API Version must not exceed 50 characters")
            .When(x => !string.IsNullOrEmpty(x.EmbeddingApiVersion));

        RuleFor(x => x.EmbeddingDimensions)
            .GreaterThan(0).WithMessage("Embedding Dimensions must be greater than 0");

        RuleFor(x => x.EmbeddingMaxTokens)
            .GreaterThan(0).WithMessage("Embedding Max Tokens must be greater than 0");

        RuleFor(x => x.EmbeddingBatchSize)
            .GreaterThan(0).WithMessage("Embedding Batch Size must be greater than 0");

        RuleFor(x => x.HuggingfaceTokenizer)
            .MaximumLength(200).WithMessage("Huggingface Tokenizer must not exceed 200 characters")
            .When(x => !string.IsNullOrEmpty(x.HuggingfaceTokenizer));
    }
}
