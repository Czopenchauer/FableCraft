using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.SemanticKernel.ChatCompletion;

namespace FableCraft.Application.NarrativeEngine.Agents;

/// <summary>
/// Fluent builder for constructing ChatHistory with consistent formatting
/// </summary>
internal sealed class ChatHistoryBuilder
{
    private readonly ChatHistory _chatHistory = new();

    private readonly static JsonSerializerOptions DefaultJsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true
    };

    private readonly static JsonSerializerOptions JsonOptionsIgnoreNull = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static JsonSerializerOptions GetJsonOptions(bool ignoreNull = false)
        => ignoreNull ? JsonOptionsIgnoreNull : DefaultJsonOptions;

    public ChatHistoryBuilder WithSystemMessage(string message)
    {
        _chatHistory.AddSystemMessage(message);
        return this;
    }

    public ChatHistoryBuilder WithUserMessage(string message)
    {
        _chatHistory.AddUserMessage(message);
        return this;
    }

    /// <summary>
    /// Adds a user message with content wrapped in an XML tag
    /// </summary>
    public ChatHistoryBuilder WithTaggedContent(string tag, string content)
    {
        _chatHistory.AddUserMessage($"""
                                     <{tag}>
                                     {content}
                                     </{tag}>
                                     """);
        return this;
    }

    /// <summary>
    /// Adds a user message with serialized JSON content wrapped in an XML tag
    /// </summary>
    public ChatHistoryBuilder WithTaggedJson<T>(string tag, T content, bool ignoreNull = false)
    {
        var options = ignoreNull ? JsonOptionsIgnoreNull : DefaultJsonOptions;
        _chatHistory.AddUserMessage($"""
                                     <{tag}>
                                     {JsonSerializer.Serialize(content, options)}
                                     </{tag}>
                                     """);
        return this;
    }

    /// <summary>
    /// Adds a user message with serialized JSON content wrapped in an XML tag, only if content is not null
    /// </summary>
    public ChatHistoryBuilder WithTaggedJsonIfNotNull<T>(string tag, T? content, bool ignoreNull = false) where T : class
    {
        if (content != null)
        {
            return WithTaggedJson(tag, content, ignoreNull);
        }
        return this;
    }

    public ChatHistory Build() => _chatHistory;

    /// <summary>
    /// Creates a new ChatHistoryBuilder instance
    /// </summary>
    public static ChatHistoryBuilder Create() => new();
}
