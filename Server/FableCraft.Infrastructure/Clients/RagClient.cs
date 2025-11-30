using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace FableCraft.Infrastructure.Clients;

public interface IRagBuilder
{
    Task<Dictionary<string, string>> AddDataAsync(List<string> content, string adventureId, CancellationToken cancellationToken = default);

    Task CognifyAsync(string adventureId, CancellationToken cancellationToken = default);

    Task MemifyAsync(string adventureId, CancellationToken cancellationToken = default);

    Task<List<DatasetData>> GetDatasetsAsync(string adventureId, CancellationToken cancellationToken = default);

    Task UpdateDataAsync(string adventureId, string dataId, string content, CancellationToken cancellationToken = default);

    Task DeleteNodeAsync(string datasetName, string dataId, CancellationToken cancellationToken = default);

    Task DeleteDatasetAsync(string adventureId, CancellationToken cancellationToken = default);

    Task CleanAsync(CancellationToken cancellationToken = default);
}

public interface IRagSearch
{
    Task<SearchResponse> SearchAsync(CallerContext context, string query, string searchType = "GRAPH_COMPLETION", CancellationToken cancellationToken = default);
}

internal class RagClient : IRagBuilder, IRagSearch
{
    private readonly HttpClient _httpClient;

    public RagClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<Dictionary<string, string>> AddDataAsync(List<string> content, string adventureId, CancellationToken cancellationToken = default)
    {
        var request = new AddDataRequest
        {
            Content = content,
            AdventureId = adventureId
        };

        HttpResponseMessage response = await _httpClient.PostAsJsonAsync("/add", request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<Dictionary<string, string>>(cancellationToken: cancellationToken)
               ?? throw new InvalidOperationException("Failed to deserialize response");
    }

    public async Task CognifyAsync(string adventureId, CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response = await _httpClient.PostAsync($"/cognify/{Uri.EscapeDataString(adventureId)}", null, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task MemifyAsync(string adventureId, CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response = await _httpClient.PostAsync($"/memify/{Uri.EscapeDataString(adventureId)}", null, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<List<DatasetData>> GetDatasetsAsync(string adventureId, CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response = await _httpClient.GetAsync($"/datasets/{Uri.EscapeDataString(adventureId)}", cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<List<DatasetData>>(cancellationToken: cancellationToken)
               ?? new List<DatasetData>();
    }

    public async Task UpdateDataAsync(string adventureId, string dataId, string content, CancellationToken cancellationToken = default)
    {
        var request = new UpdateDataRequest
        {
            AdventureId = adventureId,
            DataId = Guid.Parse(dataId),
            Content = content
        };

        HttpResponseMessage response = await _httpClient.PutAsJsonAsync("/update", request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteNodeAsync(string datasetName, string dataId, CancellationToken cancellationToken = default)
    {
        try
        {
            HttpResponseMessage response = await _httpClient.DeleteAsync($"/delete/node/{Uri.EscapeDataString(datasetName)}/{dataId}", cancellationToken);
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException e) when (e.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
        }
    }

    public async Task DeleteDatasetAsync(string adventureId, CancellationToken cancellationToken = default)
    {
        try
        {
            HttpResponseMessage response = await _httpClient.DeleteAsync($"/delete/{Uri.EscapeDataString(adventureId)}", cancellationToken);
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException e) when (e.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
        }
    }

    public async Task CleanAsync(CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response = await _httpClient.DeleteAsync("/nuke", cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<SearchResponse> SearchAsync(CallerContext context, string query, string searchType = "GRAPH_COMPLETION", CancellationToken cancellationToken = default)
    {
        var request = new SearchRequest
        {
            AdventureId = context.AdventureId.ToString(),
            Query = query,
            SearchType = searchType
        };

        HttpResponseMessage response = await _httpClient.PostAsJsonAsync("/search", request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<SearchResponse>(cancellationToken: cancellationToken)
               ?? new SearchResponse { Results = new List<string>() };
    }
}

public class AddDataRequest
{
    [JsonPropertyName("content")]
    public required List<string> Content { get; set; }

    [JsonPropertyName("adventure_id")]
    public required string AdventureId { get; set; }
}

public class UpdateDataRequest
{
    [JsonPropertyName("adventure_id")]
    public required string AdventureId { get; set; }

    [JsonPropertyName("data_id")]
    public required Guid DataId { get; set; }

    [JsonPropertyName("content")]
    public required string Content { get; set; }
}

public class SearchRequest
{
    [JsonPropertyName("adventure_id")]
    public required string AdventureId { get; set; }

    [JsonPropertyName("query")]
    public required string Query { get; set; }

    [JsonPropertyName("search_type")]
    public required string SearchType { get; set; }
}

public class SearchResponse
{
    [JsonPropertyName("results")]
    public List<string> Results { get; set; } = new();
}

public class VisualizeRequest
{
    [JsonPropertyName("path")]
    public required string Path { get; set; }
}

public class DatasetData
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}