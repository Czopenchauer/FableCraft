using System.ComponentModel;
using System.Text.Json;

using FableCraft.Infrastructure.Llm;

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

using OpenAI.Chat;

using Polly;
using Polly.Retry;

using Serilog;

using ChatMessageContent = Microsoft.SemanticKernel.ChatMessageContent;

namespace FableCraft.Application.NarrativeEngine.Plugins;

internal sealed class CharacterCrafterPlugin
{
    private readonly ChatHistory _chatHistory;
    private readonly Kernel _kernel;
    private readonly ILogger _logger;

    public CharacterCrafterPlugin(
        NarrativeContext narrativeContext,
        Kernel kernel,
        ILogger logger)
    {
        _chatHistory = new ChatHistory();
        _logger = logger;
        _kernel = kernel;
    }

    [KernelFunction("create_character")]
    [Description(
        "Create a character based on the provided description. Use this to generate new characters with detailed personalities, backgrounds, abilities, and characteristics for the narrative.")]
    public async Task<string> CreateCharacter(
        [Description("The query or description for the character to be created")]
        string description)
    {
        ResiliencePipeline pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<InvalidCastException>(),
                MaxRetryAttempts = 1,
                Delay = TimeSpan.FromSeconds(5),
                OnRetry = args =>
                {
                    _logger.Warning("Attempt {attempt}: Retrying generation for type {type} due to error: {error}",
                        args.AttemptNumber,
                        nameof(CharacterCrafterPlugin),
                        args.Outcome.Exception?.Message);
                    return default;
                }
            })
            .Build();

        // TODO add system prompt for character creation
        _chatHistory.AddUserMessage(description);
        var promptExecutionSettings = new OpenAIPromptExecutionSettings
        {
            Temperature = 1,
            MaxTokens = 30000
        };

        var chatCompletionService = _kernel.GetRequiredService<IChatCompletionService>();
        try
        {
            return await GetResponse(pipeline, chatCompletionService, promptExecutionSettings);
        }
        catch (InvalidCastException ex)
        {
            _chatHistory.AddUserMessage($"I've encountered an error parsing your response. Fix your response. {ex.Message}");
            return await GetResponse(pipeline, chatCompletionService, promptExecutionSettings);
        }
    }

    private async Task<string> GetResponse(
        ResiliencePipeline pipeline,
        IChatCompletionService chatCompletionService,
        OpenAIPromptExecutionSettings promptExecutionSettings)
    {
        var result = await pipeline.ExecuteAsync(async token =>
                     {
                         ChatMessageContent result = await chatCompletionService.GetChatMessageContentAsync(_chatHistory, promptExecutionSettings, _kernel, token);
                         var replyInnerContent = result.InnerContent as ChatCompletion;
                         _logger.Information("Input usage: {usage}, output usage {output}, total usage {total}",
                             replyInnerContent?.Usage.InputTokenCount,
                             replyInnerContent?.Usage.OutputTokenCount,
                             replyInnerContent?.Usage.TotalTokenCount);
                         _logger.Debug("Generated response: {response}", JsonSerializer.Serialize(result));
                         return result.Content?.RemoveThinkingBlock();
                     })
                     ?? string.Empty;
        _chatHistory.AddAssistantMessage(result);
        return result;
    }
}