using Microsoft.AspNetCore.Mvc;
using FableCraft.Server.Clients;

namespace FableCraft.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GraphRagController : ControllerBase
{
    private readonly GraphRagClient _graphRagClient;
    private readonly ILogger<GraphRagController> _logger;

    public GraphRagController(GraphRagClient graphRagClient, ILogger<GraphRagController> logger)
    {
        _graphRagClient = graphRagClient;
        _logger = logger;
    }

    /// <summary>
    /// Start building a GraphRAG index
    /// </summary>
    /// <param name="request">Index build configuration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task information including task ID for status tracking</returns>
    [HttpPost("index")]
    [ProducesResponseType(typeof(IndexBuildResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IndexBuildResponse>> BuildIndex(
        [FromBody] IndexBuildRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Received request to build index");
            var response = await _graphRagClient.BuildIndexAsync(request, cancellationToken);
            return AcceptedAtAction(nameof(GetIndexStatus), new { taskId = response.TaskId }, response);
        }
        catch (GraphRagClientException ex)
        {
            _logger.LogError(ex, "Failed to build index");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while building index");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "An unexpected error occurred" });
        }
    }

    /// <summary>
    /// Get the status of an index build task
    /// </summary>
    /// <param name="taskId">The task ID returned from the build index endpoint</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Current status of the index build task</returns>
    [HttpGet("index/{taskId}")]
    [ProducesResponseType(typeof(IndexStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IndexStatusResponse>> GetIndexStatus(
        string taskId,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Checking status for task: {TaskId}", taskId);
            var response = await _graphRagClient.GetIndexStatusAsync(taskId, cancellationToken);
            return Ok(response);
        }
        catch (GraphRagClientException ex) when (ex.Message.Contains("not found"))
        {
            _logger.LogWarning("Task not found: {TaskId}", taskId);
            return NotFound(new { error = ex.Message });
        }
        catch (GraphRagClientException ex)
        {
            _logger.LogError(ex, "Failed to get index status for task: {TaskId}", taskId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while getting index status");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "An unexpected error occurred" });
        }
    }

    /// <summary>
    /// Build an index and wait for completion (synchronous operation)
    /// </summary>
    /// <param name="request">Index build configuration</param>
    /// <param name="pollIntervalSeconds">Seconds between status checks (default: 5)</param>
    /// <param name="timeoutSeconds">Maximum seconds to wait (default: 300, 0 for no timeout)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Final status when build completes or fails</returns>
    [HttpPost("index/build-and-wait")]
    [ProducesResponseType(typeof(IndexStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status408RequestTimeout)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IndexStatusResponse>> BuildIndexAndWait(
        [FromBody] IndexBuildRequest request,
        [FromQuery] int pollIntervalSeconds = 5,
        [FromQuery] int timeoutSeconds = 300,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Building index and waiting for completion");
            
            // Start the build
            var buildResponse = await _graphRagClient.BuildIndexAsync(request, cancellationToken);
            
            // Wait for completion
            var finalStatus = await _graphRagClient.WaitForIndexCompletionAsync(
                buildResponse.TaskId,
                pollIntervalSeconds,
                timeoutSeconds,
                cancellationToken);

            if (finalStatus.Status == "failed" || finalStatus.Status == "error")
            {
                _logger.LogWarning("Index build failed for task: {TaskId}", buildResponse.TaskId);
                return StatusCode(StatusCodes.Status500InternalServerError, finalStatus);
            }

            return Ok(finalStatus);
        }
        catch (TimeoutException ex)
        {
            _logger.LogWarning(ex, "Index build timed out");
            return StatusCode(StatusCodes.Status408RequestTimeout, new { error = ex.Message });
        }
        catch (GraphRagClientException ex)
        {
            _logger.LogError(ex, "Failed to build index");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during index build");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "An unexpected error occurred" });
        }
    }
}

