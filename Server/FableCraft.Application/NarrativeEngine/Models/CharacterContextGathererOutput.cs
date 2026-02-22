using System.Text.Json.Serialization;

namespace FableCraft.Application.NarrativeEngine.Models;

/// <summary>
///     Output from CharacterContextGatherer agent.
///     Similar to ContextGathererOutput but without scene-specific fields like background_roster.
/// </summary>
internal class CharacterContextGathererOutput
{
    [JsonPropertyName("carried_forward")]
    public required CarriedForwardContext CarriedForward { get; set; }

    [JsonPropertyName("world_queries")]
    public ContextQuery[] WorldQueries { get; set; } = [];

    [JsonPropertyName("narrative_queries")]
    public ContextQuery[] NarrativeQueries { get; set; } = [];

    [JsonExtensionData]
    public Dictionary<string, object>? AdditionalData { get; set; }
}
