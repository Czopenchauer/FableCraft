namespace FableCraft.Infrastructure.Persistence.Entities;

public interface IEntity
{
    Guid Id { get; init; }
}

public interface IChunkedEntity<TEntity> where TEntity : ChunkBase
{
    List<TEntity> Chunks { get; init; }
}

public static class ChunkedEntityExtensions
{
    public static ProcessingStatus GetProcessingStatus<TEntity>(this IChunkedEntity<TEntity> entity) where TEntity : ChunkBase
    {
        if(entity.Chunks.Count == 0)
        {
            return ProcessingStatus.Pending;
        }

        if (entity.Chunks.Any(x => x.ProcessingStatus == ProcessingStatus.Failed))
        {
            return ProcessingStatus.Failed;
        }
        
        return entity.Chunks.All(c => c.ProcessingStatus == ProcessingStatus.Completed) ? ProcessingStatus.Completed : ProcessingStatus.InProgress;
    }
}