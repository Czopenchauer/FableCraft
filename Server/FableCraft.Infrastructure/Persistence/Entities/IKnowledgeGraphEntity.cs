using System.Diagnostics.CodeAnalysis;

namespace FableCraft.Infrastructure.Persistence.Entities;

public interface IKnowledgeGraphEntity
{
    string? KnowledgeGraphNodeId { get; init; }

    ProcessingStatus ProcessingStatus { get; set; }

    [MemberNotNullWhen(true, nameof(KnowledgeGraphNodeId))]
    public bool HasKnowledgeGraph() => ProcessingStatus == ProcessingStatus.Completed;
}