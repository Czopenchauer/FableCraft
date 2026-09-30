using System.Text.Json;
using System.Text.Json.Serialization;

using FableCraft.Infrastructure.Persistence.Entities.Adventure;

namespace FableCraft.Application.NarrativeEngine.Models;

internal sealed class GenerationContext
{
    public required Guid AdventureId { get; set; }

    public required string PlayerAction { get; set; }

    /// <summary>
    ///     Optional ad-hoc player instruction for the NarrativeCatalystAgent, submitted with the player action.
    /// </summary>
    public string? NarrativeCatalystInstruction { get; set; }

    /// <summary>
    ///     Optional ad-hoc player instruction for the WriterAgent, submitted with the player action.
    /// </summary>
    public string? WriterInstruction { get; set; }

    /// <summary>
    ///     Optional ad-hoc player instruction for the LoreAgent, submitted with the player action.
    ///     Persisted with the generation context so it is available during the enrichment phase.
    /// </summary>
    public string? LoreInstruction { get; set; }

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

    [JsonIgnore]
    public MainCharacterTracker? InitialMainCharacterTracker { get; set; }

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

    public NarrativeCatalystOutput? NarrativeCatalystOutput { get; set; }

    public GeneratedLore[] ChroniclerLore { get; set; } = [];

    /// <summary>
    ///     Writer guidance from ChroniclerAgent for the next scene.
    /// </summary>
    public WriterGuidance? WriterGuidance => ChroniclerOutput?.WriterGuidance;

    /// <summary>
    ///     Chronicler story state to persist in scene metadata.
    /// </summary>
    public ChroniclerStoryState? NewChroniclerState => ChroniclerOutput?.StoryState;

    /// <summary>
    ///     When true, SimulationOrchestrator should skip execution.
    ///     Set during regeneration when Simulation was not selected for regeneration.
    /// </summary>
    [JsonIgnore]
    public bool SkipSimulation { get; set; }

    /// <summary>
    ///     When true, ChroniclerAgent should skip execution.
    ///     Set during regeneration when Chronicler was not selected for regeneration.
    /// </summary>
    [JsonIgnore]
    public bool SkipChronicler { get; set; }

    /// <summary>
    ///     When true, ContextGatherer should skip execution.
    ///     Set during regeneration when ContextGatherer was not selected for regeneration.
    /// </summary>
    [JsonIgnore]
    public bool SkipContextGatherer { get; set; }

    /// <summary>
    ///     When true, WorldInfoExtractorAgent should skip execution.
    ///     Set during regeneration when WorldInfoExtractor was not selected for regeneration.
    /// </summary>
    [JsonIgnore]
    public bool SkipWorldInfoExtractor { get; set; }

    /// <summary>
    ///     When true, CoLocationAgent should skip execution.
    ///     Set during regeneration when CoLocation was not selected for regeneration.
    /// </summary>
    [JsonIgnore]
    public bool SkipCoLocation { get; set; }

    /// <summary>
    ///     When true, ProgressionAgent should skip execution.
    ///     Set during regeneration when Progression was not selected (and MainCharacterTracker isn't being regenerated).
    /// </summary>
    [JsonIgnore]
    public bool SkipProgression { get; set; }

    /// <summary>
    ///     When true, InventoryTrackerAgent should skip execution.
    ///     Set during regeneration when InventoryTracker was not selected (and MainCharacterTracker isn't being regenerated).
    /// </summary>
    [JsonIgnore]
    public bool SkipInventory { get; set; }

    /// <summary>
    ///     When true, StorySummaryAgent should skip execution for both per-character and MC summaries.
    ///     Set during regeneration when NarrativeCatalyst was not selected.
    /// </summary>
    [JsonIgnore]
    public bool SkipStorySummary { get; set; }

    /// <summary>
    ///     When true, CharacterContextGatherer should run for all characters even if already processed.
    ///     Set during regeneration when ContextGatherer is selected for regeneration.
    /// </summary>
    [JsonIgnore]
    public bool ForceCharacterContextGathering { get; set; }

