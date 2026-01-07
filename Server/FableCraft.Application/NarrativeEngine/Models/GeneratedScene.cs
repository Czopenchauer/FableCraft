using System.Text.Json.Serialization;

using FableCraft.Infrastructure.Persistence.Entities.Adventure;

namespace FableCraft.Application.NarrativeEngine.Models;

public class GeneratedScene
{
    [JsonPropertyName("scene")]
    public string Scene { get; init; } = null!;

    [JsonPropertyName("choices")]
    public string[] Choices { get; init; } = null!;

    [JsonPropertyName("creation_requests")]
    public CreationRequests? CreationRequests { get; init; }

    [JsonPropertyName("importance_flags")]
    public ImportanceFlags? ImportanceFlags { get; init; }

    [JsonExtensionData]
    public Dictionary<string, object> AdditionalData { get; set; } = new();
}