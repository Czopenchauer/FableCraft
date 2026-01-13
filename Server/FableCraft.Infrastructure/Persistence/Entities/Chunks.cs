namespace FableCraft.Infrastructure.Persistence.Entities;

public class Chunk : IEntity
{
    public required Guid EntityId { get; init; }

    public required Guid? AdventureId { get; set; }

    public required string Name { get; set; }

    public required ulong ContentHash { get; set; }

    public required string Path { get; set; }

    public required string Content { get; set; }

    public required string ContentType { get; set; }

    public required string DatasetName { get; set; }

    public required string? KnowledgeGraphNodeId { get; set; }

    public Guid Id { get; set; }
}

public class ChunkLocation
{
    public required string DatasetName { get; set; }

    public required string? KnowledgeGraphNodeId { get; set; }
}