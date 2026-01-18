using System.Text.Json.Serialization;

namespace FableCraft.Application.NarrativeEngine.Models;

internal class ContextGathererOutput
{
    [JsonPropertyName("carried_forward")]
    public required CarriedForwardContext CarriedForward { get; set; }

    [JsonPropertyName("world_queries")]
    public ContextQuery[] WorldQueries { get; set; } = [];

    [JsonPropertyName("narrative_queries")]
    public ContextQuery[] NarrativeQueries { get; set; } = [];

    [JsonPropertyName("background_roster")]
    public string[] BackgroundRoster { get; set; } = [];

    [JsonExtensionData]
    public Dictionary<string, object> AdditionalData { get; set; } = new();
}

internal class CarriedForwardContext
{
    [JsonPropertyName("world_context")]
    public ContextItem[] WorldContext { get; set; } = [];

    [JsonPropertyName("narrative_context")]
    public ContextItem[] NarrativeContext { get; set; } = [];
}

internal class ContextItem
{
    [JsonPropertyName("topic")]
    public required string Topic { get; set; }

    [JsonPropertyName("content")]
    public required string Content { get; set; }
}

internal class ContextQuery
{
    [JsonPropertyName("query")]
    public required string Query { get; set; }

    [JsonPropertyName("rationale")]
    public required string Rationale { get; set; }
}