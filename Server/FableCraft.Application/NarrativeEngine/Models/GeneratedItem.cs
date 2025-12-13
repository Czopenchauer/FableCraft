using System.Text.Json.Serialization;

namespace FableCraft.Application.NarrativeEngine.Models;

internal class GeneratedItem
{
    [JsonPropertyName("entity_data")]
    public ItemEntityData EntityData { get; init; } = null!;

    [JsonPropertyName("narrative_data")]
    public ItemNarrativeData NarrativeData { get; init; } = null!;

    [JsonPropertyName("mechanics")]
    public ItemMechanics Mechanics { get; init; } = null!;

    [JsonPropertyName("relationships")]
    public List<ItemRelationship> Relationships { get; init; } = new();

    [JsonPropertyName("acquisition")]
    public ItemAcquisition Acquisition { get; init; } = null!;
}

internal class ItemEntityData
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;

    [JsonPropertyName("subtype")]
    public string Subtype { get; init; } = string.Empty;

    [JsonPropertyName("power_level")]
    public string PowerLevel { get; init; } = string.Empty;

    [JsonPropertyName("tags")]
    public List<string> Tags { get; init; } = new();
}

internal class ItemNarrativeData
{
    [JsonPropertyName("short_description")]
    public string ShortDescription { get; init; } = string.Empty;

    [JsonPropertyName("detailed_description")]
    public string DetailedDescription { get; init; } = string.Empty;

    [JsonPropertyName("lore_text")]
    public string LoreText { get; init; } = string.Empty;
}

internal class ItemMechanics
{
    [JsonPropertyName("is_magical")]
    public bool IsMagical { get; init; }

    [JsonPropertyName("is_unique")]
    public bool IsUnique { get; init; }

    [JsonPropertyName("is_tradeable")]
    public bool IsTradeable { get; init; }

    [JsonPropertyName("is_consumable")]
    public bool IsConsumable { get; init; }

    [JsonPropertyName("durability")]
    public string Durability { get; init; } = string.Empty;

    [JsonPropertyName("effects")]
    public List<ItemEffect> Effects { get; init; } = new();

    [JsonPropertyName("requirements")]
    public ItemRequirements Requirements { get; init; } = new();

    [JsonPropertyName("value")]
    public ItemValue Value { get; init; } = new();
}

internal class ItemEffect
{
    [JsonPropertyName("effect_name")]
    public string EffectName { get; init; } = string.Empty;

    [JsonPropertyName("effect_type")]
    public string EffectType { get; init; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; init; } = string.Empty;

    [JsonPropertyName("activation")]
    public string Activation { get; init; } = string.Empty;

    [JsonPropertyName("resource_cost")]
    public string ResourceCost { get; init; } = string.Empty;
}

internal class ItemRequirements
{
    [JsonPropertyName("skill_requirements")]
    public List<string> SkillRequirements { get; init; } = new();

    [JsonPropertyName("stat_requirements")]
    public List<string> StatRequirements { get; init; } = new();

    [JsonPropertyName("other_requirements")]
    public List<string> OtherRequirements { get; init; } = new();
}

internal class ItemValue
{
    [JsonPropertyName("monetary_value")]
    public string MonetaryValue { get; init; } = string.Empty;

    [JsonPropertyName("rarity")]
    public string Rarity { get; init; } = string.Empty;

    [JsonPropertyName("desirability")]
    public string Desirability { get; init; } = string.Empty;
}

internal class ItemRelationship
{
    [JsonPropertyName("target")]
    public string Target { get; init; } = string.Empty;

    [JsonPropertyName("relation_type")]
    public string RelationType { get; init; } = string.Empty;

    [JsonPropertyName("context")]
    public string Context { get; init; } = string.Empty;
}

internal class ItemAcquisition
{
    [JsonPropertyName("method")]
    public string Method { get; init; } = string.Empty;

    [JsonPropertyName("source")]
    public string Source { get; init; } = string.Empty;

    [JsonPropertyName("discovery_context")]
    public string DiscoveryContext { get; init; } = string.Empty;

    [JsonPropertyName("narrative_hook")]
    public string NarrativeHook { get; init; } = string.Empty;
}
