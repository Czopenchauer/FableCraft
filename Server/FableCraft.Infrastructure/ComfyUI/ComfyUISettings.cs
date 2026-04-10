namespace FableCraft.Infrastructure.ComfyUI;

/// <summary>
/// Configuration settings for ComfyUI image generation service.
/// </summary>
public sealed class ComfyUISettings
{
    public const string SectionName = "ComfyUI";

    /// <summary>
    /// Whether ComfyUI image generation is enabled.
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Base URL for the ComfyUI API.
    /// </summary>
    public string BaseUrl { get; set; } = "http://localhost:8188";

    /// <summary>
    /// Maximum time to wait for image generation to complete.
    /// </summary>
    public TimeSpan GenerationTimeout { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Interval between polling for generation status.
    /// </summary>
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Path for storing generated images (relative, used in non-Docker scenarios).
    /// </summary>
    public string ImageStoragePath { get; set; } = "./scene-images";

    /// <summary>
    /// Absolute host path for image storage (for Docker volume mounts).
    /// When set, this takes precedence over ImageStoragePath for file operations.
    /// </summary>
    public string? ImageStorageHostPath { get; set; }

    /// <summary>
    /// Path to the ComfyUI workflow JSON file.
    /// </summary>
    public string WorkflowPath { get; set; } = "./comfyui-workflows/scene-generation.json";

    /// <summary>
    /// Optional: Specify exact node ID for positive prompt injection.
    /// If not set, the client will find CLIPTextEncode nodes automatically.
    /// </summary>
    public string? PositivePromptNodeId { get; set; }

    /// <summary>
    /// Optional: Specify exact node ID for negative prompt injection.
    /// If not set, the client will find nodes with "negative" in the title.
    /// </summary>
    public string? NegativePromptNodeId { get; set; }

    /// <summary>
    /// Gets the effective image storage path for file operations.
    /// Returns the host path if configured, otherwise falls back to relative path.
    /// </summary>
    public string GetEffectiveImageStoragePath() =>
        string.IsNullOrEmpty(ImageStorageHostPath) ? ImageStoragePath : ImageStorageHostPath;
}
