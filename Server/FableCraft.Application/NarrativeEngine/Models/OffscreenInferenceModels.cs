using System.Text.Json.Serialization;

namespace FableCraft.Application.NarrativeEngine.Models;

/// <summary>
/// Input context for the OffscreenInferenceAgent.
/// </summary>
internal sealed class OffscreenInferenceInput
{
    /// <summary>
    /// The significant character to infer state for.
    /// </summary>
    public required CharacterContext Character { get; init; }

    /// <summary>
    /// Events logged by arc_important characters that affected this character.
    /// </summary>
    public required List<CharacterEventDto> EventsLog { get; init; }

    /// <summary>
    /// How long since the character was last simulated/inferred (e.g., "3 days").
    /// </summary>
    public required string TimeElapsed { get; init; }

    /// <summary>
    /// Current in-world datetime.
    /// </summary>
    public required string CurrentDateTime { get; init; }

    /// <summary>
    /// World events that may have affected the character.
    /// </summary>
    public object? WorldEvents { get; init; }
}

/// <summary>
/// Output from the OffscreenInferenceAgent.
/// </summary>
internal sealed class OffscreenInferenceOutput
{
    /// <summary>
    /// Brief first-person narrative memories from the character's perspective (1-2 scenes).
    /// </summary>
    [JsonPropertyName("scenes")]
    public List<OffscreenScene>? Scenes { get; init; }

    /// <summary>
    /// Where the character is and what they're doing right now.
    /// </summary>
    [JsonPropertyName("current_situation")]
    public required CurrentSituation CurrentSituation { get; init; }

    /// <summary>
    /// Updates to the character's profile/state using dot-notation keys.
    /// </summary>
    [JsonPropertyName("profile_updates")]
    public Dictionary<string, object>? ProfileUpdates { get; init; }

    /// <summary>
    /// Updates to the character's tracker (physical state) using dot-notation keys.
    /// </summary>
    [JsonPropertyName("tracker_updates")]
    public Dictionary<string, object>? TrackerUpdates { get; init; }

    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }
}

/// <summary>
/// A scene from the character's perspective during offscreen time.
/// </summary>
internal sealed class OffscreenScene
{
    /// <summary>
    /// Story tracker for the scene (DateTime, Location, Weather, CharactersPresent).
    /// </summary>
    [JsonPropertyName("story_tracker")]
    public required OffscreenStoryTracker StoryTracker { get; init; }

    /// <summary>
    /// First-person prose from this character's perspective.
    /// </summary>
    [JsonPropertyName("narrative")]
    public required string Narrative { get; init; }

    /// <summary>
    /// Memory index entry for this scene.
    /// </summary>
    [JsonPropertyName("memory")]
    public required OffscreenMemory Memory { get; init; }
}

/// <summary>
/// Story tracker for an offscreen scene.
/// </summary>
internal sealed class OffscreenStoryTracker
{
    [JsonPropertyName("DateTime")]
    public required string DateTime { get; init; }

    [JsonPropertyName("Location")]
    public required string Location { get; init; }

    [JsonPropertyName("Weather")]
    public string? Weather { get; init; }

    [JsonPropertyName("CharactersPresent")]
    public List<string>? CharactersPresent { get; init; }
}

/// <summary>
/// Memory entry for an offscreen scene.
/// </summary>
internal sealed class OffscreenMemory
{
    [JsonPropertyName("summary")]
    public required string Summary { get; init; }

    [JsonPropertyName("salience")]
    public required int Salience { get; init; }

    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }
}

/// <summary>
/// The character's current situation for interaction context.
/// </summary>
internal sealed class CurrentSituation
{
    /// <summary>
    /// Where they are right now.
    /// </summary>
    [JsonPropertyName("location")]
    public required string Location { get; init; }

    /// <summary>
    /// What they're doing when found/contacted.
    /// </summary>
    [JsonPropertyName("activity")]
    public required string Activity { get; init; }

    /// <summary>
    /// Context for whoever is about to interact — busy? distracted? expecting trouble?
    /// </summary>
    [JsonPropertyName("ready_for_interaction")]
    public required string ReadyForInteraction { get; init; }

    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }
}

/// <summary>
/// DTO for character events (from CharacterEvent entity).
/// </summary>
internal sealed class CharacterEventDto
{
    /// <summary>
    /// In-world time when the event occurred.
    /// </summary>
    public required string Time { get; init; }

    /// <summary>
    /// What happened from this character's perspective.
    /// </summary>
    public required string Event { get; init; }

    /// <summary>
    /// Name of the arc_important character who caused/witnessed this.
    /// </summary>
    public required string SourceCharacter { get; init; }

    /// <summary>
    /// The source character's interpretation of how this affected the target.
    /// </summary>
    public required string SourceRead { get; init; }
}
