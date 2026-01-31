using System.ComponentModel.DataAnnotations;

namespace FableCraft.Infrastructure.Persistence.Entities;

public class GraphRagSettings : IEntity
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(100)]
    public required string Name { get; set; } = null!;

    [Required]
    [MaxLength(50)]
    public required string LlmProvider { get; set; } = null!;

    [Required]
    [MaxLength(200)]
    public required string LlmModel { get; set; } = null!;

    [MaxLength(500)]
    public string? LlmEndpoint { get; set; }

    [Required]
    [MaxLength(500)]
    public required string LlmApiKey { get; set; } = null!;

    [MaxLength(50)]
    public string? LlmApiVersion { get; set; }

    public int LlmMaxTokens { get; set; } = 4096;

    public bool LlmRateLimitEnabled { get; set; }

    public int LlmRateLimitRequests { get; set; }

    public int LlmRateLimitInterval { get; set; }

    [Required]
    [MaxLength(50)]
    public required string EmbeddingProvider { get; set; } = null!;

    [Required]
    [MaxLength(200)]
    public required string EmbeddingModel { get; set; } = null!;

    [MaxLength(500)]
    public string? EmbeddingEndpoint { get; set; }

    [MaxLength(500)]
    public string? EmbeddingApiKey { get; set; }

    public string? EmbeddingApiVersion { get; set; }

    public int EmbeddingDimensions { get; set; } = 1536;

    public int EmbeddingMaxTokens { get; set; } = 8191;

    public int EmbeddingBatchSize { get; set; } = 100;

    [MaxLength(200)]
    public string? HuggingfaceTokenizer { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public ICollection<Worldbook> Worldbooks { get; set; } = new List<Worldbook>();

    public ICollection<Adventure.Adventure> Adventures { get; set; } = new List<Adventure.Adventure>();
}
