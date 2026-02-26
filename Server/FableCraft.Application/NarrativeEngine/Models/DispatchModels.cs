using System.Text.Json.Serialization;

namespace FableCraft.Application.NarrativeEngine.Models;

/// <summary>
///     A dispatch being sent by a character to communicate with someone not present.
/// </summary>
internal sealed class OutgoingDispatch
{
    /// <summary>
    ///     The recipient character name.
    /// </summary>
    [JsonPropertyName("to")]
    public required string To { get; init; }

    /// <summary>
    ///     How the message is being sent (e.g., "Street kid runner, paid two copper").
    /// </summary>
    [JsonPropertyName("method")]
    public required string Method { get; init; }

    /// <summary>
    ///     In-world timestamp when the dispatch was sent.
    /// </summary>
    [JsonPropertyName("sent_at")]
    public required string SentAt { get; init; }

    /// <summary>
    ///     Description of expected transit time (e.g., "Under an hour on foot").
    /// </summary>
    [JsonPropertyName("estimated_transit")]
    public required string EstimatedTransit { get; init; }

    /// <summary>
    ///     Private context about why this dispatch is being sent.
    ///     Stripped before delivery to recipient.
    /// </summary>
    [JsonPropertyName("sender_context")]
    public string? SenderContext { get; init; }

    /// <summary>
    ///     The scene beat the recipient experiences upon delivery.
    /// </summary>
    [JsonPropertyName("what_arrives")]
    public required string WhatArrives { get; init; }
}

/// <summary>
///     A dispatch that has arrived for a character.
///     SenderContext is stripped - recipient only sees what actually arrives.
/// </summary>
internal sealed class IncomingDispatch
{
    /// <summary>
    ///     Unique identifier for resolving this dispatch.
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    /// <summary>
    ///     Who sent this dispatch.
    /// </summary>
    [JsonPropertyName("from")]
    public required string From { get; init; }

    /// <summary>
    ///     How the message arrived (messenger type, delivery method).
    /// </summary>
    [JsonPropertyName("method")]
    public required string Method { get; init; }

    /// <summary>
    ///     When the dispatch was sent.
    /// </summary>
    [JsonPropertyName("sent_at")]
    public required string SentAt { get; init; }

    /// <summary>
    ///     Expected transit time.
    /// </summary>
    [JsonPropertyName("estimated_transit")]
    public required string EstimatedTransit { get; init; }

    /// <summary>
    ///     What the recipient experiences (the delivered content).
    /// </summary>
    [JsonPropertyName("what_arrives")]
    public required string WhatArrives { get; init; }
}

/// <summary>
///     Combined dispatch output block from Writer agent.
/// </summary>
internal sealed class DispatchOutput
{
    [JsonPropertyName("dispatches")]
    public List<OutgoingDispatch>? Dispatches { get; init; }

    [JsonPropertyName("dispatches_resolved")]
    public List<DispatchResolution>? DispatchesResolved { get; init; }
}

/// <summary>
///     Resolution of a received dispatch.
/// </summary>
internal sealed class DispatchResolution
{
    /// <summary>
    ///     ID of the dispatch being resolved.
    /// </summary>
    [JsonPropertyName("dispatch_id")]
    public required string DispatchId { get; init; }

    /// <summary>
    ///     In-world time when the dispatch was resolved.
    /// </summary>
    [JsonPropertyName("time")]
    public required string Time { get; init; }

    /// <summary>
    ///     If true, the resolution becomes a discoverable world fact.
    ///     If false, it only enters sender's and recipient's Character KGs.
    /// </summary>
    [JsonPropertyName("discoverable")]
    public bool Discoverable { get; init; }

    /// <summary>
    ///     What happened when the dispatch was resolved.
    /// </summary>
    [JsonPropertyName("resolution")]
    public required string Resolution { get; init; }
}
