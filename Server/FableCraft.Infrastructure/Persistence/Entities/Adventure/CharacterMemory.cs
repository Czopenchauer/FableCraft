namespace FableCraft.Infrastructure.Persistence.Entities.Adventure;

/// <summary>
/// Represents a memory belonging to a character, created after a scene.
/// Memories are linked to a specific sequence number to enable historical retrieval.
/// </summary>
public sealed class CharacterMemory : IEntity
{
    public Guid Id { get; set; }

    /// <summary>
    /// The character who owns this memory.
    /// </summary>
    public Guid CharacterId { get; set; }

    /// <summary>
    /// The scene where this memory was created.
    /// </summary>
    public Guid SceneId { get; set; }

    public Scene Scene { get; set; } = null!;

    public required SceneTracker SceneTracker { get; set; }

    /// <summary>
    /// Short summary of the memory.
    /// </summary>
    public required string Summary { get; set; }

    /// <summary>
    /// Importance score (0 - 10). Higher salience memories persist longer.
    /// Betrayals, promises, intimate moments = high salience.
    /// </summary>
    public double Salience { get; set; }

    /// <summary>
    /// JSON string containing additional memory data (entities, emotional_tone, etc.)
    /// </summary>
    public IDictionary<string, object>? Data { get; set; }
}