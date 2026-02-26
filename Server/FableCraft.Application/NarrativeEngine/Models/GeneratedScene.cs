using System.Text.Json.Serialization;

using FableCraft.Infrastructure.Persistence.Entities.Adventure;

namespace FableCraft.Application.NarrativeEngine.Models;

internal class GeneratedScene
{
    [JsonPropertyName("scene")]
    public string Scene { get; init; } = null!;

    [JsonPropertyName("choices")]
    public string[] Choices { get; init; } = null!;

    [JsonPropertyName("creation_requests")]
    public CreationRequests? CreationRequests { get; init; }

    [JsonPropertyName("importance_flags")]
    public ImportanceFlags? ImportanceFlags { get; init; }

    /// <summary>
    ///     Dispatches sent by the MC during this scene.
    /// </summary>
    [JsonPropertyName("dispatches")]
    public List<OutgoingDispatch>? Dispatches { get; init; }

    /// <summary>
    ///     Dispatch resolutions from the MC acknowledging incoming messages.
    /// </summary>
    [JsonPropertyName("dispatches_resolved")]
    public List<DispatchResolution>? DispatchesResolved { get; init; }

    [JsonExtensionData]
    public Dictionary<string, object> AdditionalData { get; set; } = new();
}