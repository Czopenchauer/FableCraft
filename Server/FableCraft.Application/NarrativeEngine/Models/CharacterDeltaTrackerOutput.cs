using System.Text.Json.Serialization;

namespace FableCraft.Application.NarrativeEngine.Models;

internal sealed class CharacterDeltaTrackerOutput
{
    [JsonPropertyName("time_update")]
    public TimeUpdate TimeUpdate { get; set; } = null!;

    [JsonPropertyName("changes_summary")]
    public ChangesSummary ChangesSummary { get; set; } = null!;

    public string Description { get; set; } = null!;

    [JsonPropertyName("changes")]
    public TrackerChanges TrackerChanges { get; set; } = null!;
}

internal sealed class TimeUpdate
{
    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }
}

internal sealed class ChangesSummary
{
    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }
}

internal sealed class TrackerChanges
{
    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }
}