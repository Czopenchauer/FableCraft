namespace FableCraft.Infrastructure.Persistence.Entities.Adventure;

/// <summary>
/// Stores the full character-POV prose rewrite of a scene.
/// Created by the Character Reflection Agent post-scene.
/// Will later be added to Knowledge Graph for semantic search.
/// </summary>
public sealed class CharacterSceneRewrite : IEntity
{
    public Guid Id { get; set; }

    /// <summary>
    /// The character whose POV this rewrite represents.
    /// </summary>
    public Guid CharacterId { get; set; }

    /// <summary>
    /// The scene this rewrite is based on.
    /// </summary>
    public Guid SceneId { get; set; }

    public Scene Scene { get; set; } = null!;

    /// <summary>
    /// The sequence number of the scene.
    /// </summary>
    public int SequenceNumber { get; set; }

    public required StoryTracker StoryTracker { get; set; }

    /// <summary>
    /// Full prose rewrite of the scene from the character's perspective.
    /// Includes their thoughts, feelings, knowledge, and reactions.
    /// </summary>
    public required string Content { get; set; }
}