using System.Text.Json.Serialization;

namespace FableCraft.Infrastructure.Persistence.Entities.Adventure;

public sealed class Character
{
    public Guid Id { get; set; }

    public Guid CharacterId { get; set; }

    public Guid AdventureId { get; set; }

    public Guid SceneId { get; set; }

    public int SequenceNumber { get; set; }

    public required string Description { get; set; }

    public CharacterStats CharacterStats { get; init; } = null!;

    public CharacterTracker Tracker { get; init; } = null!;

    public CharacterDevelopmentTracker DevelopmentTracker { get; init; } = null!;
}

public class CharacterStats
{
    [JsonPropertyName("character_identity")]
    public required CharacterIdentity CharacterIdentity { get; set; }
    
    [JsonExtensionData]
    public IDictionary<string, object>? ExtensionData { get; set; }
}

public class CharacterIdentity
{
    [JsonPropertyName("full_name")]
    public string? FullName { get; set; }
}