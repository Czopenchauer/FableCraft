using System.Text.Json.Serialization;

using FableCraft.Infrastructure.Persistence.Entities.Adventure;

namespace FableCraft.Application.NarrativeEngine.Models;

internal sealed class GenerationContext
{
    public required Guid AdventureId { get; set; }

    public required string PlayerAction { get; set; }

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

    [JsonIgnore]
    public AdventureAgentLlmPreset[] AgentLlmPreset { get; set; } = null!;

    [JsonIgnore]
    public string PromptPath { get; set; } = null!;

    [JsonIgnore]
    public string AdventureStartTime { get; set; } = null!;

    /// <summary>
    ///     Extra lore entries added during adventure creation. Used in the first scene to provide
    ///     additional world context to the writer.
    /// </summary>
    [JsonIgnore]
    public List<ExtraLoreContext> ExtraLoreEntries { get; set; } = [];

    // Lore entries that were already generated in previous steps, they weren't yet commited to KG.
    [JsonIgnore]
    public LorebookEntry[] PreviouslyGeneratedLore { get; set; } = [];

    [JsonIgnore]
    public LorebookEntry[] PreviouslyGeneratedLocations { get; set; } = [];

    [JsonIgnore]
    public LorebookEntry[] PreviouslyGeneratedItems { get; set; } = [];

    [JsonIgnore]
    public List<BackgroundCharacter> BackgroundCharacters { get; set; } = [];

    public List<CharacterContext> NewCharacters { get; set; } = [];

    public List<CharacterContext> CharacterUpdates { get; set; } = [];

    public LocationGenerationResult[]? NewLocations { get; set; }

    public List<GeneratedLore> NewLore { get; set; } = [];

    public GeneratedItem[]? NewItems { get; set; }

    public List<GeneratedPartialProfile> NewBackgroundCharacters { get; set; } = [];

    public string? NewResolution { get; set; }

    public ContextBase? ContextGathered { get; set; }

    public GeneratedScene? NewScene { get; set; }

    public Tracker? NewTracker { get; set; }

    public Guid? NewSceneId { get; set; }

    public ChroniclerOutput? ChroniclerOutput { get; set; }

    public GeneratedLore[] ChroniclerLore { get; set; } = [];

    /// <summary>
    ///     Writer guidance from ChroniclerAgent for the next scene.
    /// </summary>
    public WriterGuidance? WriterGuidance => ChroniclerOutput?.WriterGuidance;

    /// <summary>
    ///     World events emitted by ChroniclerAgent and Character simulation. Saved as LorebookEntries.
    /// </summary>
    public List<WorldEvent> NewWorldEvents { get; set; } = [];

    /// <summary>
    ///     Chronicler story state to persist in scene metadata.
    /// </summary>
    public ChroniclerStoryState? NewChroniclerState => ChroniclerOutput?.StoryState;

    /// <summary>
    ///     Simulation plan from SimulationPlannerAgent.
    /// </summary>
    public SimulationPlannerOutput? SimulationPlan { get; set; }

    /// <summary>
    ///     CharacterEvent IDs to mark as consumed in SaveEnrichmentStep.
    ///     Collected by OffscreenInferenceProcessor after processing events.
    /// </summary>
    public List<Guid> CharacterEventsToConsume { get; set; } = [];

    /// <summary>
    ///     New CharacterEvents to save in SaveEnrichmentStep.
    ///     Collected by SimulationOrchestrator when arc_important characters interact with significant characters.
    /// </summary>
    public List<CharacterEventToSave> NewCharacterEvents { get; set; } = [];

    public void SetupRequiredFields(
        SceneContext[] sceneContext,
        TrackerStructure trackerStructure,
        MainCharacter mainCharacter,
        List<CharacterContext> characters,
        AdventureAgentLlmPreset[] agentLlmPresets,
        string promptPath,
        string adventureStartTime,
        LorebookEntry[] previouslyGeneratedLore,
        LorebookEntry[] previouslyGeneratedLocations,
        LorebookEntry[] previouslyGeneratedItems,
        List<BackgroundCharacter> backgroundCharacters,
        List<ExtraLoreContext>? extraLoreEntries = null)
    {
        SceneContext = sceneContext;
        TrackerStructure = trackerStructure;
        MainCharacter = mainCharacter;
        Characters = characters;
        AgentLlmPreset = agentLlmPresets;
        PromptPath = promptPath;
        AdventureStartTime = adventureStartTime;
        PreviouslyGeneratedLore = previouslyGeneratedLore;
        PreviouslyGeneratedLocations = previouslyGeneratedLocations;
        PreviouslyGeneratedItems = previouslyGeneratedItems;
        BackgroundCharacters = backgroundCharacters;
        ExtraLoreEntries = extraLoreEntries ?? [];
    }

    public Tracker? LatestTracker()
    {
        return SceneContext.Where(x => x.Metadata.Tracker != null).OrderByDescending(x => x.SequenceNumber).FirstOrDefault()?.Metadata.Tracker;
    }
}

/// <summary>
///     Data for a CharacterEvent to be saved.
/// </summary>
internal sealed class CharacterEventToSave
{
    public required Guid AdventureId { get; init; }

    public required string TargetCharacterName { get; init; }

    public required string SourceCharacterName { get; init; }

    public required string Time { get; init; }

    public required string Event { get; init; }

    public required string SourceRead { get; init; }
}

internal sealed class CharacterContext
{
    public required bool IsDead { get; set; }

    public required Guid CharacterId { get; set; }

    public required string Name { get; set; } = null!;

    public required string Description { get; set; } = null!;

    public required CharacterImportance Importance { get; set; }

    public required CharacterStats CharacterState { get; set; } = null!;

    public required CharacterTracker? CharacterTracker { get; set; }

    public required List<MemoryContext> CharacterMemories { get; set; } = new();

    public required List<CharacterRelationshipContext> Relationships { get; set; } = new();

    public required List<CharacterSceneContext> SceneRewrites { get; set; } = new();

    public required SimulationMetadata? SimulationMetadata { get; set; }
}

internal sealed class MemoryContext
{
    public required string MemoryContent { get; set; } = null!;

    public required SceneTracker SceneTracker { get; set; }

    public required double Salience { get; set; }

    public required IDictionary<string, object>? Data { get; set; } = null!;
}

internal sealed class CharacterRelationshipContext
{
    public required string TargetCharacterName { get; set; } = null!;

    public required object Dynamic { get; set; }

    public required IDictionary<string, object> Data { get; set; } = null!;

    public required int SequenceNumber { get; set; }

    public required string? UpdateTime { get; set; }
}

internal sealed class CharacterSceneContext
{
    public required string Content { get; set; } = null!;

    public required int SequenceNumber { get; set; }

    public required SceneTracker? SceneTracker { get; set; }
}

/// <summary>
///     Extra lore entry context for the first scene generation.
/// </summary>
internal sealed record ExtraLoreContext(
    string Title,
    string Content,
    string Category);

internal sealed class SceneContext
{
    public required int SequenceNumber { get; set; }

    public required string SceneContent { get; set; } = null!;

    public required string PlayerChoice { get; set; } = null!;

    public required Metadata Metadata { get; set; } = null!;

    public static SceneContext CreateFromScene(Scene scene)
    {
        return new SceneContext
        {
            SceneContent = scene.NarrativeText,
            PlayerChoice = scene.CharacterActions.FirstOrDefault(y => y.Selected)
                               ?.ActionDescription
                           ?? string.Empty,
            Metadata = scene.Metadata,
            SequenceNumber = scene.SequenceNumber
        };
    }
}