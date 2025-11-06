using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FableCraft.Infrastructure.Persistence.Entities;

public class Scene : IEntity
{
    [Key]
    public Guid Id { get; init; }

    [Required]
    public Guid AdventureId { get; init; }

    public Adventure? Adventure { get; init; }

    public int SequenceNumber { get; init; }

    [Required]
    public required string NarrativeText { get; init; }

    [Column(TypeName = "jsonb")]
    public string? SceneStateJson { get; init; } = null!;

    public DateTime CreatedAt { get; init; }

    public List<CharacterAction> CharacterActions { get; init; } = new();
}