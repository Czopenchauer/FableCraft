using System.Text.Json;
using System.Text.Json.Serialization;

namespace FableCraft.Application.NarrativeEngine.Models;

/// <summary>
///     Output model for the ProgressionAgent. Updates is constrained to Skills and/or Abilities only.
///     If no progression occurred, no_progression is true and Updates is empty/absent.
/// </summary>
internal sealed class ProgressionDeltaOutput
{
    [JsonPropertyName("no_progression")]
    public bool NoProgression { get; set; }

    [JsonPropertyName("changes_summary")]
    public JsonElement? ChangesSummary { get; set; }

    [JsonPropertyName("updates")]
    public JsonElement Updates { get; set; }

    [JsonPropertyName("progression_effects")]
    public List<string>? ProgressionEffects { get; set; }
}