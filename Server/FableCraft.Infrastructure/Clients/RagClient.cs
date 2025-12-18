using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

using FableCraft.Infrastructure.Queue;

namespace FableCraft.Infrastructure.Clients;

public static class RagClientExtensions
{
    public static string GetCharacterDatasetName(Guid adventureId, Guid characterId)
    {
        return $"{adventureId}_{characterId}";
    }

    public static string GetWorldDatasetName(Guid adventureId)
    {
        return $"{adventureId}_world";
    }

    public static string GetMainCharacterDatasetName(Guid adventureId)
    {
        return $"{adventureId}_main_character";
    }
}

public readonly struct SearchType : IEquatable<SearchType>
{
    public readonly static SearchType Summaries = new("SUMMARIES");
    public readonly static SearchType Chunks = new("CHUNKS");
    public readonly static SearchType RagCompletion = new("RAG_COMPLETION");
    public readonly static SearchType GraphCompletion = new("GRAPH_COMPLETION");
    public readonly static SearchType GraphSummaryCompletion = new("GRAPH_SUMMARY_COMPLETION");
    public readonly static SearchType Code = new("CODE");
    public readonly static SearchType Cypher = new("CYPHER");
    public readonly static SearchType NaturalLanguage = new("NATURAL_LANGUAGE");
    public readonly static SearchType GraphCompletionCot = new("GRAPH_COMPLETION_COT");
    public readonly static SearchType GraphCompletionContextExtension = new("GRAPH_COMPLETION_CONTEXT_EXTENSION");
    public readonly static SearchType FeelingLucky = new("FEELING_LUCKY");
    public readonly static SearchType Feedback = new("FEEDBACK");
    public readonly static SearchType Temporal = new("TEMPORAL");
    public readonly static SearchType CodingRules = new("CODING_RULES");
    public readonly static SearchType ChunksLexical = new("CHUNKS_LEXICAL");

    public string Value { get; }

    private SearchType(string value) => Value = value;

    public override string ToString() => Value;
    public override int GetHashCode() => Value.GetHashCode();
    public override bool Equals(object? obj) => obj is SearchType other && Equals(other);
    public bool Equals(SearchType other) => Value == other.Value;

    public static bool operator ==(SearchType left, SearchType right) => left.Equals(right);
    public static bool operator !=(SearchType left, SearchType right) => !left.Equals(right);

    public static implicit operator string(SearchType searchType) => searchType.Value;
}

public interface IRagBuilder
{
    Task<Dictionary<string, Dictionary<string, string>>> AddDataAsync(List<string> content, List<string> datasets, CancellationToken cancellationToken = default);

    Task CognifyAsync(List<string> datasets, bool temporal = false, CancellationToken cancellationToken = default);

    Task MemifyAsync(List<string> datasets, CancellationToken cancellationToken = default);

    Task<List<DatasetData>> GetDatasetsAsync(string dataset, CancellationToken cancellationToken = default);

    Task UpdateDataAsync(string dataset, string dataId, string content, CancellationToken cancellationToken = default);

    Task DeleteNodeAsync(string datasetName, string dataId, CancellationToken cancellationToken = default);

    Task DeleteDatasetAsync(string dataset, CancellationToken cancellationToken = default);

    Task CleanAsync(CancellationToken cancellationToken = default);
}

public record SearchResult(string Query, SearchResponse Response);

public interface IRagSearch
{
    Task<SearchResult[]> SearchAsync(CallerContext context, List<string> datasets, string[] query, SearchType? searchType = null,
        CancellationToken cancellationToken = default);
}

internal class RagClient : IRagBuilder, IRagSearch
{
    private readonly HttpClient _httpClient;
    private readonly IMessageDispatcher _messageDispatcher;

    public RagClient(HttpClient httpClient, IMessageDispatcher messageDispatcher)
    {
        _httpClient = httpClient;
        _messageDispatcher = messageDispatcher;
    }

