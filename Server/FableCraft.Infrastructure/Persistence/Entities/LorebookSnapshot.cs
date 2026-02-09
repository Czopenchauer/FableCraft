namespace FableCraft.Infrastructure.Persistence.Entities;

/// <summary>
/// Stores the indexed state of a lorebook for change detection and revert capability.
/// Created during indexing, used to detect modifications and restore original state.
/// </summary>
public class LorebookSnapshot : IEntity
{
    public Guid Id { get; set; }

    public Guid WorldbookId { get; set; }

    public Worldbook Worldbook { get; set; } = null!;

    public Guid LorebookId { get; set; }

    public required string Title { get; set; }

    public required string Content { get; set; }

    public required string Category { get; set; }

    public required ContentType ContentType { get; set; }

    public DateTimeOffset IndexedAt { get; set; }
}