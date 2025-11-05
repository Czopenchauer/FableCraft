namespace FableCraft.Infrastructure.Persistence.Entities;

public class Chunk : IEntity
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