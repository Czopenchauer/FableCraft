namespace FableCraft.Infrastructure.Persistence.Entities;

public class GenerationProcess : IEntity
{
    public Guid Id { get; set; }

    public Guid AdventureId { get; set; }

    public required string Context { get; set; }
}