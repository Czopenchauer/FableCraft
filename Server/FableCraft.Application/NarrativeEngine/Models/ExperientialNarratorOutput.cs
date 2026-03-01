using System.Text.Json.Serialization;

namespace FableCraft.Application.NarrativeEngine.Models;

/// <summary>
///     Output from the Experiential Narrator Agent (Pass 1 of character reflection).
///     Produces only the character's subjective scene rewrite and death status.
/// </summary>
public sealed class ExperientialNarratorOutput
{
    [JsonPropertyName("is_dead")]
    public bool IsDead { get; set; }

    [JsonPropertyName("scene_rewrite")]
    public required string SceneRewrite { get; set; }
}
