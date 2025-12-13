using System.Text.Json.Serialization;

using FableCraft.Infrastructure.Persistence.Entities;
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
        LlmPreset llmPreset,
        LlmPreset complexPreset, 
        TrackerStructure trackerStructure, 
        MainCharacter mainCharacter,
        List<CharacterContext> characters)
    {
        SceneContext = sceneContext;
        LlmPreset = llmPreset;
        ComplexPreset = complexPreset;
        TrackerStructure = trackerStructure;
        MainCharacter = mainCharacter;
        Characters = characters;
    }

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

    public GeneratedItem[]? NewItems { get; set; }

    public NarrativeDirectorOutput? NewNarrativeDirection { get; set; }

    public ContextBase? ContextGathered { get; set; }

    public GeneratedScene? NewScene { get; set; }

    public Tracker? NewTracker { get; set; }

    public List<CharacterContext>? CharacterUpdates { get; set; }

    public Guid? NewSceneId { get; set; }

    public GenerationProcessStep GenerationProcessStep { get; set; }

    public SceneContext? LatestSceneContext() => SceneContext.OrderByDescending(x => x.SequenceNumber).FirstOrDefault();

    public Tracker? LatestTracker() => SceneContext.Where(x => x.Metadata.Tracker != null).OrderByDescending(x => x.SequenceNumber).FirstOrDefault()?.Metadata.Tracker;
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

    public static SceneContext CreateFromScene(Scene scene)
    {
        return new SceneContext
        {
            SceneContent = scene.NarrativeText,
            PlayerChoice = scene.CharacterActions.FirstOrDefault(y => y.Selected)
                               ?.ActionDescription
                           ?? string.Empty,
            Metadata = scene.Metadata,
            Characters = scene.CharacterStates.Select(y => new CharacterContext
            {
                CharacterState = y.CharacterStats,
                CharacterTracker = y.Tracker,
                DevelopmentTracker = y.DevelopmentTracker,
                Description = y.Description,
                Name = y.CharacterStats.CharacterIdentity.FullName!,
                CharacterId = y.CharacterId,
                SequenceNumber = y.SequenceNumber
            }),
            SequenceNumber = scene.SequenceNumber
        };
    }
}