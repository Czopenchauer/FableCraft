using System.Text.Json.Serialization;

using Microsoft.SemanticKernel.ChatCompletion;

namespace FableCraft.Application.NarrativeEngine.Models;

/// <summary>
/// Input context for the SimulationModeratorAgent.
/// </summary>
internal sealed class CohortSimulationInput
{
    /// <summary>
    /// Characters being simulated together.
    /// </summary>
    public required CharacterContext[] CohortMembers { get; init; }

    /// <summary>
    /// The time period to simulate.
    /// </summary>
    public required SimulationPeriod SimulationPeriod { get; init; }

    /// <summary>
    /// Known interactions from SimulationPlanner/IntentCheck.
    /// Extracted from SimulationCohort.ExtensionData.
    /// </summary>
    public Dictionary<string, object>? KnownInteractions { get; init; }

    /// <summary>
    /// World events that may affect character behavior.
    /// </summary>
    public object? WorldEvents { get; init; }

    /// <summary>
    /// Names of significant characters (profiled but not arc-important).
    /// Characters can interact with these during simulation.
    /// </summary>
    public string[]? SignificantCharacters { get; init; }
}

/// <summary>
/// Information about a cohort member for the Moderator.
/// </summary>
internal sealed class CohortMemberInfo
{
    /// <summary>
    /// Character name.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>
    /// Current location.
    /// </summary>
    [JsonPropertyName("location")]
    public required string Location { get; init; }

    /// <summary>
    /// Primary active goal.
    /// </summary>
    [JsonPropertyName("primary_goal")]
    public required string PrimaryGoal { get; init; }

    /// <summary>
    /// Relationship notes with other cohort members.
    /// </summary>
    [JsonPropertyName("cohort_relationships")]
    public Dictionary<string, string>? CohortRelationships { get; init; }
}

/// <summary>
/// Output from the SimulationModeratorAgent.
/// </summary>
internal sealed class CohortSimulationOutput
{
    /// <summary>
    /// The time period that was simulated.
    /// </summary>
    [JsonPropertyName("simulation_period")]
    public required CohortSimulationPeriodOutput SimulationPeriod { get; init; }

    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }
}

/// <summary>
/// Simulation period in the Moderator output.
/// </summary>
internal sealed class CohortSimulationPeriodOutput
{
    [JsonPropertyName("from")]
    public required string From { get; init; }

    [JsonPropertyName("to")]
    public required string To { get; init; }
}

/// <summary>
/// Tracks a character's state during cohort simulation.
/// Lives in QueryCharacterPlugin for the duration of the simulation.
/// </summary>
internal sealed class CharacterSimulationSession
{
    /// <summary>
    /// Character name.
    /// </summary>
    public required string CharacterName { get; init; }

    /// <summary>
    /// Character ID for database operations.
    /// </summary>
    public required Guid CharacterId { get; init; }

    /// <summary>
    /// Accumulated chat history for this character during simulation.
    /// </summary>
    public ChatHistory ChatHistory { get; } = new();

    /// <summary>
    /// The character's submitted reflection output (same structure as standalone).
    /// Set when character calls submit_reflection tool.
    /// </summary>
    public StandaloneSimulationOutput? SubmittedReflection { get; set; }

    /// <summary>
    /// Whether the character has submitted their reflection.
    /// </summary>
    public bool ReflectionSubmitted => SubmittedReflection != null;
}

/// <summary>
/// Query types for Moderator -> Character communication.
/// </summary>
internal enum CharacterQueryType
{
    /// <summary>
    /// Ask character about their intentions for the simulation period.
    /// </summary>
    Intention,

    /// <summary>
    /// Ask character to respond to a situation or stimulus.
    /// </summary>
    Response,

    /// <summary>
    /// Ask character to provide their final reflection output.
    /// </summary>
    Reflection
}

/// <summary>
/// Result from a cohort simulation run.
/// </summary>
internal sealed class CohortSimulationResult
{
    public required CohortSimulationOutput Result { get; set; }
    
    /// <summary>
    /// Character reflections indexed by character name.
    /// Each value is a StandaloneSimulationOutput (same structure as standalone simulation).
    /// </summary>
    public required Dictionary<string, StandaloneSimulationOutput> CharacterReflections { get; init; }
}
