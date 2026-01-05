using System.Text.Json.Serialization;

namespace FableCraft.Infrastructure.Persistence.Entities.Adventure;

/// <summary>
/// Chronicler story state tracking narrative elements: dramatic questions, promises, threads, stakes, windows, and world momentum.
/// </summary>
public sealed class ChroniclerStoryState
{
    [JsonExtensionData]
    public Dictionary<string, object> AdditionalData { get; init; } = new();
}