namespace FableCraft.Infrastructure.Persistence.Entities;

public class Worldbook : IEntity
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public List<Lorebook> Lorebooks { get; set; } = new List<Lorebook>();
}