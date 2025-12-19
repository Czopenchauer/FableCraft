namespace FableCraft.Infrastructure.Persistence.Entities.Adventure;

public sealed class Character : IEntity
{
    public Guid Id { get; set; }

    public Guid AdventureId { get; set; }

    public required string Name { get; set; }

    public int Version { get; set; }

    public List<CharacterState> CharacterStates { get; set; } = new();

    public List<CharacterRelationship> CharacterRelationships { get; set; } = new();

    public List<CharacterSceneRewrite> CharacterSceneRewrites { get; set; } = new();

    public List<CharacterMemory> CharacterMemories { get; set; } = new();
}