namespace FableCraft.Application.AdventureGeneration;

internal class AdventureCreationConfig
{
    public string LlmModel { get; init; } = string.Empty;

    public int MaxTokens { get; init; }

    public double Temperature { get; init; }

    public double TopP { get; init; }

    public double FrequencyPenalty { get; init; }

    public double PresencePenalty { get; init; }

    private readonly Dictionary<string, LorebookConfig> _lorebooks = new();

    public Dictionary<string, LorebookConfig> Lorebooks
    {
        get => _lorebooks;
        init
        {
            _lorebooks = new Dictionary<string, LorebookConfig>();
            int priority = 1;
            foreach (var (key, lorebookConfig) in value)
            {
                _lorebooks[key] = lorebookConfig with { Priority = priority };
                priority++;
            }
        }
    }
}

internal record LorebookConfig
{
    public string Description { get; init; } = string.Empty;

    public string PromptPath { get; init; } = string.Empty;

    public int Priority { get; init; }
}