using System.Text.Json.Serialization;

using FableCraft.Infrastructure.Persistence.Entities.Adventure;

namespace FableCraft.Application.NarrativeEngine.Models;

/// <summary>
/// Input context for the SimulationPlanner agent.
/// </summary>
internal sealed class SimulationPlannerInput
{
    /// <summary>
    /// Current scene tracker with time, location, and characters present.
    /// </summary>
    public required SceneTracker SceneTracker { get; init; }

    /// <summary>
    /// All arc_important and significant characters.
    /// </summary>
    public required List<CharacterRosterEntry> CharacterRoster { get; init; }

    /// <summary>
    /// Active world events that may affect character behavior.
    /// </summary>
    public object? WorldEvents { get; init; }

    /// <summary>
    /// Characters who have flagged intent to seek the MC.
    /// </summary>
    public List<PendingMcInteractionEntry>? PendingMcInteractions { get; init; }

    /// <summary>
    /// Writer guidance from Chronicler - where the story is heading.
    /// </summary>
    public WriterGuidance? NarrativeDirection { get; init; }
}

/// <summary>
/// Character data for simulation planning.
/// </summary>
internal sealed class CharacterRosterEntry
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("importance")]
    public required string Importance { get; init; }

    [JsonPropertyName("location")]
    public required string Location { get; init; }

    [JsonPropertyName("last_simulated")]
    public string? LastSimulated { get; init; }

    [JsonPropertyName("goals_summary")]
    public string? GoalsSummary { get; init; }

    [JsonPropertyName("key_relationships")]
    public string[]? KeyRelationships { get; init; }

    [JsonPropertyName("relationship_notes")]
    public string? RelationshipNotes { get; init; }

    [JsonPropertyName("routine_summary")]
    public string? RoutineSummary { get; init; }

    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }
}

/// <summary>
/// Entry for a character's pending MC interaction.
/// </summary>
internal sealed class PendingMcInteractionEntry
{
    [JsonPropertyName("character")]
    public required string Character { get; init; }

    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }
}

/// <summary>
/// Output from the SimulationPlanner agent.
/// </summary>
internal sealed class SimulationPlannerOutput
{
    /// <summary>
    /// False when no simulation is needed this cycle.
    /// </summary>
    [JsonPropertyName("simulation_needed")]
    public bool? SimulationNeeded { get; init; }

    /// <summary>
    /// Reason when simulation_needed is false.
    /// </summary>
    [JsonPropertyName("reason")]
    public string? Reason { get; init; }

    /// <summary>
    /// How much in-world time to simulate.
    /// </summary>
    [JsonPropertyName("simulation_period")]
    public SimulationPeriod? SimulationPeriod { get; init; }

    /// <summary>
    /// Groups of 2-4 arc_important characters to simulate together.
    /// </summary>
    [JsonPropertyName("cohorts")]
    public List<SimulationCohort>? Cohorts { get; init; }

    /// <summary>
    /// arc_important characters to simulate alone.
    /// </summary>
    [JsonPropertyName("standalone")]
    public List<StandaloneSimulation>? Standalone { get; set; }

    /// <summary>
    /// arc_important characters who don't need simulation (present_in_scene or recently_simulated).
    /// </summary>
    [JsonPropertyName("skip")]
    public List<SkippedCharacter>? Skip { get; init; }

    /// <summary>
    /// Significant characters likely to appear in next scene (need OffscreenInference).
    /// </summary>
    [JsonPropertyName("significant_for_inference")]
    public List<SignificantForInference>? SignificantForInference { get; init; }

    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }
}

/// <summary>
/// Time period for simulation.
/// </summary>
internal sealed class SimulationPeriod
{
    [JsonPropertyName("to")]
    public required string To { get; init; }

    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }
}

/// <summary>
/// A group of characters to simulate together.
/// </summary>
internal sealed class SimulationCohort
{
    /// <summary>
    /// Names of characters in this cohort.
    /// </summary>
    [JsonPropertyName("characters")]
    public required List<string> Characters { get; init; }

    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }
}

/// <summary>
/// A character to simulate alone.
/// </summary>
internal sealed class StandaloneSimulation
{
    /// <summary>
    /// Character name.
    /// </summary>
    [JsonPropertyName("character")]
    public required string Character { get; init; }

    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }
}

/// <summary>
/// A character skipped from simulation.
/// </summary>
internal sealed class SkippedCharacter
{
    /// <summary>
    /// Character name.
    /// </summary>
    [JsonPropertyName("character")]
    public required string Character { get; init; }

    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }
}

/// <summary>
/// A significant character that needs OffscreenInference before next scene.
/// </summary>
internal sealed class SignificantForInference
{
    /// <summary>
    /// Character name.
    /// </summary>
    [JsonPropertyName("character")]
    public required string Character { get; init; }

    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }
}