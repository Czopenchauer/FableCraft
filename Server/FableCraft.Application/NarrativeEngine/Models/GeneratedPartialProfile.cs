using System.Text.Json.Serialization;

namespace FableCraft.Application.NarrativeEngine.Models;

/// <summary>
///     Lightweight character profile for background characters.
///     Stored as LorebookEntry in World KG, not as Character entity.
/// </summary>
public sealed class GeneratedPartialProfile
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = null!;

    public string Description { get; set; } = null!;

    [JsonPropertyName("identity")]
    public string Identity { get; init; } = null!;

    [JsonExtensionData]
    public Dictionary<string, object> AdditionalData { get; set; } = new();
}