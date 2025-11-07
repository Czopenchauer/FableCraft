namespace FableCraft.Infrastructure.Persistence.Entities;

public class Chunk : IEntity
{
    public Guid Id { get; set; }

    public Guid EntityId { get; init; }

    public required string Name { get; set; }

    public required string Description { get; set; }

    public required string RawChunk { get; set; } = null!;

    /// <summary>
    /// Describe the chunk in the context of the overall text document it belongs to.
    /// </summary>
    public string? ContextualizedChunk { get; set; }

    public required int Order { get; set; }

    public string? KnowledgeGraphNodeId { get; init; }

    public ProcessingStatus ProcessingStatus { get; set; }
}