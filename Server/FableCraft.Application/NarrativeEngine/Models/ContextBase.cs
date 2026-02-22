using System.Text.Json.Serialization;

namespace FableCraft.Application.NarrativeEngine.Models;

internal class ContextBase
{
    public ContextItem[] WorldContext { get; set; } = [];

    public ContextItem[] NarrativeContext { get; set; } = [];

    public string[] BackgroundRoster { get; set; } = [];

    /// <summary>
    ///     Characters discovered to be at the same location as the scene.
    ///     Determined by ContextGatherer comparing character locations against scene location.
    /// </summary>
    public CoLocatedCharacter[] CoLocatedCharacters { get; set; } = [];

    [JsonExtensionData]
    public Dictionary<string, object> AdditionalData { get; set; } = new();
}

internal class CoLocatedCharacter
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("reason")]
    public required string Reason { get; set; }
}