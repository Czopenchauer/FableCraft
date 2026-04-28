using System.Text.Json;
using System.Text.Json.Serialization;

namespace FableCraft.Application.NarrativeEngine.Models;

internal sealed class CharacterDeltaTrackerOutput<TTracker> where TTracker : class
{
    public string Description { get; set; } = null!;

    [JsonPropertyName("changes_summary")]
    public TrackerChanges TrackerChanges { get; set; } = null!;

    [JsonPropertyName("tracker")]
    public TTracker Tracker { get; set; } = null!;
}

/// <summary>
/// Output model for delta-based tracker updates.
/// Contains only changed fields in the Updates property which get merged with the previous state.
/// </summary>
internal sealed class MainCharacterDeltaOutput
{
    [JsonPropertyName("time_update")]
    public TimeUpdate? TimeUpdate { get; set; }

    [JsonPropertyName("changes_summary")]
    public TrackerChanges ChangeSummary { get; set; } = null!;

    /// <summary>
    /// Delta updates to apply to the previous tracker state.
    /// Only contains fields that changed - omitted fields retain their previous values.
    /// </summary>
    [JsonPropertyName("updates")]
    public JsonElement Updates { get; set; }
}

internal sealed class TimeUpdate
{
    [JsonPropertyName("previous")]
    public string? Previous { get; set; }

    [JsonPropertyName("current")]
    public string? Current { get; set; }

    [JsonPropertyName("elapsed")]
    public string? Elapsed { get; set; }
}

internal sealed class TrackerChanges
{
    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }
}