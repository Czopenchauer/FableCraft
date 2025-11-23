using System.Net;
using System.Text.Json;

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
        CancellationToken cancellationToken,
        Kernel? kernel = null,
        PromptExecutionSettings? promptExecutionSettings = null);
}

internal sealed class AgentKernel : IAgentKernel
{
    private readonly IKernelBuilder _builder;
    private readonly ILogger _logger;
    private readonly ResiliencePipeline _pipeline;

    public AgentKernel(IKernelBuilder builder, ILogger logger)
    {
        _builder = builder;
        _logger = logger;
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
        CancellationToken cancellationToken,
        Kernel? kernel = null,
        PromptExecutionSettings? promptExecutionSettings = null)
    {
        Kernel agentKernel = kernel?.Clone() ?? _builder.WithBase().Build();
        var settings = promptExecutionSettings ?? _builder.GetDefaultPromptExecutionSettings();

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
                             ChatMessageContent result =
                                 await chatCompletionService.GetChatMessageContentAsync(chatHistory, settings, kernel, token);
                             var replyInnerContent = result.InnerContent as ChatCompletion;
                             _logger.Information("Input usage: {usage}, output usage {output}, total usage {total}",
                                 replyInnerContent?.Usage.InputTokenCount,
                                 replyInnerContent?.Usage.OutputTokenCount,
                                 replyInnerContent?.Usage.TotalTokenCount);
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
}