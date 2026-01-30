using System.Text.Json.Serialization;

using FableCraft.Infrastructure.Persistence.Entities.Adventure;

namespace FableCraft.Application.NarrativeEngine.Models;

/// <summary>
///     Output from the Character Reflection Agent, processed post-scene.
///     Only sections that changed are included in the output.
/// </summary>
public sealed class CharacterReflectionOutput
{
    [JsonPropertyName("scene_rewrite")]
    public required string SceneRewrite { get; set; }

    [JsonPropertyName("memory")]
    public CharacterMemoryOutput? Memory { get; set; }

    [JsonPropertyName("relationship_updates")]
    public CharacterRelationshipOutput[] RelationshipUpdates { get; set; } = [];

    [JsonPropertyName("profile_updates")]
    public CharacterStats? ProfileUpdates { get; set; }
}

public sealed class CharacterMemoryOutput
{
    [JsonPropertyName("summary")]
    public required string Summary { get; set; }

    [JsonPropertyName("salience")]
    public double Salience { get; set; }

    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }
}

public sealed class CharacterRelationshipOutput
{
    public required string Toward { get; set; }

    [JsonPropertyName("dynamic")]
    public required string Dynamic { get; set; }

    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }
}