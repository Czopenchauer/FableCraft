using System.Text.Json.Serialization;

using FableCraft.Infrastructure.Persistence.Entities;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;

namespace FableCraft.Application.NarrativeEngine.Models;

internal enum GenerationProcessStep
{
    NotStarted,
    ContextGatheringFinished,
    NarrativeDirectionFinished,
    ContentCreationFinished,
    SceneGenerationFinished,
    TrackerFinished,
    Completed,
    SceneSavedAwaitingEnrichment,
    EnrichmentStarted
}

internal sealed class GenerationContext
{
    public required Guid AdventureId { get; set; }

    public required string PlayerAction { get; set; }

    /// <summary>
    ///     The LLM preset to use for generation
    /// </summary>
    [JsonIgnore]
    public LlmPreset LlmPreset { get; set; } = null!;

    /// <summary>
    ///     The LLM preset to use for generation
    /// </summary>
    [JsonIgnore]
    public LlmPreset ComplexPreset { get; set; } = null!;

    [JsonIgnore]
    public string? Summary { get; set; }

    /// <summary>
    ///     Has to be refetched from DB as there's no point to store it
    /// </summary>
    [JsonIgnore]
    public SceneContext[] SceneContext { get; set; } = null!;

    /// <summary>
    ///     List of the all Characters. Has to be refetched from DB as there's no point to store it
    /// </summary>
    [JsonIgnore]
    public List<CharacterContext> Characters { get; set; } = new();

    [JsonIgnore]
    public TrackerStructure TrackerStructure { get; set; } = null!;

    [JsonIgnore]
    public MainCharacter MainCharacter { get; set; } = null!;

    public CharacterContext[]? NewCharacters { get; set; }

    public LocationGenerationResult[]? NewLocations { get; set; }

    public GeneratedLore[]? NewLore { get; set; }

    public NarrativeDirectorOutput? NewNarrativeDirection { get; set; }

    public List<ContextBase>? ContextGathered { get; set; }

    public GeneratedScene? NewScene { get; set; }

    public Tracker? NewTracker { get; set; }

    public CharacterContext[]? CharacterUpdates { get; set; }

    public Guid? NewSceneId { get; set; }

    public required GenerationProcessStep GenerationProcessStep { get; set; }
}

internal sealed class CharacterContext
{
    public required Guid CharacterId { get; set; }

    public Guid SceneId { get; set; }

    public string Name { get; set; } = null!;

    public string Description { get; set; } = null!;

    public required int SequenceNumber { get; set; }

    public CharacterStats CharacterState { get; set; } = null!;

    public CharacterTracker? CharacterTracker { get; set; }

    public CharacterDevelopmentTracker? DevelopmentTracker { get; set; }
}

internal sealed class SceneContext
{
    public required int SequenceNumber { get; set; }

    public required string SceneContent { get; set; } = null!;

    public required string PlayerChoice { get; set; } = null!;

    public required IEnumerable<CharacterContext> Characters { get; set; } = [];

    public required Metadata Metadata { get; set; } = null!;
}