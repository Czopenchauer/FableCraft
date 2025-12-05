using System.Diagnostics;
using System.Net;
using System.Text.Json;

using FableCraft.Infrastructure.Queue;

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

using OpenAI.Chat;

using Polly;
using Polly.Retry;

using Serilog;

using ChatMessageContent = Microsoft.SemanticKernel.ChatMessageContent;

namespace FableCraft.Infrastructure.Llm;

public interface IAgentKernel
{
    Task<T> SendRequestAsync<T>(
        ChatHistory chatHistory,
        Func<string, T> outputFunc,
        PromptExecutionSettings promptExecutionSettings,
        string operationName,
        Kernel kernel,
        CancellationToken cancellationToken);
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
                    .Handle<LlmEmptyResponseException>()
                    .Handle<HttpRequestException>(e => e.StatusCode == HttpStatusCode.TooManyRequests),
                MaxRetryAttempts = 5,
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
        CancellationToken cancellationToken)
    {
        IChatCompletionService chatCompletionService = kernel.GetRequiredService<IChatCompletionService>()
                                                       ?? throw new InvalidOperationException("ChatCompletionService not found in kernel.");
        try
        {
            return await GetResponse();
        }
        catch (InvalidCastException ex)
        {
            chatHistory.AddUserMessage($"I've encountered an error parsing your response. Fix your response. {ex.Message}");
            return await GetResponse();
        }
        catch (JsonException ex)
        {
            chatHistory.AddUserMessage($"I've encountered an error parsing your response. Fix your response. {ex.Message}");
            return await GetResponse();
        }

        async Task<T> GetResponse()
        {
            var result = await _pipeline.ExecuteAsync(async token =>
                             {
                                 try
                                 {
                                     var stopwatch = Stopwatch.StartNew();
                                     ChatMessageContent result =
                                         await chatCompletionService.GetChatMessageContentAsync(chatHistory, promptExecutionSettings, kernel, token);
                                     _logger.Debug("Generated response: {response}", JsonSerializer.Serialize(result));
                                     var replyInnerContent = result.InnerContent as ChatCompletion;
                                     _logger.Information("Input usage: {usage}, output usage {output}, total usage {total}",
                                         replyInnerContent?.Usage.InputTokenCount,
                                         replyInnerContent?.Usage.OutputTokenCount,
                                         replyInnerContent?.Usage.TotalTokenCount);
                                     await _messageDispatcher.PublishAsync(new ResponseReceivedEvent
                                         {
                                             AdventureId = ProcessExecutionContext.AdventureId.Value ?? Guid.Empty,
                                             CallerName = operationName,
                                             RequestContent = JsonSerializer.Serialize(chatHistory),
                                             ResponseContent = JsonSerializer.Serialize(result),
                                             InputToken = replyInnerContent?.Usage.InputTokenCount,
                                             OutputToken = replyInnerContent?.Usage.OutputTokenCount,
                                             TotalToken = replyInnerContent?.Usage.TotalTokenCount,
                                             Duration = stopwatch.ElapsedMilliseconds
                                         },
                                         token);

                                     return result.Content;
                                 }
                                 catch (HttpOperationException ex) when (ex.StatusCode == HttpStatusCode.BadGateway)
                                 {
                                     _logger.Error(ex, "Bad Gateway error occurred while calling LLM service. {message}", ex.ResponseContent);
                                     throw;
                                 }
                             },
                             cancellationToken)
                         ?? string.Empty;
            if (string.IsNullOrEmpty(result))
            {
                throw new LlmEmptyResponseException();
            }

            chatHistory.AddUserMessage(result);
            return outputFunc(result);
        }
    }
}