using System.ComponentModel.DataAnnotations;

namespace FableCraft.Infrastructure.Persistence.Entities;

public class Adventure : IKnowledgeGraphEntity, IEntity
{
    [Key]
    public Guid Id { get; init; }

    [Required]
    [MaxLength(200)]
    public string Name { get; init; } = null!;

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset LastPlayedAt { get; init; }

    public ProcessingStatus ProcessingStatus { get; init; }

    [MaxLength(64)]
    public string? KnowledgeGraphNodeId { get; init; }

    [MaxLength(5000)]
    public required string? AuthorNotes { get; init; }

    public Guid CharacterId { get; init; }

    public required Character Character { get; init; }

    public required ICollection<LorebookEntry> Lorebook { get; init; }

    public required ICollection<Scene> Scenes { get; init; }
}

public class LorebookEntry : IKnowledgeGraphEntity, IEntity
{
    [Key]
    public Guid Id { get; init; }

    [Required]
    public Guid AdventureId { get; init; }

    public Adventure Adventure { get; init; } = null!;

    [Required]
    [MaxLength(200)]
    public string Description { get; init; } = null!;

    public int Priority { get; init; }

    [Required]
    public string Content { get; init; } = null!;

    [Required]
    [MaxLength(100)]
    public string Category { get; init; } = null!;

    [MaxLength(64)]
    public string? KnowledgeGraphNodeId { get; init; }

    public ProcessingStatus ProcessingStatus { get; init; }
}