using System.Text.Json;
using System.Text.RegularExpressions;

using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Application.NarrativeEngine.Plugins;
using FableCraft.Infrastructure.Llm;

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

using OpenAI.Chat;

using Polly;
using Polly.Retry;

using Serilog;

using ChatMessageContent = Microsoft.SemanticKernel.ChatMessageContent;

namespace FableCraft.Application.NarrativeEngine.Agents;

internal sealed class Writer
{
    private readonly ILogger _logger;

    public Writer(ILogger logger)
    {
        _logger = logger;
    }

    public async Task<GeneratedScene> Invoke(Kernel kernel, NarrativeContext context, NarrativeDirectorOutput narrativeDirectorOutput, CancellationToken cancellationToken)
    {
        Kernel narrativeKernel = kernel.Clone();

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

        var chatHistory = new ChatHistory();
        var systemPrompt = await BuildInstruction();
        chatHistory.AddSystemMessage(systemPrompt);
        var promptContext = $"""
                             <scene_direction>
                             {narrativeDirectorOutput}
                             </scene_direction>
                             """;

        chatHistory.AddUserMessage(promptContext);
        var promptExecutionSettings = new OpenAIPromptExecutionSettings
        {
            MaxTokens = 200_000
        };

        var chatCompletionService = narrativeKernel.GetRequiredService<IChatCompletionService>();
        try
        {
            return await GetResponse(chatHistory, pipeline, chatCompletionService, promptExecutionSettings, kernel);
        }
        catch (InvalidCastException ex)
        {
            chatHistory.AddUserMessage($"I've encountered an error parsing your response. Fix your response. {ex.Message}");
            return await GetResponse(chatHistory, pipeline, chatCompletionService, promptExecutionSettings, kernel);
        }
    }

    private async Task<GeneratedScene> GetResponse(
        ChatHistory chatHistory,
        ResiliencePipeline pipeline,
        IChatCompletionService chatCompletionService,
        OpenAIPromptExecutionSettings promptExecutionSettings,
        Kernel kernel)
    {
        var result = await pipeline.ExecuteAsync(async token =>
                     {
                         ChatMessageContent result = await chatCompletionService.GetChatMessageContentAsync(chatHistory, promptExecutionSettings, kernel, token);
                         var replyInnerContent = result.InnerContent as ChatCompletion;
                         _logger.Information("Input usage: {usage}, output usage {output}, total usage {total}",
                             replyInnerContent?.Usage.InputTokenCount,
                             replyInnerContent?.Usage.OutputTokenCount,
                             replyInnerContent?.Usage.TotalTokenCount);
                         _logger.Debug("Generated response: {response}", JsonSerializer.Serialize(result));
                         return result.Content?.RemoveThinkingBlock();
                     })
                     ?? string.Empty;
        chatHistory.AddAssistantMessage(result);
        var match = Regex.Match(result, "<new_scene>(.*?)</new_scene>", RegexOptions.Singleline);
        if (match.Success)
        {
            return JsonSerializer.Deserialize<GeneratedScene>(match.Groups[1].Value) ?? throw new InvalidOperationException();
        }

        throw new InvalidCastException("Failed to parse CharacterStats from response due to stats not being in correct tags.");
    }

    private async static Task<string> BuildInstruction()
    {
        var promptPath = Path.Combine(
            AppContext.BaseDirectory,
            "Agents",
            "Prompts",
            "WriterPrompt.md"
        );

        return await File.ReadAllTextAsync(promptPath);
    }
}