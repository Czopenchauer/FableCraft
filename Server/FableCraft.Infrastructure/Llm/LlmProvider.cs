namespace FableCraft.Infrastructure.Llm;

public readonly struct LlmProvider : IEquatable<LlmProvider>
{
    public static readonly LlmProvider OpenAi = new("openai");
    public static readonly LlmProvider Gemini = new("gemini");
    public static readonly LlmProvider NanoGpt = new("nanogpt");
    public static readonly LlmProvider Ollama = new("ollama");

    public string Value { get; }

    private LlmProvider(string value)
    {
        Value = value;
    }

    public static LlmProvider FromString(string value)
    {
        return value.ToLowerInvariant() switch
        {
            "openai" => OpenAi,
            "gemini" => Gemini,
            "nanogpt" => NanoGpt,
            "ollama" => Ollama,
            _ => OpenAi
        };
    }

    public override string ToString() => Value;

    public override int GetHashCode() => Value.GetHashCode();

    public override bool Equals(object? obj) => obj is LlmProvider other && Equals(other);

    public bool Equals(LlmProvider other) => Value == other.Value;

    public static bool operator ==(LlmProvider left, LlmProvider right) => left.Equals(right);

    public static bool operator !=(LlmProvider left, LlmProvider right) => !left.Equals(right);

    public static implicit operator string(LlmProvider provider) => provider.Value;
}
