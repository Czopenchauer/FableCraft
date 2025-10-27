using System.Net.Http.Json;
using System.Text.Json;

namespace FableCraft.Application.Clients;

internal class RagClient
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public RagClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            WriteIndented = false
        };
    }

    /// <summary>
    /// Build an index from text data for search and retrieval.
    /// </summary>
    public async Task<BuildIndexResponse> BuildIndexAsync(
        string text,
        string datasetName,
        Guid? datasetId = null,
        string? ontology = null,
        CancellationToken cancellationToken = default)
    {
        var request = new BuildIndexRequest
        {
            Text = text,
            DatasetName = datasetName,
            DatasetId = datasetId,
            Ontology = ontology
        };

        var response = await _httpClient.PostAsJsonAsync("/build_index", request, _jsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<BuildIndexResponse>(_jsonOptions, cancellationToken)
               ?? throw new InvalidOperationException("Failed to deserialize build index response");
    }

    /// <summary>
    /// Get the current build status of a specific index.
    /// </summary>
    public async Task<PipelineStatusResponse> GetPipelineStatusAsync(
        Guid datasetId,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"/pipeline_status/{datasetId}", cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<PipelineStatusResponse>(_jsonOptions, cancellationToken)
               ?? throw new InvalidOperationException("Failed to deserialize pipeline status response");
    }

    /// <summary>
    /// Perform a search using the GraphRAG system.
    /// </summary>
    public async Task<SearchResponse> SearchAsync(
        string query,
        string queryType,
        int topK,
        string? systemPrompt = null,
        List<Guid>? datasetIds = null,
        CancellationToken cancellationToken = default)
    {
        var request = new SearchRequest
        {
            Query = query,
            QueryType = queryType,
            SystemPrompt = systemPrompt,
            TopK = topK,
            DatasetIds = datasetIds
        };

        var response = await _httpClient.PostAsJsonAsync("/search", request, _jsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<SearchResponse>(_jsonOptions, cancellationToken)
               ?? throw new InvalidOperationException("Failed to deserialize search response");
    }

    /// <summary>
    /// Delete specific data from a dataset.
    /// </summary>
    public async Task DeleteDataAsync(
        Guid datasetId,
        Guid dataId,
        CancellationToken cancellationToken = default)
    {
        var request = new DeleteRequest
        {
            DatasetId = datasetId,
            DataId = dataId
        };

        var response = await _httpClient.SendAsync(
            new HttpRequestMessage(HttpMethod.Delete, "/delete")
            {
                Content = JsonContent.Create(request, options: _jsonOptions)
            },
            cancellationToken);

        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Generate a visualization of the knowledge graph and return HTML content.
    /// </summary>
    public async Task<string> VisualizeGraphAsync(
        string? outputPath = null,
        CancellationToken cancellationToken = default)
    {
        var request = new VisualizeGraphRequest { OutputPath = outputPath };

        var response = await _httpClient.PostAsJsonAsync("/visualize_graph", request, _jsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();

        if (outputPath != null)
        {
            var result =
                await response.Content.ReadFromJsonAsync<VisualizeGraphFileResponse>(_jsonOptions, cancellationToken);

            return result?.Path ?? throw new InvalidOperationException("Failed to get visualization file path");
        }

        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    /// <summary>
    /// Health check to verify the API is running.
    /// </summary>
    public async Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/health", cancellationToken);

            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}

internal static class SearchType
{
    public const string Summaries = "SUMMARIES";
    public const string Chunks = "CHUNKS";
    public const string RagCompletion = "RAG_COMPLETION";
    public const string GraphCompletion = "GRAPH_COMPLETION";
    public const string GraphSummaryCompletion = "GRAPH_SUMMARY_COMPLETION";
    public const string Code = "CODE";
    public const string Cypher = "CYPHER";
    public const string NaturalLanguage = "NATURAL_LANGUAGE";
    public const string GraphCompletionCot = "GRAPH_COMPLETION_COT";
    public const string GraphCompletionContextExtension = "GRAPH_COMPLETION_CONTEXT_EXTENSION";
    public const string FeelingLucky = "FEELING_LUCKY";
    public const string Feedback = "FEEDBACK";
    public const string Temporal = "TEMPORAL";
    public const string CodingRules = "CODING_RULES";
    public const string ChunksLexical = "CHUNKS_LEXICAL";
}

internal record BuildIndexRequest
{
    public required string Text { get; init; }

    public required string DatasetName { get; init; }

    public Guid? DatasetId { get; init; }

    public string? Ontology { get; init; }
}

internal record SearchRequest
{
    public required string Query { get; init; }

    public required string QueryType { get; init; }

    public string? SystemPrompt { get; init; }

    public required int TopK { get; init; }

    public List<Guid>? DatasetIds { get; init; }
}

internal record DeleteRequest
{
    public required Guid DatasetId { get; init; }

    public required Guid DataId { get; init; }
}

internal record VisualizeGraphRequest
{
    public string? OutputPath { get; init; }
}

internal record BuildIndexResponse
{
    public required Guid DatasetId { get; init; }

    public required Guid DataId { get; init; }
}

internal record PipelineStatusResponse
{
    public required Guid DatasetId { get; init; }

    public required string Status { get; init; }
}

internal record SearchResponse
{
    public required List<string> Results { get; init; }
}

internal record VisualizeGraphFileResponse
{
    public required string Message { get; init; }

    public required string Path { get; init; }
}