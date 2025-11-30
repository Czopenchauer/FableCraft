namespace FableCraft.Infrastructure.Persistence.Entities.Adventure;

public sealed class MainCharacter : IEntity
{
    public Guid Id { get; set; }

    public Guid AdventureId { get; set; }

    public required string Name { get; init; }

    public required string Description { get; init; }
}