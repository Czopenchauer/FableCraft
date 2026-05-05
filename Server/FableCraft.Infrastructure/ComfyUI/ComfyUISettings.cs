namespace FableCraft.Infrastructure.ComfyUI;

/// <summary>
/// Configuration settings for the ComfyUI image generation backend.
/// </summary>
public sealed class ComfyUISettings
{
    public string BaseUrl { get; set; } = "http://localhost:8188";

    public TimeSpan GenerationTimeout { get; set; } = TimeSpan.FromMinutes(5);

    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(2);

    public string WorkflowPath { get; set; } = "./comfyui-workflows/scene-generation.json";

    /// <summary>
    /// Optional: exact node ID for positive prompt injection. If unset, auto-detect.
    /// </summary>
    public string? PositivePromptNodeId { get; set; }

    /// <summary>
    /// Optional: exact node ID for negative prompt injection. If unset, auto-detect.
    /// </summary>
    public string? NegativePromptNodeId { get; set; }
}
