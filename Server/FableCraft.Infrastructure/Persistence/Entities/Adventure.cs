using System.ComponentModel.DataAnnotations;

namespace FableCraft.Infrastructure.Persistence.Entities;

public class Adventure : IEntity
{
    [Key]
    public Guid Id { get; init; }

    [Required]
    [MaxLength(200)]
    public string Name { get; init; } = null!;

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? LastPlayedAt { get; init; }

    [MaxLength(5000)]
    public required string? AuthorNotes { get; init; }

    public Guid CharacterId { get; init; }

    public required Character Character { get; init; }

    public required ICollection<LorebookEntry> Lorebook { get; init; }

    public required ICollection<Scene> Scenes { get; init; }
}