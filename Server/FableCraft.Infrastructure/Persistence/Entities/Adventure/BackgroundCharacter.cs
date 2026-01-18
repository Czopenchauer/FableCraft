namespace FableCraft.Infrastructure.Persistence.Entities.Adventure;

public sealed class BackgroundCharacter : IEntity
{
    public Guid AdventureId { get; set; }

    public required Guid SceneId { get; set; }

    public Scene Scene { get; set; } = null!;

    public required string Name { get; set; }

    public required string Identity { get; set; }

    public required string Description { get; set; }

    public required string LastLocation { get; set; }

    public required string LastSeenTime { get; set; }

    public required bool ConvertedToFull { get; set; }

    public int Version { get; set; }

    public Guid Id { get; set; }
}