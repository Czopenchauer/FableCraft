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
    Task<string> AddDataAsync(AddDataRequest request, CancellationToken cancellationToken = default);

    Task DeleteDataAsync(string dataId, CancellationToken cancellationToken = default);

    Task ClearAsync(CancellationToken cancellationToken = default);
}

public interface IRagSearch
{
    Task<SearchResult[]> SearchAsync(string query, string? characterName = null,
        CancellationToken cancellationToken = default);
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
    public async Task<SearchResult[]> SearchAsync(string query, string? characterName = null,
        CancellationToken cancellationToken = default)
    {
        SearchRequest request = new()
        {
            Query = query,
            CharacterName = characterName
        };

        HttpResponseMessage response = await _httpClient.PostAsJsonAsync("/search", request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<SearchResult[]>(cancellationToken)
               // ReSharper disable once UseCollectionExpression
               ?? Array.Empty<SearchResult>();
    }

    /// <summary>
    ///     Add data to the graph database
    /// </summary>
    public async Task<string> AddDataAsync(AddDataRequest request, CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response = await _httpClient.PostAsJsonAsync("/add", request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<string>(cancellationToken)
               ?? string.Empty;
    }

    /// <summary>
    ///     Delete data from the graph database by episode ID
    /// </summary>
    public async Task DeleteDataAsync(string dataId, CancellationToken cancellationToken = default)
    {
        DeleteRequest request = new() { EpisodeId = dataId };
        HttpResponseMessage response = await _httpClient.PostAsJsonAsync("/delete_data", request, cancellationToken);
        response.EnsureSuccessStatusCode();
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
    [JsonPropertyName("query")]
    public required string Query { get; set; }

    [JsonPropertyName("character_name")]
    public string? CharacterName { get; set; }
}

public class SearchResult
{
    [JsonPropertyName("uuid")]
    public string? Uuid { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("content")]
    public string? Content { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime? CreatedAt { get; set; }

    [JsonPropertyName("group_id")]
    public string? GroupId { get; set; }

    [JsonPropertyName("source")]
    public string? Source { get; set; }
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

    [JsonPropertyName("reference_time")]
    public DateTime? ReferenceTime { get; set; }
}

public class DeleteRequest
{
    [JsonPropertyName("episode_id")]
    public required string EpisodeId { get; set; }
}