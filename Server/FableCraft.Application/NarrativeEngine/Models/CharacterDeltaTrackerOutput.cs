using System.Text.Json.Serialization;

namespace FableCraft.Application.NarrativeEngine.Models;

internal sealed class CharacterDeltaTrackerOutput
{
    public string Description { get; set; } = null!;

    [JsonPropertyName("changes")]
    public TrackerChanges TrackerChanges { get; set; } = null!;
    
    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }
}

internal sealed class TrackerChanges
{
    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }
}