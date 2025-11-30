using System.ComponentModel.DataAnnotations;

namespace FableCraft.Infrastructure.Persistence.Entities.Adventure;

public class Adventure : IEntity
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; init; } = null!;

    [Required]
    public required string FirstSceneGuidance { get; init; }

    public required string AdventureStartTime { get; set; }

    public ProcessingStatus ProcessingStatus { get; set; } = ProcessingStatus.Pending;

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? LastPlayedAt { get; init; }

    public required string? AuthorNotes { get; init; }

    public required MainCharacter MainCharacter { get; set; }

    public List<Character> Characters { get; init; } = [];

    public required TrackerStructure TrackerStructure { get; init; }

    public required List<LorebookEntry> Lorebook { get; init; }

    public List<Scene> Scenes { get; init; } = [];
}