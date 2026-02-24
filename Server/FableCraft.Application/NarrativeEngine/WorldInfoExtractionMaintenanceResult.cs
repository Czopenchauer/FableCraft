namespace FableCraft.Application.NarrativeEngine;

/// <summary>
///     Result of world info extraction maintenance operation for an adventure.
/// </summary>
public sealed class WorldInfoExtractionResult
{
    public required Guid AdventureId { get; init; }

    public required int TotalScenesProcessed { get; init; }

    public required int SuccessCount { get; init; }

    public required List<SceneWorldInfoResult> SceneResults { get; init; }
}

/// <summary>
///     Result of world info extraction for a single scene.
/// </summary>
public sealed class SceneWorldInfoResult
{
    public required Guid SceneId { get; init; }

    public required int SequenceNumber { get; init; }

    public required bool Success { get; init; }

    public int ActivityCount { get; init; }

    /// <summary>
    ///     Number of CharacterSceneRewrites processed for this scene.
    /// </summary>
    public int CharacterRewritesProcessed { get; init; }

    public string? ErrorMessage { get; init; }
}

/// <summary>
///     Result of committing Activity lorebooks to the Knowledge Graph.
/// </summary>
public sealed class ActivityCommitResult
{
    public required Guid AdventureId { get; init; }

    public required int TotalActivitiesFound { get; init; }

    public required int AlreadyCommitted { get; init; }

    public required int NewlyCommitted { get; init; }

    public required bool Success { get; init; }

    public string? ErrorMessage { get; init; }
}
