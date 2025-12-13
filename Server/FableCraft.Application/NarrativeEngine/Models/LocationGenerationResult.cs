using System.Text.Json.Serialization;

namespace FableCraft.Application.NarrativeEngine.Models;

internal class LocationGenerationResult
{
    [JsonPropertyName("entity_data")]
    public EntityData EntityData { get; init; } = null!;

    [JsonPropertyName("narrative_data")]
    public NarrativeData NarrativeData { get; init; } = null!;

    [JsonPropertyName("mechanics")]
    public Mechanics Mechanics { get; init; } = null!;

    [JsonPropertyName("relationships")]
    public List<Relationship> Relationships { get; init; } = new();

    [JsonPropertyName("generated_contents")]
    public GeneratedContents GeneratedContents { get; init; } = null!;
}

internal class EntityData
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;

    [JsonPropertyName("parent_node_id")]
    public string ParentNodeId { get; init; } = string.Empty;

    [JsonPropertyName("tags")]
    public List<string> Tags { get; init; } = new();
}

internal class NarrativeData
{
    [JsonPropertyName("short_description")]
    public string ShortDescription { get; init; } = string.Empty;

    [JsonPropertyName("detailed_description")]
    public string DetailedDescription { get; init; } = string.Empty;
}

internal class Mechanics
{
    [JsonPropertyName("danger_rating")]
    public int DangerRating { get; init; }

    [JsonPropertyName("accessibility_condition")]
    public string AccessibilityCondition { get; init; } = string.Empty;

    [JsonPropertyName("features_implementation")]
    public List<FeatureImplementation> FeaturesImplementation { get; init; } = new();
}

internal class FeatureImplementation
{
    [JsonPropertyName("feature")]
    public string Feature { get; init; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; init; } = string.Empty;
}

internal class Relationship
{
    [JsonPropertyName("target")]
    public string Target { get; init; } = string.Empty;

    [JsonPropertyName("relation_type")]
    public string RelationType { get; init; } = string.Empty;

    [JsonPropertyName("context")]
    public string Context { get; init; } = string.Empty;
}

internal class GeneratedContents
{
    [JsonPropertyName("npcs")]
    public List<string> Npcs { get; init; } = new();

    [JsonPropertyName("loot_potential")]
    public string LootPotential { get; init; } = string.Empty;
}