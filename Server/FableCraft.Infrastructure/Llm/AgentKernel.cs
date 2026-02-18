using System.ClientModel;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;

using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Queue;

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Google;
using Microsoft.SemanticKernel.Services;

using Polly;
using Polly.Retry;

using Serilog;
using Serilog.Context;
using Serilog.Core.Enrichers;

namespace FableCraft.Infrastructure.Llm;

public class LlmEmptyResponseException() : Exception("The LLM returned an empty response.");

public class AgentKernelOptions
{
    public int MaxParsingRetries { get; init; } = 4;
}

internal record TokenUsage(int? InputTokens, int? OutputTokens, int? TotalTokens, int? CachedTokens);

internal static class Telemetry
{
    public static readonly ActivitySource LlmActivitySource = new("LlmCall");
}

public interface IAgentKernel
{
    Task<T> SendRequestAsync<T>(
        ChatHistory chatHistory,
        Func<string, T> outputFunc,
        PromptExecutionSettings promptExecutionSettings,
        string operationName,
        Kernel kernel,
        CancellationToken cancellationToken,
        AgentKernelOptions? options = null);
}

internal sealed class AgentKernel : IAgentKernel
{
    private readonly ILogger _logger;
    private readonly IMessageDispatcher _messageDispatcher;
    private readonly ResiliencePipeline _pipeline;

