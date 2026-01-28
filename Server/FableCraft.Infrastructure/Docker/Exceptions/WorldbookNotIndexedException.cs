namespace FableCraft.Infrastructure.Docker.Exceptions;

public sealed class WorldbookNotIndexedException : Exception
{
    public WorldbookNotIndexedException(Guid worldbookId) : base($"Worldbook {worldbookId} has not been indexed. Run indexing first.")
    {
    }
}