using System.Text.RegularExpressions;

namespace FableCraft.Infrastructure.Llm;

public static class LlmOutputFormatter
{
    public static string RemoveThinkingBlock(this string input)
    {
        return Regex.Replace(input, "<thinking>.*?</thinking>", string.Empty, RegexOptions.Singleline).Trim();
    }
}