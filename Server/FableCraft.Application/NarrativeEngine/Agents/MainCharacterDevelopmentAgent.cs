using System.Text.Json;

using FableCraft.Application.NarrativeEngine.Agents.Builders;
using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Infrastructure.Llm;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;

using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

using IKernelBuilder = FableCraft.Infrastructure.Llm.IKernelBuilder;

namespace FableCraft.Application.NarrativeEngine.Agents;

internal sealed class MainCharacterDevelopmentAgent(
    IAgentKernel agentKernel,
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    KernelBuilderFactory kernelBuilderFactory) : BaseAgent(dbContextFactory, kernelBuilderFactory)
{
    protected override AgentName GetAgentName() => AgentName.MainCharacterDevelopmentAgent;

    public async Task<CharacterDevelopmentTracker> Invoke(
        GenerationContext context,
        StoryTracker storyTrackerResult,
        CancellationToken cancellationToken)
    {
        IKernelBuilder kernelBuilder = await GetKernelBuilder(context);

        var systemPrompt = await BuildInstruction(context);
        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(systemPrompt);
        var isFirstScene = (context.SceneContext?.Length ?? 0) == 0;
        var contextPrompt = $"""
                             {PromptSections.StoryTracker(storyTrackerResult, true)}

                             {PromptSections.MainCharacter(context)}

                             {PromptSections.NewItems(context.NewItems)}

                             {(!isFirstScene ? PromptSections.LastScenes(context.SceneContext!, 5) : "")}
                             """;
        chatHistory.AddUserMessage(contextPrompt);

        string requestPrompt;
        if (isFirstScene)
        {
            requestPrompt = $"""
                             {PromptSections.SceneContent(context.NewScene?.Scene)}

                             It's the first scene of the adventure. Initialize the tracker based on the scene content and characters description.
                             """;
        }
        else
        {
            requestPrompt = $"""
                             {PromptSections.MainCharacterTracker(context.SceneContext!)}

                             New scene content:
                             {PromptSections.SceneContent(context.NewScene?.Scene)}

                             Update the main_character_tracker based on the new scene.
                             """;
        }

        chatHistory.AddUserMessage(requestPrompt);

        var outputParser = ResponseParser.CreateJsonParser<CharacterDevelopmentTracker>("character_development", true);
        PromptExecutionSettings promptExecutionSettings = kernelBuilder.GetDefaultPromptExecutionSettings();
        promptExecutionSettings.FunctionChoiceBehavior = FunctionChoiceBehavior.None();
        Kernel kernel = kernelBuilder.Create().Build();

        return await agentKernel.SendRequestAsync(
            chatHistory,
            outputParser,
            promptExecutionSettings,
            nameof(MainCharacterDevelopmentAgent),
            kernel,
            cancellationToken);
    }

    private async Task<string> BuildInstruction(GenerationContext context)
    {
        JsonSerializerOptions options = PromptSections.GetJsonOptions();
        var structure = context.TrackerStructure;
        var prompt = await GetPromptAsync(context);
        return PromptBuilder.ReplacePlaceholders(prompt,
            (PlaceholderNames.MainCharacterTrackerStructure, JsonSerializer.Serialize(GetSystemPrompt(structure), options)),
            (PlaceholderNames.MainCharacterDevelopmentOutput, JsonSerializer.Serialize(GetOutputJson(structure), options)));
    }

    private static Dictionary<string, object> GetOutputJson(TrackerStructure structure)
    {
        return TrackerExtensions.ConvertToOutputJson(structure.MainCharacterDevelopment!);
    }

    private static Dictionary<string, object> GetSystemPrompt(TrackerStructure structure)
    {
        return TrackerExtensions.ConvertToSystemJson(structure.MainCharacterDevelopment!);
    }
}