namespace FableCraft.Infrastructure.Persistence.Entities;

public class Lorebook : IEntity
{
    public Guid WorldbookId { get; set; }

    public Worldbook Worldbook { get; set; } = null!;

    public required string Title { get; set; }

    public required string Content { get; set; }

    public required string Category { get; set; }

    public required ContentType ContentType { get; set; }

    public Guid Id { get; set; }
}