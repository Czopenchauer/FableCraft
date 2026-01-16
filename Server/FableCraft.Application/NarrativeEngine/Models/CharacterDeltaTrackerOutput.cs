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

internal sealed class TrackerChanges
{
    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }
}