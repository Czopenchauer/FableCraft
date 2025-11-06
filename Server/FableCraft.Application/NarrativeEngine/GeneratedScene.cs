using Newtonsoft.Json;

namespace FableCraft.Application.NarrativeEngine;

internal class GeneratedScene
{
    [JsonProperty("scene_text")]
    public string Scene { get; init; } = null!;

    [JsonProperty("choices")]
    public string[] Choices { get; init; } = null!;
}