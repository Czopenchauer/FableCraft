using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FableCraft.Infrastructure.Persistence.Entities;

public class Adventure : IEntity
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; init; } = null!;

    [Required]
    public required string FirstSceneGuidance { get; init; }

    public ProcessingStatus ProcessingStatus { get; set; } = ProcessingStatus.Pending;

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? LastPlayedAt { get; init; }

    public required string? AuthorNotes { get; init; }

    public string? Summary { get; set; }

    public required MainCharacter MainCharacter { get; set; }

    public Character[] Characters { get; init; }

    [Column(TypeName = "jsonb")]
    public required TrackerStructure TrackerStructure { get; init; }

    public required List<LorebookEntry> Lorebook { get; init; }

    public List<Scene> Scenes { get; init; } = [];
}