using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FableCraft.Infrastructure.Persistence.Entities;

public class Scene
{
    [Key]
    public Guid SceneId { get; init; }

    [Required]
    public Guid WorldId { get; init; }

    [ForeignKey("WorldId")]
    public World? World { get; init; }

    public int SequenceNumber { get; init; }

    [Required]
    public string NarrativeText { get; init; }

    [Column(TypeName = "jsonb")]
    public string SceneStateJson { get; init; }

    public string KnowledgeGraphNodeId { get; set; }

    public ProcessingStatus ProcessingStatus { get; init; }

    public DateTime CreatedAt { get; init; }

    public Guid? PreviousSceneId { get; init; }

    [ForeignKey("PreviousSceneId")]
    public Scene? PreviousScene { get; init; }

    public Guid? NextSceneId { get; init; }

    [ForeignKey("NextSceneId")]
    public Scene? NextScene { get; init; }

    public ICollection<CharacterAction> CharacterActions { get; init; }
}

public class Character
{
    [Key]
    public Guid CharacterId { get; init; }

    [Required]
    [MaxLength(200)]
    public string Name { get; init; }

    [Required]
    public string Description { get; init; }

    public string Background { get; init; }

    public string KnowledgeGraphNodeId { get; set; }

    public ProcessingStatus ProcessingStatus { get; init; }

    [Column(TypeName = "jsonb")]
    public string StatsJson { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime LastUpdatedAt { get; init; }
}

public class CharacterAction
{
    [Key]
    public Guid ActionId { get; init; }

    [Required]
    public Guid SceneId { get; init; }

    public Scene Scene { get; init; }

    public string KnowledgeGraphNodeId { get; set; }

    public ProcessingStatus ProcessingStatus { get; init; }

    [Required]
    public string ActionDescription { get; init; }

    public bool Selected { get; set; }

    public DateTime CreatedAt { get; init; }
}