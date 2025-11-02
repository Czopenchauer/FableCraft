using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FableCraft.Infrastructure.Persistence.Entities;

public class Scene : IKnowledgeGraphEntity, IEntity
{
    [Key]
    public Guid Id { get; init; }

    [Required]
    public Guid AdventureId { get; init; }

    [ForeignKey("WorldId")]
    public Adventure? Adventure { get; init; }

    public int SequenceNumber { get; init; }

    [Required]
    public required string NarrativeText { get; init; }

    [Column(TypeName = "jsonb")]
    public string? SceneStateJson { get; init; } = null!;

    [MaxLength(64)]
    public string? KnowledgeGraphNodeId { get; init; }

    public ProcessingStatus ProcessingStatus { get; init; }

    public DateTime CreatedAt { get; init; }

    public Guid? PreviousSceneId { get; init; }

    [ForeignKey("PreviousSceneId")]
    public Scene? PreviousScene { get; init; }

    public Guid? NextSceneId { get; init; }

    [ForeignKey("NextSceneId")]
    public Scene? NextScene { get; init; }

    public List<CharacterAction> CharacterActions { get; init; } = new();
}

public class Character : IKnowledgeGraphEntity, IEntity
{
    [Key]
    public Guid Id { get; init; }

    [Required]
    [MaxLength(200)]
    public required string Name { get; init; }

    [Required]
    public required string Description { get; init; }

    public required string Background { get; init; }

    [MaxLength(64)]
    public string? KnowledgeGraphNodeId { get; init; }

    public ProcessingStatus ProcessingStatus { get; init; }

    [Column(TypeName = "jsonb")]
    public string? StatsJson { get; init; } = null!;
}

public class CharacterAction : IEntity
{
    [Key]
    public Guid Id { get; init; }

    [Required]
    public Guid SceneId { get; init; }

    public required Scene Scene { get; init; }

    [Required]
    public required string ActionDescription { get; init; }

    public bool Selected { get; set; }
}