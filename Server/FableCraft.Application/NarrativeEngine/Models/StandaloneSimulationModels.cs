using System.Text.Json.Serialization;

using FableCraft.Infrastructure.Persistence.Entities.Adventure;

namespace FableCraft.Application.NarrativeEngine.Models;

/// <summary>
/// Input context for the StandaloneSimulationAgent.
/// </summary>
internal sealed class StandaloneSimulationInput
{
    /// <summary>
    /// The character being simulated.
    /// </summary>
    public required CharacterContext Character { get; init; }

    /// <summary>
    /// The time period to simulate (e.g., "6 hours", "until morning").
    /// </summary>
    public required string TimePeriod { get; init; }

    /// <summary>
    /// World events that may affect the character.
    /// </summary>
    public object? WorldEvents { get; init; }

    /// <summary>
    /// Names of other profiled characters (for potential_interactions flagging).
    /// </summary>
    public string[]? ProfiledCharacterNames { get; init; }
}

/// <summary>
/// Output from the StandaloneSimulationAgent, matching the &lt;solo_simulation&gt; JSON structure.
/// </summary>
internal sealed class StandaloneSimulationOutput
{
    /// <summary>
    /// Scenes generated during the simulation period.
    /// </summary>
    [JsonPropertyName("scenes")]
    public required List<SimulationScene> Scenes { get; init; }

    /// <summary>
    /// Updates to relationships based on simulation events.
    /// </summary>
    [JsonPropertyName("relationship_updates")]
    public List<SimulationRelationshipUpdate>? RelationshipUpdates { get; init; }

    /// <summary>
    /// Updates to the character's profile/state (e.g., emotional_landscape, goals).
    /// </summary>
    [JsonPropertyName("profile_updates")]
    public Dictionary<string, object>? ProfileUpdates { get; init; }

    /// <summary>
    /// Updates to the character's tracker (physical state like fatigue, hunger).
    /// </summary>
    [JsonPropertyName("tracker_updates")]
    public Dictionary<string, object>? TrackerUpdates { get; init; }

    /// <summary>
    /// Intended interactions with other profiled characters (to be resolved in future simulations).
    /// </summary>
    [JsonPropertyName("potential_interactions")]
    public List<PotentialInteraction>? PotentialInteractions { get; init; }

    /// <summary>
    /// If the character decides to seek out the protagonist.
    /// </summary>
    [JsonPropertyName("pending_mc_interaction")]
    public PendingMcInteraction? PendingMcInteraction { get; init; }

    /// <summary>
    /// World events caused by this character's actions that others could discover.
    /// </summary>
    [JsonPropertyName("world_events_emitted")]
    public List<WorldEventEmitted>? WorldEventsEmitted { get; init; }

    /// <summary>
    /// Events logged when this character interacted with significant (profiled) NPCs.
    /// These feed OffscreenInference for the affected characters.
    /// </summary>
    [JsonPropertyName("character_events")]
    public List<SimulationCharacterEvent>? CharacterEvents { get; init; }

    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }
}

/// <summary>
/// A scene generated during simulation.
/// </summary>
internal sealed class SimulationScene
{
    /// <summary>
    /// Time, location, weather, and characters present for this scene.
    /// </summary>
    [JsonPropertyName("story_tracker")]
    public required SceneTracker StoryTracker { get; init; }

    /// <summary>
    /// First-person narrative from the character's perspective.
    /// </summary>
    [JsonPropertyName("narrative")]
    public required string Narrative { get; init; }

    /// <summary>
    /// Memory metadata for indexing.
    /// </summary>
    [JsonPropertyName("memory")]
    public required SimulationMemory Memory { get; init; }
}

/// <summary>
/// Memory metadata from a simulation scene.
/// </summary>
internal sealed class SimulationMemory
{
    [JsonPropertyName("summary")]
    public required string Summary { get; init; }

    [JsonPropertyName("salience")]
    public required double Salience { get; init; }

    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }
}

/// <summary>
/// Relationship update from simulation.
/// </summary>
internal sealed class SimulationRelationshipUpdate
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }
    
    [JsonPropertyName("dynamic")]
    public required object Dynamic { get; init; }

    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }
}

/// <summary>
/// A world event emitted by the character's actions.
/// </summary>
internal sealed class WorldEventEmitted
{
    [JsonPropertyName("when")]
    public required string When { get; init; }

    [JsonPropertyName("where")]
    public required string Where { get; init; }

    [JsonPropertyName("event")]
    public required string Event { get; init; }
}

/// <summary>
/// An event logged when an arc_important character interacts with a significant character.
/// These feed OffscreenInference for the affected significant character.
/// </summary>
internal sealed class SimulationCharacterEvent
{
    /// <summary>
    /// Name of the significant character who was affected.
    /// </summary>
    [JsonPropertyName("character")]
    public required string Character { get; init; }

    /// <summary>
    /// In-world time when the event occurred.
    /// </summary>
    [JsonPropertyName("time")]
    public required string Time { get; init; }

    /// <summary>
    /// What happened from the target character's perspective.
    /// </summary>
    [JsonPropertyName("event")]
    public required string Event { get; init; }

    /// <summary>
    /// The simulating character's interpretation of how this affected the target.
    /// </summary>
    [JsonPropertyName("my_read")]
    public required string MyRead { get; init; }

    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }
}