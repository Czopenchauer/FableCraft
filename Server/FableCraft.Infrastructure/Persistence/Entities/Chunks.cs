namespace FableCraft.Infrastructure.Persistence.Entities;

public class Chunk : IEntity
{
    public Guid Id { get; set; }

    public required Guid EntityId { get; init; }

    public required string Name { get; set; }

    public required string RawChunk { get; set; } = null!;

    public required string ContentType { get; set; }

    public required DateTime ReferenceTime { get; set; }

    /// <summary>
    ///     Describe the chunk in the context of the overall text document it belongs to.
    /// </summary>
    public string? ContextualizedChunk { get; set; }

    public int Order { get; set; }

    public string? KnowledgeGraphNodeId { get; set; }

    public ProcessingStatus ProcessingStatus { get; set; }

    public string GetContent()
    {
        return string.IsNullOrEmpty(ContextualizedChunk) ? RawChunk : $"{ContextualizedChunk}\n\n{RawChunk}";
    }
}