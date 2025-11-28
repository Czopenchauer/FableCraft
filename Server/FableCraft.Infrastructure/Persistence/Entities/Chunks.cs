namespace FableCraft.Infrastructure.Persistence.Entities;

public class Chunk : IEntity
{
    public Guid Id { get; set; }

    public required Guid EntityId { get; init; }

    public required string Name { get; set; }

    public required ulong ContentHash { get; set; }

    public required string Path { get; set; }

    public required string ContentType { get; set; }

    public required string? KnowledgeGraphNodeId { get; set; }
}