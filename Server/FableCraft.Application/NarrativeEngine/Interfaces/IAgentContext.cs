using FableCraft.Application.NarrativeEngine.Models;

namespace FableCraft.Application.NarrativeEngine.Interfaces;

/// <summary>
/// Shared context available to all agents
/// </summary>
public interface IAgentContext
{
    /// <summary>
    /// Last 20 scenes for narrative context
    /// </summary>
    List<SceneContext> RecentScenes { get; }

    /// <summary>
    /// Pre-created story beats with progress tracking
    /// </summary>
    List<StoryBeat> StoryBeats { get; }

    /// <summary>
    /// Current narrative arc position
    /// </summary>
    string CurrentArcPosition { get; }

    /// <summary>
    /// Pacing history
    /// </summary>
    PacingHistory PacingHistory { get; }

    /// <summary>
    /// World consistency guidelines
    /// </summary>
    Dictionary<string, string> WorldConsistencyGuidelines { get; }

    /// <summary>
    /// Genre conventions
    /// </summary>
    List<string> GenreConventions { get; }
}

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
