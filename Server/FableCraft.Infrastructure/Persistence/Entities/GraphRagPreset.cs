using System.ComponentModel.DataAnnotations;

namespace FableCraft.Infrastructure.Persistence.Entities;

public abstract class GraphRagPresetBase : IEntity
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = null!;

    [Required]
    [MaxLength(50)]
    public string LlmProvider { get; set; } = null!;

    [Required]
    [MaxLength(200)]
    public string LlmModel { get; set; } = null!;

    [MaxLength(500)]
    public string? LlmBaseUrl { get; set; }

    [MaxLength(500)]
    public string? LlmApiKey { get; set; }

    [MaxLength(50)]
    public string? LlmApiVersion { get; set; }

    public int? LlmMaxTokens { get; set; }

    public bool RateLimitEnabled { get; set; } = true;

    public int RateLimitRequests { get; set; } = 60;

    public int RateLimitInterval { get; set; } = 60;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }
}

public class GraphRagSearchPreset : GraphRagPresetBase
{
}

public class GraphRagBuildPreset : GraphRagPresetBase
{
    [MaxLength(50)]
    public string? EmbeddingProvider { get; set; }

    [MaxLength(200)]
    public string? EmbeddingModel { get; set; }

    [MaxLength(500)]
    public string? EmbeddingBaseUrl { get; set; }

    [MaxLength(50)]
    public string? EmbeddingApiVersion { get; set; }

    public int? EmbeddingDimensions { get; set; }

    public int? EmbeddingMaxTokens { get; set; }

    public int? EmbeddingBatchSize { get; set; }

    [MaxLength(200)]
    public string? HuggingFaceTokenizer { get; set; }
}