using System.Text.Json.Serialization;

namespace FableCraft.Application.NarrativeEngine.Models;

public sealed class GeneratedLore
{
    [JsonPropertyName("title")]
    public string Title { get; init; } = null!;

    [JsonPropertyName("formatType")]
    public string FormatType { get; init; } = null!;

    [JsonPropertyName("text")]
    public string Text { get; init; } = null!;

    [JsonPropertyName("summary")]
    public string Summary { get; init; } = null!;
}