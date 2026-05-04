using System.Text.Json;
using System.Text.Json.Serialization;

namespace FableCraft.Application.NarrativeEngine.Models;

/// <summary>
///     Output model for the InventoryTrackerAgent. Updates is constrained to Carried and/or Assets only.
///     If no inventory change occurred, no_inventory_change is true and Updates is empty/absent.
/// </summary>
internal sealed class InventoryDeltaOutput
{
    [JsonPropertyName("no_inventory_change")]
    public bool NoInventoryChange { get; set; }

    [JsonPropertyName("changes_summary")]
    public JsonElement? ChangesSummary { get; set; }

    [JsonPropertyName("updates")]
    public JsonElement Updates { get; set; }
}
