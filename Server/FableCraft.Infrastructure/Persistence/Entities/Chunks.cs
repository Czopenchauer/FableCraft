namespace FableCraft.Infrastructure.Persistence.Entities;

public class Chunk : IEntity
{
    public Guid EntityId { get; init; }

    public required string Description { get; set; }

    public required string RawChunk { get; set; } = null!;

    /// <summary>
    ///     Describe the chunk in the context of the overall text document it belongs to.
    /// </summary>
    public string? ContextualizedChunk { get; set; }

    public required int Order { get; set; }

    public string? KnowledgeGraphNodeId { get; set; }

    public ProcessingStatus ProcessingStatus { get; set; }

    public Guid Id { get; set; }
}