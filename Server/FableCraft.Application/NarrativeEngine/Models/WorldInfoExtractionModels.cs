using System.Text.Json.Serialization;

using FableCraft.Infrastructure.Persistence.Entities.Adventure;

namespace FableCraft.Application.NarrativeEngine.Models;

internal sealed class WorldInfoExtractionOutput
{
    [JsonPropertyName("activity")]
    public List<ActivityExtraction> Activity { get; set; } = [];

    [JsonPropertyName("world_facts")]
    public List<WorldFactExtraction> WorldFacts { get; set; } = [];
}

internal sealed class ActivityExtraction
{
    [JsonPropertyName("time")]
    public string Time { get; init; } = null!;

    [JsonPropertyName("location")]
    public string Location { get; init; } = null!;

    [JsonPropertyName("who")]
    public string[] Who { get; init; } = [];

    [JsonPropertyName("witnesses")]
    public string[]? Witnesses { get; init; }

    [JsonPropertyName("what")]
    public string What { get; init; } = null!;

    [JsonPropertyName("information_exchanged")]
    public string? InformationExchanged { get; init; }
}

internal sealed class WorldFactExtraction
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = null!;

    [JsonPropertyName("content")]
    public string Content { get; init; } = null!;
}

internal sealed class AlreadyHandledContent
{
    public List<CharacterRequest>? Characters { get; init; }
    public List<LocationRequest>? Locations { get; init; }
    public List<ItemRequest>? Items { get; init; }
    public List<LoreRequest>? Lore { get; init; }
    public List<WorldEvent>? WorldEvents { get; init; }
    public List<GeneratedPartialProfile>? BackgroundCharacters { get; init; }
}
