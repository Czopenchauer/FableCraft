using System.ComponentModel.DataAnnotations;

namespace FableCraft.Infrastructure.Persistence.Entities;

public class ProjectFile : IEntity
{
    public Guid ProjectId { get; set; }

    public Project Project { get; set; } = null!;

    [Required]
    [MaxLength(500)]
    public required string Name { get; set; }

    [MaxLength(100)]
    public string? Category { get; set; }

    public string? ContentHash { get; set; }

    public string? IndexedContentHash { get; set; }

    public string? KnowledgeGraphNodeId { get; set; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? UpdatedAt { get; set; }

    [Key]
    public Guid Id { get; set; }
}