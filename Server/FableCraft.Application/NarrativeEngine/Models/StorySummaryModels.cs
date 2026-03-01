using System.Text.Json.Serialization;

namespace FableCraft.Application.NarrativeEngine.Models;

/// <summary>
///     Output from the Story Summary Agent.
///     Contains a rolling, compressed summary of story events for a character or MC.
/// </summary>
internal sealed class StorySummaryOutput
{
    [JsonPropertyName("story_summary")]
    public required string StorySummary { get; set; }
}
