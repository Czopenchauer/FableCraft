namespace FableCraft.Application.NarrativeEngine.Interfaces;

/// <summary>
/// Knowledge graph search functionality
/// </summary>
public interface IKnowledgeGraphPlugin
{
    /// <summary>
    /// Search the knowledge graph for entities, relationships, and narrative data
    /// </summary>
    Task<string> SearchKnowledgeGraphAsync(
        string query,
        CancellationToken cancellationToken = default);
}
