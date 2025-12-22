using System.Text.Json.Serialization;

namespace FableCraft.Application.NarrativeEngine.Models;

internal class ContextGathererOutput
{
    [JsonPropertyName("analysis_summary")]
    public required AnalysisSummary AnalysisSummary { get; set; }

    [JsonPropertyName("carried_forward")]
    public required CarriedForwardContext CarriedForward { get; set; }

    [JsonPropertyName("world_queries")]
    public ContextQuery[] WorldQueries { get; set; } = [];

    [JsonPropertyName("narrative_queries")]
    public ContextQuery[] NarrativeQueries { get; set; } = [];

    [JsonPropertyName("dropped_context")]
    public DroppedContext[] DroppedContext { get; set; } = [];
}

internal class AnalysisSummary
{
    [JsonPropertyName("current_situation")]
    public required string CurrentSituation { get; set; }

    [JsonPropertyName("key_elements_in_play")]
    public string[] KeyElementsInPlay { get; set; } = [];

    [JsonPropertyName("primary_focus_areas")]
    public string[] PrimaryFocusAreas { get; set; } = [];

    [JsonPropertyName("context_continuity")]
    public required string ContextContinuity { get; set; }
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

internal class DroppedContext
{
    [JsonPropertyName("topic")]
    public required string Topic { get; set; }

    [JsonPropertyName("reason")]
    public required string Reason { get; set; }
}
