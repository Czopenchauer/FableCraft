using System.Text.Json.Serialization;

namespace FableCraft.Infrastructure.Persistence.Entities.Adventure;

public class NarrativeDirectorOutput
{
    [JsonPropertyName("writer_instructions")]
    public WriterInstructions WriterInstructions { get; set; } = new();

    [JsonPropertyName("creation_requests")]
    public CreationRequests CreationRequests { get; set; } = new();

    [JsonPropertyName("narrative_tracking")]
    public NarrativeTracking NarrativeTracking { get; set; } = new();
}

public class WriterInstructions
{
    [JsonExtensionData]
    public Dictionary<string, object> AdditionalData { get; set; } = new();
}

public class NarrativeTracking
{
    [JsonExtensionData]
    public Dictionary<string, object> AdditionalData { get; set; } = new();
}

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
    [JsonPropertyName("importance")]
    public CharacterImportance Importance { get; set; }

    [JsonExtensionData]
    public Dictionary<string, object> AdditionalData { get; set; } = new();
}

public class LoreRequest
{
    [JsonExtensionData]
    public Dictionary<string, object> AdditionalData { get; set; } = new();
}

public class ItemRequest
{
    [JsonExtensionData]
    public Dictionary<string, object> AdditionalData { get; set; } = new();
}

public class LocationRequest
{
    [JsonExtensionData]
    public Dictionary<string, object> AdditionalData { get; set; } = new();
}