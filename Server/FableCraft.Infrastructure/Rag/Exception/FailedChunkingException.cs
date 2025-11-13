namespace FableCraft.Infrastructure.Rag.Exception;

internal sealed class FailedChunkingException : System.Exception
{
    public FailedChunkingException(IEnumerable<Guid> entitiesId) : base($"Failed to chunk content for entities {string.Join(";", entitiesId)}.")
    {
    }
}