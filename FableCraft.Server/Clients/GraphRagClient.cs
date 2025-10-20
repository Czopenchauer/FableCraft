using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FableCraft.Server.Clients;

public class GraphRagClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GraphRagClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public GraphRagClient(HttpClient httpClient, ILogger<GraphRagClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    /// <summary>
    /// Initiates a new GraphRAG index build operation
    /// </summary>
    /// <param name="request">Index build request parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Index build response with task ID</returns>
    public async Task<IndexBuildResponse> BuildIndexAsync(
        IndexBuildRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogInformation("Initiating index build for root directory: {RootDir}", 
                request.RootDir ?? "default");

            var response = await _httpClient.PostAsync("/api/index", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<IndexBuildResponse>(responseContent, _jsonOptions);

            if (result == null)
            {
                throw new InvalidOperationException("Failed to deserialize index build response");
            }

            _logger.LogInformation("Index build started successfully. Task ID: {TaskId}", result.TaskId);
            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error occurred while building index");
            throw new GraphRagClientException("Failed to build index due to HTTP error", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while building index");
            throw new GraphRagClientException("Failed to build index", ex);
        }
    }

    /// <summary>
    /// Gets the status of an index build operation
    /// </summary>
    /// <param name="taskId">The task ID returned from BuildIndexAsync</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Current status of the indexing task</returns>
    public async Task<IndexStatusResponse> GetIndexStatusAsync(
        string taskId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(taskId))
        {
            throw new ArgumentException("Task ID cannot be null or empty", nameof(taskId));
        }

        try
        {
            _logger.LogDebug("Checking status for task: {TaskId}", taskId);

            var response = await _httpClient.GetAsync($"/api/index/{taskId}", cancellationToken);
            
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                throw new GraphRagClientException($"Task with ID '{taskId}' not found");
            }

            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<IndexStatusResponse>(responseContent, _jsonOptions);

            if (result == null)
            {
                throw new InvalidOperationException("Failed to deserialize index status response");
            }

            _logger.LogDebug("Task {TaskId} status: {Status}", taskId, result.Status);
            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error occurred while getting index status for task: {TaskId}", taskId);
            throw new GraphRagClientException($"Failed to get status for task '{taskId}'", ex);
        }
        catch (GraphRagClientException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while getting index status for task: {TaskId}", taskId);
            throw new GraphRagClientException($"Failed to get status for task '{taskId}'", ex);
        }
    }

    /// <summary>
    /// Polls the index status until completion or timeout
    /// </summary>
    /// <param name="taskId">The task ID to monitor</param>
    /// <param name="pollIntervalSeconds">Interval between status checks in seconds</param>
    /// <param name="timeoutSeconds">Maximum time to wait in seconds (0 for no timeout)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Final status response when completed or failed</returns>
    public async Task<IndexStatusResponse> WaitForIndexCompletionAsync(
        string taskId,
        int pollIntervalSeconds = 5,
        int timeoutSeconds = 300,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        var hasTimeout = timeoutSeconds > 0;

        _logger.LogInformation("Waiting for task {TaskId} to complete (poll interval: {Interval}s, timeout: {Timeout}s)",
            taskId, pollIntervalSeconds, timeoutSeconds > 0 ? timeoutSeconds : "none");

        while (!cancellationToken.IsCancellationRequested)
        {
            var status = await GetIndexStatusAsync(taskId, cancellationToken);

            if (status.Status == "completed" || status.Status == "failed" || status.Status == "error")
            {
                _logger.LogInformation("Task {TaskId} finished with status: {Status}", taskId, status.Status);
                return status;
            }

            if (hasTimeout && (DateTime.UtcNow - startTime).TotalSeconds >= timeoutSeconds)
            {
                _logger.LogWarning("Task {TaskId} timed out after {Timeout}s", taskId, timeoutSeconds);
                throw new TimeoutException($"Index build task '{taskId}' timed out after {timeoutSeconds} seconds");
            }

            _logger.LogDebug("Task {TaskId} still running, waiting {Interval}s before next check", 
                taskId, pollIntervalSeconds);

            await Task.Delay(TimeSpan.FromSeconds(pollIntervalSeconds), cancellationToken);
        }

        throw new OperationCanceledException("Index status polling was cancelled");
    }
}

// DTOs
public class IndexBuildRequest
{
    [JsonPropertyName("root_dir")]
    public string? RootDir { get; set; }

    [JsonPropertyName("config")]
    public Dictionary<string, object>? Config { get; set; }

    [JsonPropertyName("resume")]
    public bool Resume { get; set; }

    [JsonPropertyName("nocache")]
    public bool NoCache { get; set; }

    [JsonPropertyName("is_update_run")]
    public bool IsUpdateRun { get; set; }
}

public class IndexBuildResponse
{
    [JsonPropertyName("task_id")]
    public string TaskId { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("root_dir")]
    public string? RootDir { get; set; }
}

public class IndexStatusResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("root_dir")]
    public string? RootDir { get; set; }

    [JsonPropertyName("started_at")]
    public string? StartedAt { get; set; }

    [JsonPropertyName("result")]
    public Dictionary<string, object>? Result { get; set; }
}

public class GraphRagClientException : Exception
{
    public GraphRagClientException(string message) : base(message)
    {
    }

    public GraphRagClientException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }
}

