using System.Text.Json;
using System.Text.Json.Serialization;

namespace FableCraft.Application.NarrativeEngine.Models;

internal sealed class TrackerDeBloaterDeltaOutput
{
    [JsonPropertyName("changes_summary")]
    public TrackerChanges ChangeSummary { get; set; } = null!;

    [JsonPropertyName("updates")]
    public JsonElement Updates { get; set; }
}