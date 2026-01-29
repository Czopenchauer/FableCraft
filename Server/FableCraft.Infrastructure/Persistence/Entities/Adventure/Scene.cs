using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace FableCraft.Infrastructure.Persistence.Entities.Adventure;

public enum CommitStatus
{
    Uncommited,
    Lock,
    Commited
}

public enum EnrichmentStatus
{
    NotEnriched,
    Enriching,
    Enriched,
    EnrichmentFailed
}

public class Scene : IEntity
{
    [Required]
    public Guid AdventureId { get; init; }

    public Adventure? Adventure { get; init; }

    public required int SequenceNumber { get; init; }

    public string? AdventureSummary { get; set; }

    [Required]
    public required string NarrativeText { get; set; }

    public CommitStatus CommitStatus { get; set; }

    public EnrichmentStatus EnrichmentStatus { get; set; } = EnrichmentStatus.NotEnriched;

    public required Metadata Metadata { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public List<CharacterState> CharacterStates { get; set; } = new();

    public List<CharacterRelationship> CharacterRelationships { get; set; } = new();

    public List<CharacterSceneRewrite> CharacterSceneRewrites { get; set; } = new();

    public List<CharacterMemory> CharacterMemories { get; set; } = new();

    public List<MainCharacterAction> CharacterActions { get; init; } = new();

    public List<LorebookEntry> Lorebooks { get; set; } = new();

    [Key]
    public Guid Id { get; set; }
}

public sealed class Metadata
{
    /// <summary>
    ///     Resolution output from ResolutionAgent (raw JSON string).
    /// </summary>
    public string? ResolutionOutput { get; set; }

    public Tracker? Tracker { get; set; }

    /// <summary>
    ///     Context gathered from RAG after this scene was generated.
    ///     Used as extra context for generating the next scene.
    /// </summary>
    public GatheredContext? GatheredContext { get; set; }

    /// <summary>
    ///     Extra context from writer that will be used in the next scene generation.
    /// </summary>
    public Dictionary<string, object>? WriterObservation { get; set; }

    /// <summary>
    ///     Chronicler story state (dramatic questions, promises, threads, stakes, windows, world momentum).
    /// </summary>
    public ChroniclerStoryState? ChroniclerState { get; set; }

    /// <summary>
    ///     Writer guidance from the Chronicler for the next scene (stored as JSON string).
    /// </summary>
    public string? WriterGuidance { get; set; }
}

/// <summary>
///     Context gathered from RAG (knowledge graph) to be used in the next scene generation.
/// </summary>
public sealed class GatheredContext
{
    public GatheredContextItem[] WorldContext { get; set; } = [];

    public GatheredContextItem[] NarrativeContext { get; set; } = [];

    public string[] BackgroundRoster { get; set; } = [];

    [JsonExtensionData]
    public Dictionary<string, object> AdditionalProperties { get; init; } = null!;
}

/// <summary>
///     A single item of gathered context from the knowledge graph.
/// </summary>
public sealed class GatheredContextItem
{
    public required string Topic { get; set; }

    public required string Content { get; set; }
}

/// <summary>
///     Tracker for story and main character progress within the adventure. Characters other than the main character are
///     tracked separately - <see cref="Character" />
/// </summary>
public sealed class Tracker
{
    public SceneTracker? Scene { get; set; }

    public MainCharacterState? MainCharacter { get; set; }
}

public sealed class SceneTracker
{
    public required string Time { get; set; }

    public required string Location { get; set; }

    public required string Weather { get; set; }

    public string[] CharactersPresent { get; set; } = [];

    [JsonExtensionData]
    public Dictionary<string, object> AdditionalProperties { get; set; } = null!;
}

public sealed class MainCharacterState
{
    public string? MainCharacterDescription { get; set; }

    public MainCharacterTracker? MainCharacter { get; set; }
}

public sealed class CharacterTracker
{
    public required string Name { get; set; }

    public required string Location { get; set; }

    public string? Appearance { get; set; }

    public string? GeneralBuild { get; set; }

    [JsonExtensionData]
    public Dictionary<string, object> AdditionalProperties { get; set; } = null!;
}

public sealed class MainCharacterTracker
{
    public required string Name { get; set; }

    public string? Appearance { get; set; }

    public string? GeneralBuild { get; set; }

    [JsonExtensionData]
    public Dictionary<string, object> AdditionalProperties { get; set; } = null!;
}