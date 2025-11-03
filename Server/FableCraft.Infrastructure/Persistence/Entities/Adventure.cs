using System.ComponentModel.DataAnnotations;

namespace FableCraft.Infrastructure.Persistence.Entities;

public class Adventure : IEntity
{
    [Key]
    public Guid Id { get; init; }

    [Required]
    [MaxLength(200)]
    public string Name { get; init; } = null!;

    [Required]
    [MaxLength(5000)]
    public required string FirstSceneGuidance { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? LastPlayedAt { get; init; }

    [MaxLength(5000)]
    public required string? AuthorNotes { get; init; }

    public Guid CharacterId { get; init; }

    public required Character Character { get; init; }

    public required List<LorebookEntry> Lorebook { get; init; }

    public List<Scene> Scenes { get; init; } = [];
}