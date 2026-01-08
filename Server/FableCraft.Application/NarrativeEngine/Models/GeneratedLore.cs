using System.Text.Json.Serialization;

namespace FableCraft.Application.NarrativeEngine.Models;

public sealed class GeneratedLore
{
    [JsonPropertyName("name")]
    public string Title { get; init; } = null!;

    [JsonPropertyName("content")]
    public string Description { get; init; } = null!;

    [JsonExtensionData]
    public Dictionary<string, object> AdditionalData { get; set; } = new();
}