using System.Net;
using System.Runtime.CompilerServices;
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
        CancellationToken cancellationToken,
        Kernel? kernel = null,
        [CallerMemberName] string callerName = "");
}

internal sealed class AgentKernel : IAgentKernel
{
    private readonly IKernelBuilder _builder;
    private readonly ILogger _logger;
    private readonly ResiliencePipeline _pipeline;
    private readonly IMessageDispatcher _messageDispatcher;

    public AgentKernel(IKernelBuilder builder, ILogger logger, IMessageDispatcher messageDispatcher)
    {
        _builder = builder;
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
        CancellationToken cancellationToken,
        Kernel? kernel = null,
        [CallerMemberName] string callerName = "")
    {
        Kernel agentKernel = kernel?.Clone() ?? _builder.WithBase().Build();
        var caller = ProcessExecutionContext.Caller.Value ?? callerName;
        var chatCompletionService = agentKernel.GetRequiredService<IChatCompletionService>();
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
                                 var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                                 ChatMessageContent result =
                                     await chatCompletionService.GetChatMessageContentAsync(chatHistory, promptExecutionSettings, kernel, token);
                                 var replyInnerContent = result.InnerContent as ChatCompletion;
                                 _logger.Information("Input usage: {usage}, output usage {output}, total usage {total}",
                                     replyInnerContent?.Usage.InputTokenCount,
                                     replyInnerContent?.Usage.OutputTokenCount,
                                     replyInnerContent?.Usage.TotalTokenCount);
                                 await _messageDispatcher.PublishAsync(new ResponseReceivedEvent
                                     {
                                         AdventureId = ProcessExecutionContext.AdventureId.Value ?? Guid.Empty,
                                         CallerName = caller,
                                         RequestContent = JsonSerializer.Serialize(chatHistory),
                                         ResponseContent = JsonSerializer.Serialize(result),
                                         InputToken = replyInnerContent?.Usage.InputTokenCount,
                                         OutputToken = replyInnerContent?.Usage.OutputTokenCount,
                                         TotalToken = replyInnerContent?.Usage.TotalTokenCount,
                                         Duration = stopwatch.ElapsedMilliseconds,
                                     },
                                     token);

                                 _logger.Debug("Generated response: {response}", JsonSerializer.Serialize(result));
                                 return result.Content;
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
    private static string GetCallerTypeName()
    {
        var stackTrace = new System.Diagnostics.StackTrace();
        var callerFrame = stackTrace.GetFrame(2);
        return callerFrame?.GetMethod()?.DeclaringType?.Name ?? "Unknown";
    }
}