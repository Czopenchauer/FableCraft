using System.ComponentModel.DataAnnotations;

namespace FableCraft.Infrastructure.Persistence.Entities;

public class ProjectChatSession : IEntity
{
    public Guid ProjectId { get; set; }

    public Project Project { get; set; } = null!;

    public Guid LlmPresetId { get; set; }

    public LlmPreset LlmPreset { get; set; } = null!;

    [Required]
    [MaxLength(500)]
    public required string Title { get; set; }

    public string? ChatHistoryJson { get; set; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? UpdatedAt { get; set; }

    [Key]
    public Guid Id { get; set; }
}