    public AgentKernel(ILogger logger, IMessageDispatcher messageDispatcher)
    {
        _logger = logger;
        _messageDispatcher = messageDispatcher;
        _pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder()
                    .Handle<HttpOperationException>(e => e.StatusCode == HttpStatusCode.TooManyRequests)
                    .Handle<HttpOperationException>(e => e.StatusCode == HttpStatusCode.InternalServerError)
                    .Handle<HttpRequestException>(e => e.StatusCode == HttpStatusCode.InternalServerError)
                    .Handle<HttpRequestException>(e => e.StatusCode == HttpStatusCode.TooManyRequests),
                MaxRetryAttempts = 4,
                Delay = TimeSpan.FromSeconds(5),
                OnRetry = args =>
                {
                    _logger.Warning("Attempt {attempt}: Retrying generation due to error: {error}",
                        args.AttemptNumber,
                        args.Outcome.Exception?.Message);
                    return default;
                },
                BackoffType = DelayBackoffType.Constant,
                UseJitter = true
            })
            .Build();
    }

    public async Task<T> SendRequestAsync<T>(
        ChatHistory chatHistory,
        Func<string, T> outputFunc,
        PromptExecutionSettings promptExecutionSettings,
        string operationName,
        Kernel kernel,
        CancellationToken cancellationToken,
        AgentKernelOptions? options = null)
    {
        var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>()
                                    ?? throw new InvalidOperationException("ChatCompletionService not found in kernel.");
        var maxParsingRetries = options?.MaxParsingRetries ?? 4;
        for (int attempt = 1; attempt <= maxParsingRetries; attempt++)
        {
            try
            {
                return await GetResponse();
            }
            catch (InvalidCastException ex)
            {
                _logger.Warning(ex,
                    "Error while calling LLM {operation} service (attempt {attempt}/{max}). {message}",
                    operationName,
                    attempt,
                    maxParsingRetries,
                    ex.Message);
                if (attempt == maxParsingRetries)
                {
                    throw;
                }

                chatHistory.AddUserMessage(
                    $"I've encountered an error parsing your response. Check your output format and ensure the response follows it precisely. Fix your response. Error: {ex.Message}");
            }
            catch (JsonException ex)
            {
                _logger.Warning(ex,
                    "Error while calling LLM {operation} service (attempt {attempt}/{max}). {message}",
                    operationName,
                    attempt,
                    maxParsingRetries,
                    ex.Message);
                if (attempt == maxParsingRetries)
                {
                    throw;
                }

                chatHistory.AddUserMessage(
                    $"I've encountered an error parsing your response. Check your output format and ensure the response follows it precisely. Fix your response. Error: {ex.Message}");
            }
            catch (InvalidOperationException ex)
            {
                _logger.Warning(ex,
                    "Error while calling LLM {operation} service (attempt {attempt}/{max}). {message}",
                    operationName,
                    attempt,
                    maxParsingRetries,
                    ex.Message);
                if (attempt == maxParsingRetries)
                {
                    throw;
                }

                chatHistory.AddUserMessage(
                    $"I've encountered an error parsing your response. Check your output format and ensure the response follows it precisely. Fix your response. Error: {ex.Message}");
            }
            catch (LlmEmptyResponseException ex)
            {
                _logger.Warning(ex,
                    "Error while calling LLM {operation} service (attempt {attempt}/{max}). {message}",
                    operationName,
                    attempt,
                    maxParsingRetries,
                    ex.Message);
                if (attempt == maxParsingRetries)
                {
                    throw;
                }

                chatHistory.AddUserMessage("You've returned nothing. Continue reasoning process and output correctly formatted response.");
            }
        }

        throw new InvalidOperationException("Unreachable code - retry loop should have returned or thrown");

        async Task<T> GetResponse()
        {
            var result = await _pipeline.ExecuteAsync(async token =>
                {
                    try
                    {
                        using (LogContext.Push(
                                   new PropertyEnricher("OperationName", operationName),
                                   new PropertyEnricher("Model", chatCompletionService.GetModelId()),
                                   new PropertyEnricher("AdventureId", ProcessExecutionContext.AdventureId.Value)
                               ))
                        {
                            using var llmActivity = Telemetry.LlmActivitySource.StartActivity("LlmCall");
                            llmActivity?.SetTag("llm.operation", operationName);
                            llmActivity?.SetTag("llm.model", chatCompletionService.GetModelId());

                            var stopwatch = Stopwatch.StartNew();

                            var (responseContent, context, usage) = await ExecuteWithResumeAsync(
                                chatCompletionService,
                                chatHistory,
                                promptExecutionSettings,
                                kernel,
                                token);

                            _logger.Information("Generated streaming response: {response}", responseContent);
                            _logger.Information(
                                "Token usage - Input: {Input}, Output: {Output}, Total: {Total}, CachedTokens: {Cached}",
                                usage?.InputTokens,
                                usage?.OutputTokens,
                                usage?.TotalTokens,
                                usage?.CachedTokens);

                            var requestContent = string.Join(",", chatHistory.Select(m => m.Content));
                            await _messageDispatcher.PublishAsync(new ResponseReceivedEvent
                                {
                                    AdventureId = ProcessExecutionContext.AdventureId.Value ?? Guid.Empty,
                                    SceneId = ProcessExecutionContext.SceneId.Value,
                                    CallerName = operationName,
                                    RequestContent = requestContent,
                                    ResponseContent = responseContent,
                                    InputToken = usage?.InputTokens,
                                    OutputToken = usage?.OutputTokens,
                                    TotalToken = usage?.TotalTokens,
                                    CachedToken = usage?.CachedTokens,
                                    Duration = stopwatch.ElapsedMilliseconds
                                },
                                token);

                            return responseContent;
                        }
                    }
                    catch (HttpOperationException ex) when (ex.StatusCode == HttpStatusCode.BadRequest)
                    {
                        _logger.Error(ex, "Error occurred while calling LLM service. {message}", ex.ResponseContent);
                        throw;
                    }
                    catch (ClientResultException e)
                    {
                        _logger.Error(e, "Error occurred while calling LLM service. {message}", e.Data.ToJsonString());
                        throw;
                    }
                },
                cancellationToken);
            if (string.IsNullOrEmpty(result))
            {
                throw new LlmEmptyResponseException();
            }

            chatHistory.AddAssistantMessage(result);
            return outputFunc(result);
        }
    }

    private async Task<(string Content, StreamingContext Context, TokenUsage? Usage)> ExecuteWithResumeAsync(
        IChatCompletionService chatCompletionService,
        ChatHistory originalHistory,
        PromptExecutionSettings settings,
        Kernel kernel,
        CancellationToken cancellationToken)
    {
        var context = new StreamingContext();
        const int maxRetries = 3;

        for (int attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                var history = CloneChatHistory(originalHistory);

                if (!string.IsNullOrEmpty(context.PartialResponse))
                {
                    history.AddAssistantMessage(context.PartialResponse);
                    history.AddUserMessage(
                        "Your previous response was interrupted. " + "Continue EXACTLY from where you stopped. " + "Do not repeat content already provided.");
                }

                var (content, usage) = await StreamResponseAsync(
                    chatCompletionService,
                    history,
                    settings,
                    kernel,
                    context,
                    cancellationToken);

                return (content, context, usage);
            }
            catch (HttpIOException ex) when (ex.Message.Contains("ResponseEnded") || ex.Message.Contains("prematurely"))
            {
                context.RetryCount = attempt + 1;
                _logger.Warning(
                    "Streaming interrupted at {Chunks} chunks, {Length} chars. Retry {Attempt}/{Max}",
                    context.ChunksReceived,
                    context.PartialResponse.Length,
                    attempt + 1,
                    maxRetries);

                if (attempt == maxRetries)
                {
                    throw new LlmStreamingFailedException(context, ex);
                }

                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            }
            catch (HttpRequestException ex) when (ex.Message.Contains("ResponseEnded") || ex.Message.Contains("prematurely"))
            {
                context.RetryCount = attempt + 1;
                _logger.Warning(
                    "Streaming interrupted (HttpRequestException) at {Chunks} chunks, {Length} chars. Retry {Attempt}/{Max}",
                    context.ChunksReceived,
                    context.PartialResponse.Length,
                    attempt + 1,
                    maxRetries);

                if (attempt == maxRetries)
                {
                    throw new LlmStreamingFailedException(context, ex);
                }
            }
        }

        throw new InvalidOperationException("Unreachable code - retry loop should have returned or thrown");
    }

    private async Task<(string Content, TokenUsage? Usage)> StreamResponseAsync(
        IChatCompletionService chatCompletionService,
        ChatHistory chatHistory,
        PromptExecutionSettings settings,
        Kernel kernel,
        StreamingContext context,
        CancellationToken cancellationToken)
    {
        var responseBuilder = new StringBuilder(context.PartialResponse);
        var reasoningBuilder = new StringBuilder();
        var itemTypes = new HashSet<string>();
        StreamingChatMessageContent? lastChunk = null;

        await foreach (var chunk in chatCompletionService.GetStreamingChatMessageContentsAsync(
                           chatHistory,
                           settings,
                           kernel,
                           cancellationToken))
        {
            foreach (var item in chunk.Items)
            {
                if (item.GetType().Name.Contains("Reasoning", StringComparison.OrdinalIgnoreCase))
                {
                    if (item is StreamingTextContent textContent)
                    {
                        reasoningBuilder.Append(textContent.Text);
                    }
                }
            }

            if (chunk.Content != null)
            {
                responseBuilder.Append(chunk.Content);
                context.PartialResponse = responseBuilder.ToString();
                context.ChunksReceived++;
            }

            lastChunk = chunk;
        }

        context.IsComplete = true;

        if (itemTypes.Count > 0)
        {
            _logger.Information("Content item types received: {Types}", string.Join(", ", itemTypes));
        }

        if (reasoningBuilder.Length > 0)
        {
            _logger.Information("Reasoning content ({Length} chars): {Reasoning}",
                reasoningBuilder.Length,
                reasoningBuilder.ToString());
        }

        TokenUsage? usage = null;
        if (lastChunk?.Metadata != null)
        {
            _logger.Information("Chunk inner content: {ChunkMetadata}", lastChunk.InnerContent.ToJsonString());
            _logger.Information("Chunk metadata: {ChunkMetadata}", lastChunk.Metadata.ToJsonString());
            if (lastChunk.Metadata.TryGetValue("Usage", out var usageObj) && usageObj != null)
            {
                usage = ExtractUsageFromObject(usageObj);
            }

            if (usage?.CachedTokens == null && lastChunk.Metadata.TryGetValue("CachedContentTokenCount", out var cachedObj))
            {
                var cachedTokens = cachedObj as int? ?? (cachedObj is long l ? (int?)l : null);
                if (cachedTokens != null)
                {
                    usage = usage != null
                        ? usage with { CachedTokens = cachedTokens }
                        : new TokenUsage(null, null, null, cachedTokens);
                }
            }
        }

        return (responseBuilder.ToString(), usage);
    }

    private TokenUsage? ExtractUsageFromObject(object usageObj)
    {
        try
        {
            var type = usageObj.GetType();
            var inputTokens = type.GetProperty("InputTokenCount")?.GetValue(usageObj) as int?
                              ?? type.GetProperty("PromptTokens")?.GetValue(usageObj) as int?
                              ?? type.GetProperty("PromptTokenCount")?.GetValue(usageObj) as int?;

            var outputTokens = type.GetProperty("OutputTokenCount")?.GetValue(usageObj) as int?
                               ?? type.GetProperty("CompletionTokens")?.GetValue(usageObj) as int?
                               ?? type.GetProperty("CandidatesTokenCount")?.GetValue(usageObj) as int?;

            var totalTokens = type.GetProperty("TotalTokenCount")?.GetValue(usageObj) as int?
                              ?? type.GetProperty("TotalTokens")?.GetValue(usageObj) as int?;

            int? cachedTokens = null;
            var inputDetails = type.GetProperty("InputTokenDetails")?.GetValue(usageObj);
            if (inputDetails != null)
            {
                cachedTokens = inputDetails.GetType().GetProperty("CachedTokenCount")?.GetValue(inputDetails) as int?;
            }
            cachedTokens ??= type.GetProperty("CachedContentTokenCount")?.GetValue(usageObj) as int?;

            return new TokenUsage(inputTokens, outputTokens, totalTokens, cachedTokens);
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to extract usage from object of type {Type}", usageObj.GetType().Name);
            return null;
        }
    }

    private static ChatHistory CloneChatHistory(ChatHistory original)
    {
        var clone = new ChatHistory();
        foreach (var message in original)
        {
            clone.Add(message);
        }

        return clone;
    }
}