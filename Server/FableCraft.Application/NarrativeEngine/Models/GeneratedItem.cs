using System.Text.Json.Serialization;

namespace FableCraft.Application.NarrativeEngine.Models;

internal class GeneratedItem
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("description")]
    public required string Description { get; set; }

    [JsonExtensionData]
    public Dictionary<string, object> AdditionalData { get; set; } = new();
}