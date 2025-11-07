using System.Text.Json.Serialization;

namespace FableCraft.Application.NarrativeEngine;

internal class GeneratedScene
{
    [JsonPropertyName("scene_text")]
    public string Scene { get; init; } = null!;

    [JsonPropertyName("choices")]
    public string[] Choices { get; init; } = null!;
}