using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FableCraft.Infrastructure.ComfyUI;

/// <summary>
/// Client for interacting with the ComfyUI API.
/// Handles workflow submission, status polling, and image retrieval.
/// </summary>
public sealed class ComfyUIClient
{
    private readonly HttpClient _httpClient;
    private readonly ComfyUISettings _settings;
    private readonly ILogger<ComfyUIClient> _logger;

    public ComfyUIClient(
        HttpClient httpClient,
        IOptions<ComfyUISettings> settings,
        ILogger<ComfyUIClient> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    /// <summary>
    /// Generates an image using the configured workflow with the provided prompts.
    /// </summary>
    /// <param name="positivePrompt">The positive prompt describing what to generate.</param>
    /// <param name="negativePrompt">The negative prompt describing what to avoid.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The generated image bytes and generation duration.</returns>
    public async Task<ComfyUIGenerationResult> GenerateImageAsync(
        string positivePrompt,
        string? negativePrompt,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        // Load and modify workflow
        var workflow = await LoadWorkflowAsync(cancellationToken);
        InjectPrompts(workflow, positivePrompt, negativePrompt);

        // Submit to ComfyUI
        var promptId = await SubmitWorkflowAsync(workflow, cancellationToken);
        _logger.LogInformation("Submitted workflow to ComfyUI with prompt_id: {PromptId}", promptId);

        // Poll for completion
        var outputInfo = await WaitForCompletionAsync(promptId, cancellationToken);

        // Retrieve the generated image
        var imageBytes = await RetrieveImageAsync(outputInfo, cancellationToken);

        stopwatch.Stop();

        return new ComfyUIGenerationResult
        {
            ImageBytes = imageBytes,
            GenerationDurationMs = stopwatch.ElapsedMilliseconds
        };
    }

    /// <summary>
    /// Checks if ComfyUI is available and responding.
    /// </summary>
    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"{_settings.BaseUrl}/system_stats",
                cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ComfyUI health check failed");
            return false;
        }
    }

    private async Task<JsonObject> LoadWorkflowAsync(CancellationToken cancellationToken)
    {
        var workflowPath = _settings.WorkflowPath;
        if (!Path.IsPathRooted(workflowPath))
        {
            workflowPath = Path.GetFullPath(workflowPath);
        }

        if (!File.Exists(workflowPath))
        {
            throw new FileNotFoundException($"ComfyUI workflow file not found: {workflowPath}");
        }

        var json = await File.ReadAllTextAsync(workflowPath, cancellationToken);
        return JsonNode.Parse(json)?.AsObject()
            ?? throw new InvalidOperationException("Failed to parse workflow JSON");
    }

    private void InjectPrompts(JsonObject workflow, string positivePrompt, string? negativePrompt)
    {
        var positiveNodeId = _settings.PositivePromptNodeId;
        var negativeNodeId = _settings.NegativePromptNodeId;

        // If node IDs not configured, find them automatically
        if (string.IsNullOrEmpty(positiveNodeId) || string.IsNullOrEmpty(negativeNodeId))
        {
            FindPromptNodes(workflow, out var foundPositive, out var foundNegative);
            positiveNodeId ??= foundPositive;
            negativeNodeId ??= foundNegative;
        }

        if (string.IsNullOrEmpty(positiveNodeId))
        {
            throw new InvalidOperationException("Could not find positive prompt node in workflow");
        }

        // Inject positive prompt
        if (workflow[positiveNodeId] is JsonObject positiveNode &&
            positiveNode["inputs"] is JsonObject positiveInputs)
        {
            positiveInputs["text"] = positivePrompt;
            _logger.LogDebug("Injected positive prompt into node {NodeId}", positiveNodeId);
        }
        else
        {
            throw new InvalidOperationException($"Invalid positive prompt node structure at {positiveNodeId}");
        }

        // Inject negative prompt if we have one and found a node for it
        if (!string.IsNullOrEmpty(negativePrompt) && !string.IsNullOrEmpty(negativeNodeId))
        {
            if (workflow[negativeNodeId] is JsonObject negativeNode &&
                negativeNode["inputs"] is JsonObject negativeInputs)
            {
                negativeInputs["text"] = negativePrompt;
                _logger.LogDebug("Injected negative prompt into node {NodeId}", negativeNodeId);
            }
        }
    }

    private static void FindPromptNodes(JsonObject workflow, out string? positiveNodeId, out string? negativeNodeId)
    {
        positiveNodeId = null;
        negativeNodeId = null;
        string? firstClipNode = null;

        foreach (var (nodeId, nodeValue) in workflow)
        {
            if (nodeValue is not JsonObject node) continue;

            var classType = node["class_type"]?.GetValue<string>();
            if (classType != "CLIPTextEncode") continue;

            // Check if this is a negative prompt node (by title or meta)
            var meta = node["_meta"]?.AsObject();
            var title = meta?["title"]?.GetValue<string>()?.ToLowerInvariant() ?? "";

            if (title.Contains("negative"))
            {
                negativeNodeId = nodeId;
            }
            else if (title.Contains("positive"))
            {
                positiveNodeId = nodeId;
            }
            else
            {
                // Track first CLIP node as fallback for positive
                firstClipNode ??= nodeId;
            }
        }

        // Fallback: use first CLIP node as positive if not found
        positiveNodeId ??= firstClipNode;
    }

    private async Task<string> SubmitWorkflowAsync(JsonObject workflow, CancellationToken cancellationToken)
    {
        var payload = new JsonObject
        {
            ["prompt"] = workflow
        };

        var response = await _httpClient.PostAsJsonAsync(
            $"{_settings.BaseUrl}/prompt",
            payload,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<JsonObject>(cancellationToken);
        return result?["prompt_id"]?.GetValue<string>()
            ?? throw new InvalidOperationException("No prompt_id returned from ComfyUI");
    }

    private async Task<ComfyUIOutputInfo> WaitForCompletionAsync(string promptId, CancellationToken cancellationToken)
    {
        using var timeoutCts = new CancellationTokenSource(_settings.GenerationTimeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        while (!linkedCts.Token.IsCancellationRequested)
        {
            var response = await _httpClient.GetAsync(
                $"{_settings.BaseUrl}/history/{promptId}",
                linkedCts.Token);

            if (!response.IsSuccessStatusCode)
            {
                await Task.Delay(_settings.PollInterval, linkedCts.Token);
                continue;
            }

            var history = await response.Content.ReadFromJsonAsync<JsonObject>(linkedCts.Token);
            if (history == null || !history.ContainsKey(promptId))
            {
                await Task.Delay(_settings.PollInterval, linkedCts.Token);
                continue;
            }

            var promptHistory = history[promptId]?.AsObject();
            if (promptHistory == null)
            {
                await Task.Delay(_settings.PollInterval, linkedCts.Token);
                continue;
            }

            // Check for errors
            if (promptHistory["status"]?.AsObject()?["status_str"]?.GetValue<string>() == "error")
            {
                var errorMessage = promptHistory["status"]?["messages"]?.ToString() ?? "Unknown error";
                throw new ComfyUIGenerationException($"ComfyUI generation failed: {errorMessage}");
            }

            // Check for outputs
            var outputs = promptHistory["outputs"]?.AsObject();
            if (outputs == null || outputs.Count == 0)
            {
                await Task.Delay(_settings.PollInterval, linkedCts.Token);
                continue;
            }

            // Find the output image
            foreach (var (_, nodeOutput) in outputs)
            {
                var images = nodeOutput?.AsObject()?["images"]?.AsArray();
                if (images == null || images.Count == 0) continue;

                var imageInfo = images[0]?.AsObject();
                if (imageInfo == null) continue;

                return new ComfyUIOutputInfo
                {
                    Filename = imageInfo["filename"]?.GetValue<string>() ?? "",
                    Subfolder = imageInfo["subfolder"]?.GetValue<string>() ?? "",
                    Type = imageInfo["type"]?.GetValue<string>() ?? "output"
                };
            }

            await Task.Delay(_settings.PollInterval, linkedCts.Token);
        }

        throw new TimeoutException($"Image generation timed out after {_settings.GenerationTimeout}");
    }

    private async Task<byte[]> RetrieveImageAsync(ComfyUIOutputInfo outputInfo, CancellationToken cancellationToken)
    {
        var url = $"{_settings.BaseUrl}/view?filename={Uri.EscapeDataString(outputInfo.Filename)}" +
                  $"&type={Uri.EscapeDataString(outputInfo.Type)}";

        if (!string.IsNullOrEmpty(outputInfo.Subfolder))
        {
            url += $"&subfolder={Uri.EscapeDataString(outputInfo.Subfolder)}";
        }

        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }

    private sealed class ComfyUIOutputInfo
    {
        public required string Filename { get; init; }
        public string Subfolder { get; init; } = "";
        public string Type { get; init; } = "output";
    }
}

/// <summary>
/// Result of a ComfyUI image generation.
/// </summary>
public sealed class ComfyUIGenerationResult
{
    public required byte[] ImageBytes { get; init; }
    public long GenerationDurationMs { get; init; }
}

/// <summary>
/// Exception thrown when ComfyUI image generation fails.
/// </summary>
public sealed class ComfyUIGenerationException : Exception
{
    public ComfyUIGenerationException(string message) : base(message) { }
    public ComfyUIGenerationException(string message, Exception innerException) : base(message, innerException) { }
}
