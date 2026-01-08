using System.Text.Json.Serialization;

using FableCraft.Infrastructure.Persistence;

namespace FableCraft.Application.NarrativeEngine.Models;

/// <summary>
/// Lightweight character profile for background characters.
/// Stored as LorebookEntry in World KG, not as Character entity.
/// </summary>
public sealed class GeneratedPartialProfile
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = null!;

    /// <summary>
    /// Computed description for LorebookEntry.Description field.
    /// </summary>
    [JsonIgnore]
    public string Description => $"{Name} {AdditionalData.ToJsonString()}";

    [JsonExtensionData]
    public Dictionary<string, object> AdditionalData { get; set; } = new();
}