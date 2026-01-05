using System.Collections.Concurrent;
using System.Text.Json.Serialization;

using FableCraft.Infrastructure.Persistence.Entities.Adventure;

namespace FableCraft.Application.NarrativeEngine.Models;

internal enum GenerationProcessStep
{
    NotStarted,
    SceneGenerated,
    EnrichmentCompleted
}

internal sealed class GenerationContext
{
    public void SetupRequiredFields(
        SceneContext[] sceneContext,
        TrackerStructure trackerStructure,
        MainCharacter mainCharacter,
        List<CharacterContext> characters,
        AdventureAgentLlmPreset[] agentLlmPresets,
        string promptPath,
        string adventureStartTime,
        string? worldSettings,
        LorebookEntry[] previouslyGeneratedLore)
    {
        SceneContext = sceneContext;
        TrackerStructure = trackerStructure;
        MainCharacter = mainCharacter;
        Characters = characters;
        AgentLlmPreset = agentLlmPresets;
        PromptPath = promptPath;
        AdventureStartTime = adventureStartTime;
        WorldSettings = worldSettings;
        PreviouslyGeneratedLore = previouslyGeneratedLore;
    }

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

    [JsonIgnore]
    public string? WorldSettings { get; set; }

    // Lore entries that were already generated in previous steps, they weren't yet commited to KG.
    [JsonIgnore]
    public LorebookEntry[] PreviouslyGeneratedLore { get; set; } = [];

    public CharacterContext[]? NewCharacters { get; set; }

    public ConcurrentQueue<CharacterContext> CharacterUpdates { get; set; } = new();

    public LocationGenerationResult[]? NewLocations { get; set; }

    public ConcurrentQueue<GeneratedLore> NewLore { get; set; } = [];

    public GeneratedItem[]? NewItems { get; set; }

    public string? NewResolution { get; set; }

    public ContextBase? ContextGathered { get; set; }

    public GeneratedScene? NewScene { get; set; }

    public Tracker? NewTracker { get; set; }

    public Guid? NewSceneId { get; set; }

    public GenerationProcessStep GenerationProcessStep { get; set; }

    public Tracker? LatestTracker() => SceneContext.Where(x => x.Metadata.Tracker != null).OrderByDescending(x => x.SequenceNumber).FirstOrDefault()?.Metadata.Tracker;

    /// <summary>
    /// Writer guidance from ChroniclerAgent for the next scene.
    /// </summary>
    public WriterGuidance? WriterGuidance { get; set; }

    /// <summary>
    /// World events emitted by ChroniclerAgent and Character simulation. Saved as LorebookEntries.
    /// </summary>
    public ConcurrentQueue<WorldEvent> NewWorldEvents { get; set; } = new();

    /// <summary>
    /// Chronicler story state to persist in scene metadata.
    /// </summary>
    public ChroniclerStoryState? NewChroniclerState { get; set; }

    /// <summary>
    /// Simulation plan from SimulationPlannerAgent.
    /// </summary>
    public SimulationPlannerOutput? SimulationPlan { get; set; }
}

internal sealed class CharacterContext
{
    public required Guid CharacterId { get; set; }

    public required string Name { get; set; } = null!;

    public required string Description { get; set; } = null!;

    public required CharacterImportance Importance { get; set; }

    public required CharacterStats CharacterState { get; set; } = null!;

    public required CharacterTracker? CharacterTracker { get; set; }

    public required List<MemoryContext> CharacterMemories { get; set; } = new();

    public required List<CharacterRelationshipContext> Relationships { get; set; } = new();

    public required List<CharacterSceneContext> SceneRewrites { get; set; } = new();

    /// <summary>
    /// Simulation-related data: last_simulated, potential_interactions, pending_mc_interaction.
    /// </summary>
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

    public required SceneTracker? StoryTracker { get; set; }
}

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