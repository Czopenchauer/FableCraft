namespace FableCraft.Infrastructure.SwarmUI;

/// <summary>
/// Configuration settings for the SwarmUI image generation backend.
/// </summary>
public sealed class SwarmUISettings
{
    public string BaseUrl { get; set; } = "http://localhost:7801";

    /// <summary>
    /// Required. SwarmUI model identifier (e.g. "OfficialStableDiffusion/sd_xl_base_1.0").
    /// </summary>
    public string Model { get; set; } = "";

    public int Width { get; set; } = 1024;

    public int Height { get; set; } = 1024;

    public int Steps { get; set; } = 20;

    public double CfgScale { get; set; } = 7.0;

    public string Sampler { get; set; } = "euler";

    /// <summary>
    /// Use -1 for a random seed per request.
    /// </summary>
    public long Seed { get; set; } = -1;

    public TimeSpan GenerationTimeout { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Appended to the LLM-generated positive prompt (e.g. quality boosters).
    /// </summary>
    public string PositiveAppendix { get; set; } = "";

    /// <summary>
    /// Appended to the LLM-generated negative prompt (e.g. common artifact filters).
    /// </summary>
    public string NegativeAppendix { get; set; } = "";

    /// <summary>
    /// LoRA names to apply (matches SwarmUI's "loras" parameter). Discover names via
    /// POST /API/ListModels with subtype: "LoRA".
    /// </summary>
    public List<string> Loras { get; set; } = new();

    /// <summary>
    /// Weights parallel to <see cref="Loras"/>. Must have the same count, or be empty
    /// (in which case SwarmUI's default weight of 1.0 is used).
    /// </summary>
    public List<double> LoraWeights { get; set; } = new();

    /// <summary>
    /// Sub-prompt applied to detected face regions via SwarmUI's segment system
    /// (&lt;segment:face&gt;...&lt;/segment&gt;). Empty disables face restoration.
    /// Requires the face YOLO model to be installed in SwarmUI.
    /// </summary>
    public string FaceRestorationPrompt { get; set; } = "";

    /// <summary>
    /// Sub-prompt applied to detected hand regions (&lt;segment:hand&gt;...&lt;/segment&gt;).
    /// Empty disables hand restoration. Requires the hand YOLO model.
    /// </summary>
    public string HandRestorationPrompt { get; set; } = "";
}
