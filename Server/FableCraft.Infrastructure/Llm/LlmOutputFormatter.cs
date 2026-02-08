using System.Text.RegularExpressions;

namespace FableCraft.Infrastructure.Llm;

public static class LlmOutputFormatter
{
    extension(string? input)
    {
        public string RemoveThinkingBlock()
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }

            return Regex.Replace(input, "<thinking>.*?</thinking>", string.Empty, RegexOptions.Singleline).Trim();
        }

        public string ExtractJsonFromMarkdown()
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }

            var match = Regex.Match(input, @"```json\s*(.*?)\s*```", RegexOptions.Singleline);
            return match.Success ? match.Groups[1].Value.Trim() : input.Trim();
        }
    }
}