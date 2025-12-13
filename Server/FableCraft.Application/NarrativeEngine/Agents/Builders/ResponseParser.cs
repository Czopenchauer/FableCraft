using System.Text.Json;
using System.Text.RegularExpressions;

using FableCraft.Infrastructure.Llm;

namespace FableCraft.Application.NarrativeEngine.Agents.Builders;

/// <summary>
///     Parses LLM responses extracting content from XML tags and deserializing JSON
/// </summary>
internal static class ResponseParser
{
    /// <summary>
    ///     Extracts content from a single XML tag and deserializes it as JSON
    /// </summary>
    public static T ExtractJson<T>(string response, string tag, bool ignoreNull = false)
    {
        var content = ExtractTagContent(response, tag);
        JsonSerializerOptions options = PromptSections.GetJsonOptions(ignoreNull);
        if (content == null)
        {
            var santized = response.RemoveThinkingBlock().ExtractJsonFromMarkdown();
            return JsonSerializer.Deserialize<T>(santized, options)
                   ?? throw new InvalidOperationException($"Failed to extract JSON from response: <{tag}> tag not found and deserialization returned null. Place the json in correct tag.");
        }

        return JsonSerializer.Deserialize<T>(content.RemoveThinkingBlock().ExtractJsonFromMarkdown(), options)
               ?? throw new InvalidOperationException($"Deserialization of {typeof(T).Name} returned null.");
    }

    /// <summary>
    ///     Extracts raw text content from an XML tag
    /// </summary>
    public static string ExtractText(string response, string tag)
    {
        var content = ExtractTagContent(response, tag);
        if (content == null)
        {
            throw new InvalidOperationException($"Failed to extract text from response: <{tag}> tag not found.");
        }

        return content.RemoveThinkingBlock().Trim();
    }

    /// <summary>
    ///     Creates a parser function for use with SendRequestAsync that extracts a single JSON object
    /// </summary>
    public static Func<string, T> CreateJsonParser<T>(string tag, bool ignoreNull = false)
    {
        return response => ExtractJson<T>(response, tag, ignoreNull);
    }

    /// <summary>
    ///     Creates a parser function that extracts JSON and text from different tags
    /// </summary>
    public static Func<string, (T, string)> CreateJsonTextParser<T>(
        string jsonTag, string textTag,
        bool ignoreNull = false)
    {
        return response => (
            ExtractJson<T>(response, jsonTag, ignoreNull),
            ExtractText(response, textTag)
        );
    }

    /// <summary>
    ///     Creates a parser function that deserializes raw JSON response (no XML tag extraction)
    /// </summary>
    public static Func<string, T> CreateRawJsonParser<T>(bool ignoreNull = false)
    {
        return response =>
        {
            JsonSerializerOptions options = PromptSections.GetJsonOptions(ignoreNull);
            return JsonSerializer.Deserialize<T>(response.RemoveThinkingBlock().ExtractJsonFromMarkdown(), options)
                   ?? throw new InvalidOperationException($"Deserialization of {typeof(T).Name} returned null.");
        };
    }

    private static string? ExtractTagContent(string response, string tag)
    {
        Match match = Regex.Match(response, $"<{tag}>(.*?)</{tag}>", RegexOptions.Singleline);
        return match.Success ? match.Groups[1].Value.RemoveThinkingBlock().ExtractJsonFromMarkdown() : null;
    }
}