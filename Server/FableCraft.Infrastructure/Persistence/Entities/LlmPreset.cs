using System.ComponentModel.DataAnnotations;

namespace FableCraft.Infrastructure.Persistence.Entities;

public class LlmPreset : IEntity
{
    [Required]
    [MaxLength(100)]
    public required string Name { get; set; } = null!;

    [Required]
    [MaxLength(50)]
    public required string Provider { get; set; } = null!;

    [Required]
    [MaxLength(200)]
    public required string Model { get; set; } = null!;

    [MaxLength(500)]
    public string? BaseUrl { get; set; }

    [MaxLength(500)]
    public required string ApiKey { get; set; }

    public int MaxTokens { get; set; }

    public double? Temperature { get; set; }

    public double? TopP { get; set; }

    public int? TopK { get; set; }

    public double? FrequencyPenalty { get; set; }

    public double? PresencePenalty { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    [Key]
    public Guid Id { get; set; }
}