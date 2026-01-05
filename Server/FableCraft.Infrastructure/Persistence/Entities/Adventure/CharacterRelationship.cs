namespace FableCraft.Infrastructure.Persistence.Entities.Adventure;

/// <summary>
/// Represents a character's subjective view of their relationship with another character.
/// This is one-sided: Character A's view of Character B may differ from B's view of A.
/// Each update creates a new row to preserve history.
/// </summary>
public sealed class CharacterRelationship : IEntity
{
    public Guid Id { get; set; }

    /// <summary>
    /// The character who holds this view of the relationship.
    /// </summary>
    public Guid CharacterId { get; set; }

    /// <summary>
    /// The name of the character this relationship is about.
    /// </summary>
    public required string TargetCharacterName { get; set; }

    /// <summary>
    /// The scene where this relationship state was recorded.
    /// </summary>
    public Guid SceneId { get; set; }

    public Scene Scene { get; set; } = null!;

    public required string? UpdateTime { get; set; }

    /// <summary>
    /// The sequence number when this relationship was created/updated.
    /// Used for querying the latest relationship at a specific state.
    /// </summary>
    public int SequenceNumber { get; set; }

    /// <summary>
    /// 2-4 sentences: How they feel about this person and why. The emotional reality of the relationship. Include warmth, tension, history, unresolved feelings - whatever is true.
    /// </summary>
    public object? Dynamic { get; set; }

    /// <summary>
    /// JSON string containing the full relationship data (feelings, trust level, history, etc.)
    /// </summary>
    public required IDictionary<string, object> Data { get; set; }
}