    public async Task<Dictionary<string, Dictionary<string, string>>> AddDataAsync(List<string> content, List<string> datasets, CancellationToken cancellationToken = default)
    {
        var request = new AddDataRequest
        {
            Content = content,
            datasets = datasets
        };

        HttpResponseMessage response = await _httpClient.PostAsJsonAsync("/add", request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<Dictionary<string, Dictionary<string, string>>>(cancellationToken)
               ?? throw new InvalidOperationException("Failed to deserialize response");
    }

    public async Task CognifyAsync(List<string> datasets, bool temporal = false, CancellationToken cancellationToken = default)
    {
        var request = new CognifyRequest { Datasets = datasets, Temporal = temporal };
        HttpResponseMessage response = await _httpClient.PostAsJsonAsync("/cognify", request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task MemifyAsync(List<string> datasets, CancellationToken cancellationToken = default)
    {
        var request = new MemifyRequest { datasets = datasets };
        HttpResponseMessage response = await _httpClient.PostAsJsonAsync("/memify", request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<List<DatasetData>> GetDatasetsAsync(string dataset, CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response = await _httpClient.GetAsync($"/datasets/{Uri.EscapeDataString(dataset)}", cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<List<DatasetData>>(cancellationToken)
               ?? new List<DatasetData>();
    }

    public async Task UpdateDataAsync(string dataset, string dataId, string content, CancellationToken cancellationToken = default)
    {
        var request = new UpdateDataRequest
        {
            dataset = dataset,
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
        catch (HttpRequestException e) when (e.StatusCode == HttpStatusCode.NotFound)
        {
        }
    }

    public async Task DeleteDatasetAsync(string dataset, CancellationToken cancellationToken = default)
    {
        try
        {
            HttpResponseMessage response = await _httpClient.DeleteAsync($"/delete/{Uri.EscapeDataString(dataset)}", cancellationToken);
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException e) when (e.StatusCode == HttpStatusCode.NotFound)
        {
        }
    }

    public async Task CleanAsync(CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response = await _httpClient.DeleteAsync("/nuke", cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<SearchResult[]> SearchAsync(CallerContext context, List<string> datasets, string[] queries, SearchType? searchType = null,
        CancellationToken cancellationToken = default)
    {
        var type = searchType ?? SearchType.GraphCompletion;
        var request = queries.Select(async query =>
        {
            HttpResponseMessage response = await _httpClient.PostAsJsonAsync("/search",
                new SearchRequest
                {
                    Datasets = datasets,
                    Query = query,
                    SearchType = type
                },
                cancellationToken);

            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<SearchResponse>(cancellationToken);
            return (query, result ?? new SearchResponse { Results = new List<SearchResultItem>() });
        }).ToArray();

        var results = await Task.WhenAll(request);
        return results.Select(x => new SearchResult(x.query, x.Item2)).ToArray();
    }
}

public class AddDataRequest
{
    [JsonPropertyName("content")]
    public required List<string> Content { get; set; }

    [JsonPropertyName("adventure_ids")]
    public required List<string> datasets { get; set; }
}

public class CognifyRequest
{
    [JsonPropertyName("adventure_ids")]
    public required List<string> Datasets { get; set; }

    [JsonPropertyName("temporal")]
    public bool Temporal { get; set; }
}

public class MemifyRequest
{
    [JsonPropertyName("adventure_ids")]
    public required List<string> datasets { get; set; }
}

public class UpdateDataRequest
{
    [JsonPropertyName("adventure_id")]
    public required string dataset { get; set; }

    [JsonPropertyName("data_id")]
    public required Guid DataId { get; set; }

    [JsonPropertyName("content")]
    public required string Content { get; set; }
}

public class SearchRequest
{
    [JsonPropertyName("adventure_ids")]
    public required List<string> Datasets { get; set; }

    [JsonPropertyName("query")]
    public required string Query { get; set; }

    [JsonPropertyName("search_type")]
    public required string SearchType { get; set; }
}

public class SearchResultItem
{
    [JsonPropertyName("dataset_name")]
    public string DatasetName { get; set; } = string.Empty;

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
}

public class SearchResponse
{
    [JsonPropertyName("results")]
    public List<SearchResultItem> Results { get; set; } = new();
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