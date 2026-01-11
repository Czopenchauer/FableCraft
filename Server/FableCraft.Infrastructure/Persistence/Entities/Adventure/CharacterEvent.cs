namespace FableCraft.Infrastructure.Persistence.Entities.Adventure;

/// <summary>
///     Represents an event logged by an arc_important character when interacting with a significant character.
///     These events are consumed by OffscreenInference to derive the significant character's current state.
///     Events are deleted after consumption - no accumulation.
/// </summary>
public sealed class CharacterEvent : IEntity
{
    /// <summary>
    ///     The adventure this event belongs to.
    /// </summary>
    public Guid AdventureId { get; set; }

    /// <summary>
    ///     Name of the significant character who was affected by this event.
    /// </summary>
    public required string TargetCharacterName { get; set; }

    /// <summary>
    ///     Name of the arc_important character who logged this event during their simulation.
    /// </summary>
    public required string SourceCharacterName { get; set; }

    /// <summary>
    ///     In-world time when the event occurred.
    /// </summary>
    public required string Time { get; set; }

    /// <summary>
    ///     What happened from the target character's perspective.
    /// </summary>
    public required string Event { get; set; }

    /// <summary>
    ///     The source character's interpretation of how this affected the target (may be biased/wrong).
    /// </summary>
    public required string SourceRead { get; set; }

    /// <summary>
    ///     Whether this event has been consumed by OffscreenInference.
    ///     Events are typically deleted after consumption, but this flag can be used for soft-delete.
    /// </summary>
    public bool Consumed { get; set; }

    public Guid Id { get; set; }
}