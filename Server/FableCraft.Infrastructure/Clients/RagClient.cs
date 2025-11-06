using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace FableCraft.Infrastructure.Clients;

public enum DataType
{
    Text,
    Json
}

public interface IRagBuilder
{
    Task<AddDataResponse> AddDataAsync(AddDataRequest request);

    Task<TaskStatusResponse> GetTaskStatusAsync(string taskId, CancellationToken cancellationToken = default);

    Task<EpisodeResponse> GetEpisodeAsync(string episodeId, CancellationToken cancellationToken = default);

    Task DeleteDataAsync(string dataId, CancellationToken cancellationToken = default);

    Task ClearAsync(CancellationToken cancellationToken = default);
}

public interface IRagSearch
{
    Task<SearchResult> SearchAsync(string adventureId, string query, CancellationToken cancellationToken = default);
}

internal class RagClient : IRagBuilder, IRagSearch
{
    private readonly HttpClient _httpClient;

    public RagClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    ///     Search the graph database
    /// </summary>
    public async Task<SearchResult> SearchAsync(string adventureId, string query, CancellationToken cancellationToken = default)
    {
        SearchRequest request = new()
        {
            AdventureId = adventureId,
            Query = query,
        };

        HttpResponseMessage response = await _httpClient.PostAsJsonAsync("/search", request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<SearchResult>(cancellationToken)
               ?? new SearchResult
               {
                   Content = string.Empty
               };
    }

    /// <summary>
    ///     Add data to the graph database (returns immediately with task info)
    /// </summary>
    public async Task<AddDataResponse> AddDataAsync(AddDataRequest request)
    {
        HttpResponseMessage response = await _httpClient.PostAsJsonAsync("/add", request);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<AddDataResponse>()
               ?? throw new InvalidOperationException("Failed to deserialize AddDataResponse");
    }

    /// <summary>
    ///     Get the status of a background task
    /// </summary>
    public async Task<TaskStatusResponse> GetTaskStatusAsync(string taskId, CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response = await _httpClient.GetAsync($"/task/{Uri.EscapeDataString(taskId)}", cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<TaskStatusResponse>(cancellationToken)
               ?? throw new InvalidOperationException("Failed to deserialize TaskStatusResponse");
    }

    /// <summary>
    ///     Get episode details by episode ID
    /// </summary>
    public async Task<EpisodeResponse> GetEpisodeAsync(string episodeId, CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response = await _httpClient.GetAsync($"/episode/{Uri.EscapeDataString(episodeId)}", cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<EpisodeResponse>(cancellationToken)
               ?? throw new InvalidOperationException("Failed to deserialize EpisodeResponse");
    }

    /// <summary>
    ///     Delete data from the graph database by episode ID
    /// </summary>
    public async Task DeleteDataAsync(string dataId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.DeleteAsync($"/delete_data?episode_id={Uri.EscapeDataString(dataId)}", cancellationToken);
        try
        {
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            // Good to go, data already doesn't exist
        }
    }

    /// <summary>
    ///     Clear all data from the graph database
    /// </summary>
    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response = await _httpClient.DeleteAsync("/clear", cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}

public class SearchRequest
{
    [JsonPropertyName("adventure_id")]
    public required string AdventureId { get; set; }

    [JsonPropertyName("query")]
    public required string Query { get; set; }

    [JsonPropertyName("character_name")]
    public string? CharacterName { get; set; }
}

public class SearchResult
{
    [JsonPropertyName("content")]
    public string? Content { get; set; }
}

public class AddDataRequest
{
    [JsonPropertyName("episode_type")]
    public required string EpisodeType { get; set; }

    [JsonPropertyName("description")]
    public required string Description { get; set; }

    [JsonPropertyName("content")]
    public required string Content { get; set; }

    [JsonPropertyName("group_id")]
    public required string GroupId { get; set; }

    [JsonPropertyName("task_id")]
    public required string TaskId { get; set; }

    [JsonPropertyName("reference_time")]
    public DateTime? ReferenceTime { get; set; }
}

public class AddDataResponse
{
    [JsonPropertyName("status")]
    public string? Status { get; set; }
}

public enum TaskStatus
{
    Pending,
    Processing,
    Completed,
    Failed
}

public class TaskStatusResponse
{
    [JsonPropertyName("episode_id")]
    public required string EpisodeId { get; set; }

    [JsonPropertyName("status")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public TaskStatus Status { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("completed_at")]
    public DateTime? CompletedAt { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}

public class EpisodeResponse
{
    [JsonPropertyName("uuid")]
    public required string Uuid { get; set; }

    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("group_id")]
    public required string GroupId { get; set; }

    [JsonPropertyName("source")]
    public required string Source { get; set; }

    [JsonPropertyName("source_description")]
    public required string SourceDescription { get; set; }

    [JsonPropertyName("content")]
    public required string Content { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("valid_at")]
    public DateTime ValidAt { get; set; }

    [JsonPropertyName("entity_edges")]
    public required List<string> EntityEdges { get; set; }
}
