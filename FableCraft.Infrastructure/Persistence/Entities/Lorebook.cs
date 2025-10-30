using System.ComponentModel.DataAnnotations;

namespace FableCraft.Infrastructure.Persistence.Entities;

public class World : IKnowledgeGraphEntity
{
    [Key]
    public Guid WorldId { get; init; }

    [Required]
    [MaxLength(200)]
    public string Name { get; init; }

    [Required]
    public string Backstory { get; init; }

    [Required]
    public string UniverseBackstory { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime LastPlayedAt { get; init; }

    public ProcessingStatus ProcessingStatus { get; init; }

    public string KnowledgeGraphNodeId { get; init; }

    public Guid CharacterId { get; init; }

    public Character Character { get; init; }

    public ICollection<LorebookEntry> Lorebook { get; init; }

    public ICollection<Scene> Scenes { get; init; }
}

public class LorebookEntry : IKnowledgeGraphEntity
{
    [Key]
    public Guid EntryId { get; init; }

    [Required]
    public Guid WorldId { get; init; }

    public World World { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; init; }

    [Required]
    public string Content { get; init; }

    [Required]
    [MaxLength(100)]
    public string Category { get; init; }

    public string KnowledgeGraphNodeId { get; init; }

    public ProcessingStatus ProcessingStatus { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime LastUpdatedAt { get; init; }
}