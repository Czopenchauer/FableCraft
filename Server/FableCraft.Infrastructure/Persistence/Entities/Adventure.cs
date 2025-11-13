using System.ComponentModel.DataAnnotations;

namespace FableCraft.Infrastructure.Persistence.Entities;

public class Adventure : IEntity
{
    [Required]
    [MaxLength(200)]
    public string Name { get; init; } = null!;

    [Required]
    public required string FirstSceneGuidance { get; init; }

    public ProcessingStatus ProcessingStatus { get; set; } = ProcessingStatus.Pending;

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? LastPlayedAt { get; init; }

    public required string? AuthorNotes { get; init; }

    public Guid CharacterId { get; init; }

    public required Character Character { get; init; }

    public required List<LorebookEntry> Lorebook { get; init; }

    public List<Scene> Scenes { get; init; } = [];

    [Key]
    public Guid Id { get; set; }
}