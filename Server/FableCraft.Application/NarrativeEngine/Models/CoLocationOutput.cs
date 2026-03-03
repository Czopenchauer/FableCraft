using System.Text.Json.Serialization;

namespace FableCraft.Application.NarrativeEngine.Models;

/// <summary>
///     Output from the CoLocationAgent determining which characters are at the scene location.
/// </summary>
internal class CoLocationOutput
{
    [JsonPropertyName("co_located_characters")]
    public CoLocatedCharacterOutput[] CoLocatedCharacters { get; set; } = [];
}

/// <summary>
///     A character determined to be at the same location as the current scene.
/// </summary>
internal class CoLocatedCharacterOutput
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("reason")]
    public required string Reason { get; set; }
}
