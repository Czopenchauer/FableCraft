using System.Text.Json.Serialization;

using FableCraft.Infrastructure.Persistence.Entities.Adventure;

namespace FableCraft.Application.NarrativeEngine.Models;

/// <summary>
///     Complete output from the ChroniclerAgent.
/// </summary>
internal sealed class ChroniclerOutput
{
    [JsonPropertyName("writer_guidance")]
    public WriterGuidance WriterGuidance { get; init; } = null!;

    [JsonPropertyName("story_state")]
    public ChroniclerStoryState StoryState { get; init; } = null!;

    [JsonPropertyName("world_events")]
    public WorldEvent[] WorldEvents { get; init; } = [];

    [JsonPropertyName("lore_requests")]
    public ChroniclerLoreRequest[] LoreRequests { get; init; } = [];
}

/// <summary>
///     Narrative-aware guidance for the Writer agent.
/// </summary>
internal sealed class WriterGuidance
{
    [JsonExtensionData]
    public Dictionary<string, object> AdditionalData { get; init; } = new();
}

/// <summary>
///     A world event to be recorded as a discoverable fact.
/// </summary>
internal sealed class WorldEvent
{
    [JsonPropertyName("when")]
    public string When { get; init; } = null!;

    [JsonPropertyName("where")]
    public string Where { get; init; } = null!;

    [JsonPropertyName("event")]
    public string Event { get; init; } = null!;
}

/// <summary>
///     A request from the Chronicler to create lore when world momentum implies missing knowledge.
/// </summary>
internal sealed class ChroniclerLoreRequest
{
    [JsonExtensionData]
    public Dictionary<string, object> AdditionalData { get; init; } = new();
}