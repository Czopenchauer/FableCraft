namespace FableCraft.Infrastructure.Llm;

/// <summary>
/// Thrown when all retry attempts have been exhausted during streaming.
/// </summary>
public class LlmStreamingFailedException : Exception
{
    public StreamingContext Context { get; }

    public LlmStreamingFailedException(StreamingContext context, Exception? innerException = null)
        : base($"LLM streaming failed after {context.RetryCount} retries. Partial response: {context.PartialResponse.Length} chars", innerException)
    {
        Context = context;
    }
}
