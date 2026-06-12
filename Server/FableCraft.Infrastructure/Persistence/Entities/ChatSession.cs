using System.ComponentModel.DataAnnotations;

namespace FableCraft.Infrastructure.Persistence.Entities;

public class ChatSession : IEntity
{
    public Guid AdventureId { get; set; }

    public Adventure.Adventure Adventure { get; set; } = null!;

    public Guid LlmPresetId { get; set; }

    public LlmPreset LlmPreset { get; set; } = null!;

    [Required]
    [MaxLength(500)]
    public required string Title { get; set; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public List<ChatMessage> Messages { get; init; } = [];

    [Key]
    public Guid Id { get; set; }
}