using System.Diagnostics.CodeAnalysis;

namespace FableCraft.Infrastructure.Persistence.Entities;

public enum ProcessingStatus
{
    Pending,
    InProgress,
    Completed,
    Failed
}

public interface IEntity
{
    Guid Id { get; init; }
}

public interface IKnowledgeGraphEntity
{
    string? KnowledgeGraphNodeId { get; init; }

    ProcessingStatus ProcessingStatus { get; init; }

    [MemberNotNullWhen(true, nameof(KnowledgeGraphNodeId))]
    public bool HasKnowledgeGraph() => ProcessingStatus == ProcessingStatus.Completed;
}