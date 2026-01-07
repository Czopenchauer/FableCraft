using System.Text.Json.Serialization;

namespace FableCraft.Application.NarrativeEngine.Models;

/// <summary>
/// Input for IntentCheckAgent - character planning their intentions for an upcoming period.
/// </summary>
internal sealed class IntentCheckInput
{
    public required CharacterContext Character { get; init; }
    public required string TimePeriod { get; init; }
    public required string[] ArcImportantCharacters { get; init; }
    public string? WorldEvents { get; init; }
    public string? PreviousIntentions { get; init; }
}

/// <summary>
/// Output from IntentCheckAgent - character's intentions for the period.
/// </summary>
internal sealed class IntentCheckOutput
{
    [JsonPropertyName("seeking")]
    public List<SeekingIntent>? Seeking { get; init; }

    [JsonPropertyName("avoiding")]
    public List<AvoidingIntent>? Avoiding { get; init; }

    [JsonPropertyName("self_focused")]
    public required SelfFocusedIntent SelfFocused { get; init; }

    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }
}

internal sealed class SeekingIntent
{
    [JsonPropertyName("character")]
    public required string Character { get; init; }

    [JsonPropertyName("intent")]
    public required string Intent { get; init; }

    [JsonPropertyName("driver")]
    public required string Driver { get; init; }

    [JsonPropertyName("urgency")]
    public required string Urgency { get; init; }

    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }
}

internal sealed class AvoidingIntent
{
    [JsonPropertyName("character")]
    public required string Character { get; init; }

    [JsonPropertyName("reason")]
    public required string Reason { get; init; }

    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }
}

internal sealed class SelfFocusedIntent
{
    [JsonPropertyName("primary_activity")]
    public required string PrimaryActivity { get; init; }

    [JsonPropertyName("goal_served")]
    public required string GoalServed { get; init; }

    [JsonPropertyName("location")]
    public required string Location { get; init; }

    [JsonPropertyName("open_to_interruption")]
    public required string OpenToInterruption { get; init; }

    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }
}
