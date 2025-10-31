using System.ComponentModel.DataAnnotations;

namespace FableCraft.Infrastructure.Llm;

internal class LlmConfiguration
{
    [Required]
    public string ApiKey { get; init; } = null!;

    [Required]
    public string BaseUrl { get; init; } = null!;

    [Required]
    public string Model { get; init; } = null!;
}