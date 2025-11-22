using System.Text.RegularExpressions;

namespace FableCraft.Infrastructure.Llm;

public static class LlmOutputFormatter
{
    public static string RemoveThinkingBlock(this string? input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }

        return Regex.Replace(input, "<thinking>.*?</thinking>", string.Empty, RegexOptions.Singleline).Trim();
    }

    public static string ExtractJsonFromMarkdown(this string? input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }

        var match = Regex.Match(input, @"```json\s*(.*?)\s*```", RegexOptions.Singleline);
        return match.Success ? match.Groups[1].Value.Trim() : string.Empty;
    }
}