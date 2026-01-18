using System.Text.Json.Serialization;

namespace FableCraft.Application.NarrativeEngine.Models;

internal class ContextBase
{
    public ContextItem[] WorldContext { get; set; } = [];

    public ContextItem[] NarrativeContext { get; set; } = [];

    public string[] BackgroundRoster { get; set; } = [];

    [JsonExtensionData]
    public Dictionary<string, object> AdditionalData { get; set; } = new();
}