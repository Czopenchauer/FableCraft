namespace FableCraft.Infrastructure.Persistence.Entities.Adventure;

/// <summary>
///     A queued message for remote character communication with transit time.
///     Dispatches are queued when sent and delivered to recipients in their next processing cycle.
/// </summary>
public sealed class Dispatch : IEntity
{
    public Guid Id { get; set; }

    public Guid AdventureId { get; set; }

    /// <summary>
    ///     The character who sent this dispatch.
    /// </summary>
    public required string FromCharacter { get; set; }

    /// <summary>
    ///     The character who should receive this dispatch.
    /// </summary>
    public required string ToCharacter { get; set; }

    /// <summary>
    ///     How the message is being sent (e.g., "Street kid runner, paid two copper").
    /// </summary>
    public required string Method { get; set; }

    /// <summary>
    ///     In-world timestamp when the dispatch was sent (e.g., "15:00 05-06-845").
    /// </summary>
    public required string SentAt { get; set; }

    /// <summary>
    ///     Description of expected transit time (e.g., "Under an hour on foot").
    /// </summary>
    public required string EstimatedTransit { get; set; }

    /// <summary>
    ///     Private context from the sender about why they're sending this.
    ///     Stripped before delivery to recipient.
    /// </summary>
    public string? SenderContext { get; set; }

    /// <summary>
    ///     The scene beat the recipient experiences upon delivery.
    ///     This is what the recipient actually sees/hears/experiences.
    /// </summary>
    public required string WhatArrives { get; set; }

    /// <summary>
    ///     Current status of this dispatch in the queue.
    /// </summary>
    public DispatchStatus Status { get; set; } = DispatchStatus.Pending;

    /// <summary>
    ///     How the dispatch was resolved, if resolved.
    ///     Becomes a world fact if Discoverable is true.
    /// </summary>
    public string? Resolution { get; set; }

    /// <summary>
    ///     In-world timestamp when the dispatch was resolved.
    /// </summary>
    public string? ResolvedAt { get; set; }

    /// <summary>
    ///     If true, the resolution becomes a discoverable world fact.
    ///     If false, it only enters sender's and recipient's Character KGs.
    /// </summary>
    public bool Discoverable { get; set; }

    /// <summary>
    ///     UTC timestamp when this dispatch was created.
    /// </summary>
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>
///     Status of a dispatch in the queue.
/// </summary>
public enum DispatchStatus
{
    /// <summary>
    ///     Dispatch is queued and awaiting delivery based on transit time.
    /// </summary>
    Pending,

    /// <summary>
    ///     Dispatch has been delivered to the recipient's incoming queue.
    /// </summary>
    Delivered,

    /// <summary>
    ///     Dispatch has been resolved by the recipient.
    /// </summary>
    Resolved
}
