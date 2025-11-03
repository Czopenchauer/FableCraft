namespace FableCraft.Infrastructure.Persistence.Entities;

public interface IEntity
{
    Guid Id { get; init; }
}

public interface IChunkedEntity<TEntity> where TEntity : ChunkBase
{
    List<TEntity> Chunks { get; init; }

    bool IsProcessed() => Chunks.Count > 0 && Chunks.All(c => c.ProcessingStatus == ProcessingStatus.Completed);
}