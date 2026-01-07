using System.Text.Json;
using System.Text.Json.Serialization;

namespace FableCraft.Application.NarrativeEngine.Models;

/// <summary>
/// Partial profile for background characters. Stored as LorebookEntry in World KG.
/// </summary>
public sealed class GeneratedPartialProfile
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = null!;

    [JsonPropertyName("identity")]
    public string Identity { get; init; } = null!;

    [JsonPropertyName("appearance")]
    public string Appearance { get; init; } = null!;

    [JsonPropertyName("personality")]
    public string Personality { get; init; } = null!;

    [JsonPropertyName("behavioral_patterns")]
    public BehavioralPatterns? BehavioralPatterns { get; init; }

    [JsonPropertyName("voice")]
    public VoiceProfile? Voice { get; init; }

    [JsonPropertyName("knowledge_boundaries")]
    public KnowledgeBoundaries? KnowledgeBoundaries { get; init; }

    [JsonExtensionData]
    public Dictionary<string, object> AdditionalData { get; set; } = new();

    /// <summary>
    /// Combined description for LorebookEntry.Description field.
    /// </summary>
    public string Description => $"{Identity}\n\n{Appearance}\n\n{Personality}";

    public string ToJsonString() => JsonSerializer.Serialize(this);
}

public class BehavioralPatterns
{
    [JsonPropertyName("default")]
    public string? Default { get; init; }

    [JsonPropertyName("when_stressed")]
    public string? WhenStressed { get; init; }

    [JsonPropertyName("tell")]
    public string? Tell { get; init; }
}

public class VoiceProfile
{
    [JsonPropertyName("style")]
    public string? Style { get; init; }

    [JsonPropertyName("distinctive_quality")]
    public string? DistinctiveQuality { get; init; }

    [JsonPropertyName("warm")]
    public string? Warm { get; init; }

    [JsonPropertyName("cold")]
    public string? Cold { get; init; }
}

public class KnowledgeBoundaries
{
    [JsonPropertyName("knows_from_role")]
    public List<string> KnowsFromRole { get; init; } = new();

    [JsonPropertyName("blind_spots")]
    public List<string> BlindSpots { get; init; } = new();

    [JsonPropertyName("would_pick_up_on")]
    public List<string> WouldPickUpOn { get; init; } = new();
}
