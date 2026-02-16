using System.Text.Json.Serialization;

using Microsoft.SemanticKernel.ChatCompletion;

namespace FableCraft.Application.NarrativeEngine.Models;

/// <summary>
///     Input context for the SimulationModeratorAgent.
/// </summary>
internal sealed class CohortSimulationInput
{
    /// <summary>
    ///     Characters being simulated together.
    /// </summary>
    public required CharacterContext[] CohortMembers { get; init; }

    /// <summary>
    ///     The time period to simulate.
    /// </summary>
    public required string SimulationPeriod { get; init; }

    /// <summary>
    ///     Known interactions from SimulationPlanner/IntentCheck.
    ///     Extracted from SimulationCohort.ExtensionData.
    /// </summary>
    public Dictionary<string, object>? KnownInteractions { get; init; }

    /// <summary>
    ///     World events that may affect character behavior.
    /// </summary>
    public object? WorldEvents { get; init; }

    /// <summary>
    ///     Names of significant characters (profiled but not arc-important).
    ///     Characters can interact with these during simulation.
    /// </summary>
    public string[]? SignificantCharacters { get; init; }
}

/// <summary>
///     Output from the SimulationModeratorAgent.
/// </summary>
internal sealed class CohortSimulationOutput
{
    /// <summary>
    ///     The time period that was simulated.
    /// </summary>
    [JsonPropertyName("simulation_period")]
    public required CohortSimulationPeriodOutput SimulationPeriod { get; init; }

    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }
}

/// <summary>
///     Simulation period in the Moderator output.
/// </summary>
internal sealed class CohortSimulationPeriodOutput
{
    [JsonPropertyName("from")]
    public required string From { get; init; }

    [JsonPropertyName("to")]
    public required string To { get; init; }
}

/// <summary>
///     Tracks a character's state during cohort simulation.
///     Lives in QueryCharacterPlugin for the duration of the simulation.
/// </summary>
internal sealed class CharacterSimulationSession
{
    /// <summary>
    ///     Character name.
    /// </summary>
    public required string CharacterName { get; init; }

    /// <summary>
    ///     Character ID for database operations.
    /// </summary>
    public required Guid CharacterId { get; init; }

    /// <summary>
    ///     Accumulated chat history for this character during simulation.
    ///     Contains system prompt and all prior queries/responses.
    /// </summary>
    public ChatHistory ChatHistory { get; } = new();

    /// <summary>
    ///     Whether the reflection prompt has been added to ChatHistory.
    ///     Used to avoid duplicate prompts on retry.
    /// </summary>
    public bool ReflectionPromptAdded { get; set; }
}

/// <summary>
///     Query types for Moderator -> Character communication.
/// </summary>
internal enum CharacterQueryType
{
    /// <summary>
    ///     Ask character about their intentions for the simulation period.
    /// </summary>
    Intention,

    /// <summary>
    ///     Ask character to respond to a situation or stimulus.
    /// </summary>
    Response,

    /// <summary>
    ///     Ask character to provide their final reflection output.
    /// </summary>
    Reflection
}

/// <summary>
///     Result from a cohort simulation run.
/// </summary>
internal sealed class CohortSimulationResult
{
    public required CohortSimulationOutput Result { get; set; }

    /// <summary>
    ///     Character reflections indexed by character name.
    ///     Each value is a StandaloneSimulationOutput (same structure as standalone simulation).
    /// </summary>
    public required Dictionary<string, StandaloneSimulationOutput> CharacterReflections { get; init; }
}

/// <summary>
///     Saved state from a cohort simulation that can be used to resume reflection collection.
///     Stored in GenerationContext.CohortSimulationState.
/// </summary>
internal sealed class CohortSimulationState
{
    /// <summary>
    ///     The moderator's simulation output.
    /// </summary>
    public required CohortSimulationOutput ModeratorResult { get; init; }

    /// <summary>
    ///     Reference to the generation context (for accessing Characters list during resume).
    /// </summary>
    public required GenerationContext Context { get; init; }

    /// <summary>
    ///     Character sessions with accumulated ChatHistory, indexed by character name (case-insensitive).
    /// </summary>
    public required Dictionary<string, CharacterSimulationSession> Sessions { get; init; }

    /// <summary>
    ///     Characters that still need reflections collected.
    /// </summary>
    public required List<CharacterContext> PendingCharacters { get; init; }

    /// <summary>
    ///     Successfully collected reflections so far (case-insensitive keys).
    /// </summary>
    public Dictionary<string, StandaloneSimulationOutput> CollectedReflections { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}