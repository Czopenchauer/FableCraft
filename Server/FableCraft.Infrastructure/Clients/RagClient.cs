using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace FableCraft.Infrastructure.Clients;

public interface IRagBuilder
{
    Task<AddDataResponse> AddDataAsync(string content, string adventureId, CancellationToken cancellationToken = default);

    Task<AddDataResponse> AddDataBatchAsync(List<string> content, string adventureId, CancellationToken cancellationToken = default);

    Task DeleteNodeAsync(string datasetId, string dataId, CancellationToken cancellationToken = default);

    Task DeleteDatasetAsync(string datasetId, CancellationToken cancellationToken = default);

    Task CleanAsync(CancellationToken cancellationToken = default);
}

public interface IRagSearch
{
    Task<List<string>> SearchAsync(string adventureId, string query, CancellationToken cancellationToken = default);
}

internal class RagClient : IRagBuilder, IRagSearch
{
    private readonly HttpClient _httpClient;

    public RagClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<AddDataResponse> AddDataAsync(string content, string adventureId, CancellationToken cancellationToken = default)
    {
        var request = new AddDataRequest
        {
            Content = content,
            AdventureId = adventureId
        };

        HttpResponseMessage response = await _httpClient.PostAsJsonAsync("/add", request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<AddDataResponse>(cancellationToken: cancellationToken)
               ?? throw new InvalidOperationException("Failed to deserialize AddDataResponse");
    }

    public async Task<AddDataResponse> AddDataBatchAsync(List<string> content, string adventureId, CancellationToken cancellationToken = default)
    {
        var request = new AddDataBatchRequest
        {
            Content = content,
            AdventureId = adventureId
        };

        HttpResponseMessage response = await _httpClient.PostAsJsonAsync("/add-batch", request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<AddDataResponse>(cancellationToken: cancellationToken)
               ?? throw new InvalidOperationException("Failed to deserialize AddDataResponse");
    }

    public async Task DeleteNodeAsync(string datasetId, string dataId, CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response = await _httpClient.DeleteAsync($"/delete/node/{Uri.EscapeDataString(datasetId)}/{Uri.EscapeDataString(dataId)}", cancellationToken);
        response.EnsureSuccessStatusCode();
    }
    
    public async Task DeleteDatasetAsync(string datasetId, CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response = await _httpClient.DeleteAsync($"/delete/{Uri.EscapeDataString(datasetId)}", cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task CleanAsync(CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response = await _httpClient.DeleteAsync("/nuke", cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<List<string>> SearchAsync(string adventureId, string query, CancellationToken cancellationToken = default)
    {
        var request = new SearchRequest
        {
            AdventureId = adventureId,
            Query = query
        };

        HttpResponseMessage response = await _httpClient.PostAsJsonAsync("/search", request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<List<string>>(cancellationToken: cancellationToken)
               ?? new List<string>();
    }
}

public class AddDataRequest
{
    [JsonPropertyName("content")]
    public required string Content { get; set; }

    [JsonPropertyName("adventure_id")]
    public required string AdventureId { get; set; }
}

public class AddDataBatchRequest
{
    [JsonPropertyName("content")]
    public required List<string> Content { get; set; }

    [JsonPropertyName("adventure_id")]
    public required string AdventureId { get; set; }
}

public class SearchRequest
{
    [JsonPropertyName("adventure_id")]
    public required string AdventureId { get; set; }

    [JsonPropertyName("query")]
    public required string Query { get; set; }
}

public class AddDataResponse
{
    [JsonPropertyName("status")]
    public required string Status { get; set; }

    [JsonPropertyName("pipeline_run_id")]
    public required string PipelineRunId { get; set; }

    [JsonPropertyName("dataset_id")]
    public required string DatasetId { get; set; }

    [JsonPropertyName("dataset_name")]
    public required string DatasetName { get; set; }

    [JsonPropertyName("data_ingestion_info")]
    public List<DataIngestionInfo> DataIngestionInfo { get; set; } = new();
    
    public string GetDataId() => DataIngestionInfo.Single(x => x.RunInfo.Status == "PipelineRunCompleted").DataId;
}

public class DataIngestionInfo
{
    [JsonPropertyName("run_info")]
    public required RunInfo RunInfo { get; set; }

    [JsonPropertyName("data_id")]
    public required string DataId { get; set; }
}

public class RunInfo
{
    [JsonPropertyName("status")]
    public required string Status { get; set; }

    [JsonPropertyName("pipeline_run_id")]
    public required string PipelineRunId { get; set; }

    [JsonPropertyName("dataset_id")]
    public required string DatasetId { get; set; }

    [JsonPropertyName("dataset_name")]
    public required string DatasetName { get; set; }
}
