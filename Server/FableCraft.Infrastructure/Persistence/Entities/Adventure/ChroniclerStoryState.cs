using System.Text.Json.Serialization;

namespace FableCraft.Infrastructure.Persistence.Entities.Adventure;

/// <summary>
/// Chronicler story state tracking narrative elements: dramatic questions, promises, threads, stakes, windows, and world momentum.
/// </summary>
public sealed class ChroniclerStoryState
{
    [JsonPropertyName("story_state")]
    public required StoryState StoryState { get; set; }

    [JsonExtensionData]
    public Dictionary<string, object> AdditionalData { get; init; } = new();
}

public sealed class StoryState
{
    [JsonPropertyName("world_momentum")]
    public object? WorldMomentum { get; set; }

    [JsonExtensionData]
    public Dictionary<string, object> AdditionalData { get; init; } = new();
}