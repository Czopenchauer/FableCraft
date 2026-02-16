using System.Text.Json.Serialization;

namespace FableCraft.Infrastructure.Persistence.Entities.Adventure;

public class CreationRequests
{
    [JsonPropertyName("characters")]
    public List<CharacterRequest> Characters { get; set; } = new();

    [JsonPropertyName("lore")]
    public List<LoreRequest> Lore { get; set; } = new();

    [JsonPropertyName("items")]
    public List<ItemRequest> Items { get; set; } = new();

    [JsonPropertyName("locations")]
    public List<LocationRequest> Locations { get; set; } = new();
}

public class CharacterRequest
{
    public Guid? CharacterId { get; set; }

    [JsonPropertyName("importance")]
    public CharacterImportance Importance { get; set; }

    [JsonExtensionData]
    public Dictionary<string, object> AdditionalData { get; set; } = new();
}

public class LoreRequest
{
    [JsonPropertyName("processed")]
    public bool Processed { get; set; }

    [JsonExtensionData]
    public Dictionary<string, object> AdditionalData { get; set; } = new();
}

public class ItemRequest
{
    [JsonPropertyName("processed")]
    public bool Processed { get; set; }

    [JsonExtensionData]
    public Dictionary<string, object> AdditionalData { get; set; } = new();
}

public class LocationRequest
{
    [JsonPropertyName("processed")]
    public bool Processed { get; set; }

    [JsonExtensionData]
    public Dictionary<string, object> AdditionalData { get; set; } = new();
}