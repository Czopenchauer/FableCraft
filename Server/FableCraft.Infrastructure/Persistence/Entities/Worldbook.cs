namespace FableCraft.Infrastructure.Persistence.Entities;

public class Worldbook : IEntity
{
    public required string Name { get; set; }

    public List<Lorebook> Lorebooks { get; set; } = new();

    public Guid Id { get; set; }
}