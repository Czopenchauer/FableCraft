namespace FableCraft.Infrastructure.Persistence.Entities;

public class Lorebook : IEntity
{
    public Guid Id { get; set; }

    public required Guid WorldbookId { get; set; }

    public Worldbook Worldbook { get; set; } = null!;

    public required string Title { get; set; }

    public required string Content { get; set; }

    public required string Category { get; set; }
}