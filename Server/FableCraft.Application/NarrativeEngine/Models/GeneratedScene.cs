using System.Text.Json.Serialization;

namespace FableCraft.Application.NarrativeEngine.Models;

public class GeneratedScene
{
    [JsonPropertyName("scene")]
    public string Scene { get; init; } = null!;

    [JsonPropertyName("choices")]
    public string[] Choices { get; init; } = null!;
}