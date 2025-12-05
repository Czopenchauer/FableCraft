using System.ComponentModel.DataAnnotations;

namespace FableCraft.Infrastructure.Persistence.Entities.Adventure;

public class Adventure : IEntity
{
    [Required]
    [MaxLength(200)]
    public string Name { get; init; } = null!;

    [Required]
    public required string FirstSceneGuidance { get; init; }

    public required string AdventureStartTime { get; set; }

    public ProcessingStatus RagProcessingStatus { get; set; }

    public ProcessingStatus SceneGenerationStatus { get; set; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? LastPlayedAt { get; init; }

    public required string? AuthorNotes { get; init; }

    public required MainCharacter MainCharacter { get; set; }

    public List<Character> Characters { get; init; } = [];

    public required TrackerStructure TrackerStructure { get; init; }

    public required List<LorebookEntry> Lorebook { get; init; }

    public List<Scene> Scenes { get; init; } = [];

    public Guid? FastPresetId { get; set; }

    public LlmPreset? FastPreset { get; set; }

    public Guid? ComplexPresetId { get; set; }

    public LlmPreset? ComplexPreset { get; set; }

    [Key]
    public Guid Id { get; set; }
}