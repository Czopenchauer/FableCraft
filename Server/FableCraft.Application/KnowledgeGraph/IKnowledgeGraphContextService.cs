namespace FableCraft.Application.KnowledgeGraph;

/// <summary>
/// Result of a knowledge graph indexing operation.
/// </summary>
public sealed record IndexingResult(bool Success, string? Error = null);

/// <summary>
/// Manages knowledge graph operations with volume-based isolation.
///
/// IMPORTANT: Only ONE operation can run at a time because the underlying
/// graph service container can only mount one volume. All operations
/// acquire an exclusive lock for their entire duration.
/// </summary>
public interface IKnowledgeGraphContextService
{
    /// <summary>
    /// Indexes a worldbook's lorebook entries into a template knowledge graph volume.
    /// This is an expensive one-time operation. Adventures copy this template.
    ///
    /// Acquires exclusive lock for the entire operation duration.
    /// </summary>
    /// <param name="worldbookId">Worldbook to index.</param>
    /// <param name="lorebookEntries">Entries to index.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<IndexingResult> IndexWorldbookAsync(
        Guid worldbookId,
        IReadOnlyList<LorebookIndexEntry> lorebookEntries,
        CancellationToken ct = default);

    /// <summary>
    /// Creates an adventure's knowledge graph by copying a worldbook template
    /// and adding the main character data plus any extra lore entries.
    ///
    /// Acquires exclusive lock for the entire operation duration.
    /// </summary>
    /// <param name="adventureId">Adventure to initialize.</param>
    /// <param name="worldbookId">Source worldbook template to copy.</param>
    /// <param name="mainCharacter">Main character data to add.</param>
    /// <param name="extraLoreEntries">Optional extra lore entries to add to the world dataset.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<IndexingResult> InitializeAdventureAsync(
        Guid adventureId,
        Guid worldbookId,
        MainCharacterIndexEntry mainCharacter,
        IReadOnlyList<ExtraLoreIndexEntry>? extraLoreEntries = null,
        CancellationToken ct = default);

    /// <summary>
    /// Commits scene data to an adventure's knowledge graph.
    /// Switches to the adventure's volume, commits data, then releases lock.
    ///
    /// Acquires exclusive lock for the entire operation duration.
    /// </summary>
    /// <param name="adventureId">Adventure to commit to.</param>
    /// <param name="commitAction">Action that performs the actual commit using IRagChunkService.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<IndexingResult> CommitSceneDataAsync(
        Guid adventureId,
        Func<CancellationToken, Task> commitAction,
        CancellationToken ct = default);

    /// <summary>
    /// Checks if a worldbook has been indexed (has a template volume).
    /// Does not require exclusive lock.
    /// </summary>
    Task<bool> IsWorldbookIndexedAsync(Guid worldbookId, CancellationToken ct = default);

    /// <summary>
    /// Deletes an adventure's knowledge graph volume.
    /// Acquires exclusive lock.
    /// </summary>
    Task DeleteAdventureVolumeAsync(Guid adventureId, CancellationToken ct = default);

    /// <summary>
    /// Deletes a worldbook's template volume.
    /// Acquires exclusive lock.
    /// </summary>
    Task DeleteWorldbookVolumeAsync(Guid worldbookId, CancellationToken ct = default);
}

/// <summary>
/// Lorebook entry for indexing.
/// </summary>
public sealed record LorebookIndexEntry(
    Guid Id,
    string Content,
    string ContentType);

/// <summary>
/// Main character data for indexing.
/// </summary>
public sealed record MainCharacterIndexEntry(
    Guid Id,
    string Name,
    string Description);

/// <summary>
/// Extra lore entry for indexing during adventure creation.
/// </summary>
public sealed record ExtraLoreIndexEntry(
    Guid Id,
    string Title,
    string Content,
    string Category);
