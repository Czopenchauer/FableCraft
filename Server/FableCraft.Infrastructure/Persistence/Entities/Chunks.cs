namespace FableCraft.Infrastructure.Persistence.Entities;

public abstract class ChunkBase : IEntity
{
    public Guid Id { get; init; }

    public Guid EntityId { get; init; }

    public required string Name { get; set; }

    public required string Description { get; set; }

    public required string RawChunk { get; set; } = null!;

    public string? ContextualizedChunk { get; set; }

    public string? KnowledgeGraphNodeId { get; init; }

    public ProcessingStatus ProcessingStatus { get; set; }
}

public class LorebookEntryChunk : ChunkBase
{
    public LorebookEntry LorebookEntry { get; init; } = null!;
}

public class CharacterChunk : ChunkBase
{
    public Character Character { get; init; } = null!;
}

public class SceneChunk : ChunkBase
{
    public Scene Scene { get; init; } = null!;
}