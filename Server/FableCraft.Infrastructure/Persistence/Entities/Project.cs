using System.ComponentModel.DataAnnotations;

namespace FableCraft.Infrastructure.Persistence.Entities;

public class Project : IEntity
{
    [Required]
    [MaxLength(200)]
    public required string Name { get; set; }

    public string? Description { get; set; }

    public Guid? GraphRagSettingsId { get; set; }

    public GraphRagSettings? GraphRagSettings { get; set; }

    public Guid? LlmPresetId { get; set; }

    public LlmPreset? LlmPreset { get; set; }

    public IndexingStatus IndexingStatus { get; set; } = IndexingStatus.NotIndexed;

    public DateTimeOffset? LastIndexedAt { get; set; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public List<ProjectFile> Files { get; set; } = [];

    public List<ProjectChatSession> ChatSessions { get; set; } = [];

    [Key]
    public Guid Id { get; set; }
}