using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FableCraft.Infrastructure.Persistence.Entities;

public class Scene : IKnowledgeGraphEntity
{
    [Required]
    public Guid AdventureId { get; init; }

    public Adventure? Adventure { get; init; }

    public required int SequenceNumber { get; init; }

    [Required]
    public required string NarrativeText { get; init; }

    [Column(TypeName = "jsonb")]
    public string? SceneStateJson { get; init; } = null!;

    public DateTime CreatedAt { get; init; }

    public List<CharacterAction> CharacterActions { get; init; } = new();

    [Key]
    public Guid Id { get; set; }

    public string GetContent()
    {
        return $"{NarrativeText}\n{CharacterActions.FirstOrDefault(x => x.Selected)?.ActionDescription ?? string.Empty}".Trim();
    }

    public string GetContentDescription()
    {
        return $"Scene number {SequenceNumber}";
    }
}