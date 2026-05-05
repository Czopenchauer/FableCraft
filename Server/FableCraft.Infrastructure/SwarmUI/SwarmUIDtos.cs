using System.Text.Json.Serialization;

namespace FableCraft.Infrastructure.SwarmUI;

internal sealed class NewSessionResponse
{
    [JsonPropertyName("session_id")]
    public string? SessionId { get; set; }
}

internal sealed class GenerateText2ImageRequest
{
    [JsonPropertyName("session_id")]
    public required string SessionId { get; set; }

    [JsonPropertyName("images")]
    public int Images { get; set; } = 1;

    [JsonPropertyName("prompt")]
    public required string Prompt { get; set; }

    [JsonPropertyName("negativeprompt")]
    public string? NegativePrompt { get; set; }

    [JsonPropertyName("model")]
    public required string Model { get; set; }

    [JsonPropertyName("width")]
    public int Width { get; set; }

    [JsonPropertyName("height")]
    public int Height { get; set; }

    [JsonPropertyName("steps")]
    public int Steps { get; set; }

    [JsonPropertyName("cfgscale")]
    public double CfgScale { get; set; }

    [JsonPropertyName("seed")]
    public long Seed { get; set; }

    [JsonPropertyName("sampler")]
    public string? Sampler { get; set; }

    [JsonPropertyName("loras")]
    public List<string>? Loras { get; set; }

    [JsonPropertyName("loraweights")]
    public List<double>? LoraWeights { get; set; }
}

internal sealed class GenerateText2ImageResponse
{
    [JsonPropertyName("images")]
    public List<string>? Images { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("error_id")]
    public string? ErrorId { get; set; }
}
