using System.ComponentModel.DataAnnotations;

namespace FableCraft.Infrastructure.Persistence.Entities;

public class LorebookEntry : IEntity, IChunkedEntity<LorebookEntryChunk>
{
    [Key]
    public Guid Id { get; init; }

    [Required]
    public Guid AdventureId { get; init; }

    public Adventure Adventure { get; init; } = null!;

    [Required]
    public string Description { get; init; } = null!;

    public int Priority { get; init; }

    [Required]
    public string Content { get; init; } = null!;

    [Required]
    public string Category { get; init; } = null!;

    public List<LorebookEntryChunk> Chunks { get; init; } = new();
}