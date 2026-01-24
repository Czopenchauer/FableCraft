namespace FableCraft.Infrastructure.Llm;

/// <summary>
/// Tracks the state of a streaming LLM response, enabling resume from partial responses.
/// </summary>
public class StreamingContext
{
    /// <summary>
    /// The accumulated response content received so far.
    /// </summary>
    public string PartialResponse { get; set; } = string.Empty;

    /// <summary>
    /// Number of streaming chunks received.
    /// </summary>
    public int ChunksReceived { get; set; }

    /// <summary>
    /// When the streaming request started.
    /// </summary>
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// When the last chunk was received.
    /// </summary>
    public DateTimeOffset? LastChunkAt { get; set; }

    /// <summary>
    /// Number of retry attempts made due to connection interruptions.
    /// </summary>
    public int RetryCount { get; set; }

    /// <summary>
    /// Whether the streaming completed successfully.
    /// </summary>
    public bool IsComplete { get; set; }
}