    /// <summary>
    ///     World info extractions accumulated from main narrative and character reflections.
    /// </summary>
    public WorldInfoExtractionOutput? WorldInfoExtractions { get; set; }

    /// <summary>
    ///     Tracks which sources have already been processed for world info extraction.
    ///     Used to prevent duplicate extractions on retry.
    ///     Keys: "main" for main narrative, "reflection:{characterName}" for reflections,
    ///     "simulation:{characterName}:{sceneIndex}" for simulation scenes.
    /// </summary>
    public HashSet<string> ProcessedWorldInfoSources { get; set; } = [];

    /// <summary>
    ///     Updated MC story summary to persist in scene metadata.
    ///     Non-null also signals that the MC StorySummaryAgent has already produced a value
    ///     for this enrichment, so retries skip the agent.
    /// </summary>
    public string? NewMcStorySummary { get; set; }

    /// <summary>
    ///     True once ProgressionAgent has finished for this enrichment (even if it produced no delta).
    ///     Used to skip the agent on retries.
    /// </summary>
    public bool ProgressionAgentRan { get; set; }

    /// <summary>
    ///     Delta produced by ProgressionAgent that has not yet been merged into the main character tracker.
    ///     Cleared once merged so the merge stays idempotent across retries.
    /// </summary>
    public JsonElement? ProgressionDelta { get; set; }

    /// <summary>
    ///     True once InventoryTrackerAgent has finished for this enrichment (even if it produced no delta).
    ///     Used to skip the agent on retries.
    /// </summary>
    public bool InventoryAgentRan { get; set; }

    /// <summary>
    ///     Delta produced by InventoryTrackerAgent that has not yet been merged into the main character tracker.
    ///     Cleared once merged so the merge stays idempotent across retries.
    /// </summary>
    public JsonElement? InventoryDelta { get; set; }

    /// <summary>
    ///     Co-location output from CoLocationAgent.
    ///     Determines which characters from the registry are at the scene location.
    /// </summary>
    public CoLocationOutput? CoLocationOutput { get; set; }

    public QaReviewOutput? QaReview { get; set; }

    public bool ScenePipelineRevisionComplete { get; set; }

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
        List<ExtraLoreContext>? extraLoreEntries = null,
        MainCharacterTracker? initialMainCharacterTracker = null)
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
        InitialMainCharacterTracker = initialMainCharacterTracker;
    }

    public Tracker? LatestTracker()
    {
        return SceneContext.Where(x => x.Metadata.Tracker != null).OrderByDescending(x => x.SequenceNumber).FirstOrDefault()?.Metadata.Tracker;
    }
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

    public required List<CharacterRelationshipContext> Relationships { get; set; } = new();

    public required List<CharacterSceneContext> SceneRewrites { get; set; } = new();

    public required SimulationMetadata? SimulationMetadata { get; set; }
}

internal sealed class CharacterRelationshipContext
{
    public required string TargetCharacterName { get; set; } = null!;

    public required string Dynamic { get; set; }

    public required IDictionary<string, object> Data { get; set; } = null!;

    public required int SequenceNumber { get; set; }

    public required string? UpdateTime { get; set; }
}

internal sealed class CharacterSceneContext
{
    public required string Content { get; set; } = null!;

    public required int SequenceNumber { get; set; }

    public required SceneTracker? SceneTracker { get; set; }

    /// <summary>
    ///     Context gathered after simulation for use in next invocation.
    /// </summary>
    public CharacterGatheredContext? GatheredContext { get; set; }

    /// <summary>
    ///     Rolling story summary for this character up to this scene.
    /// </summary>
    public string? StorySummary { get; set; }
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

    public required Metadata Metadata { get; set; } = null!;

    public static SceneContext CreateFromScene(Scene scene)
    {
        return new SceneContext
        {
            SceneContent = scene.NarrativeText,
            Metadata = scene.Metadata,
            SequenceNumber = scene.SequenceNumber
        };
    }
}