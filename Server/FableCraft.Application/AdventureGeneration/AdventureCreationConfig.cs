namespace FableCraft.Application.AdventureGeneration;

internal class AdventureCreationConfig
{
    public string LlmModel { get; init; } = string.Empty;

    public int MaxTokens { get; init; }

    public double Temperature { get; init; }

    public double TopP { get; init; }

    public double FrequencyPenalty { get; init; }

    public double PresencePenalty { get; init; }

    public Dictionary<string, LorebookConfig> Lorebooks { get; init; } = new();
}

internal record LorebookConfig
{
    public string Description { get; init; } = string.Empty;

    public string PromptPath { get; init; } = string.Empty;

    public int Priority { get; init; }

    public string GetPromptFileName()
    {
        return Path.Combine(AppContext.BaseDirectory, "Prompts", "adventure_generation", PromptPath);
    }
}