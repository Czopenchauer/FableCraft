using System.Text.Json;

using FableCraft.Application.NarrativeEngine.Agents.Builders;
using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Application.NarrativeEngine.Plugins;
using FableCraft.Application.NarrativeEngine.Plugins.Impl;
using FableCraft.Infrastructure.Clients;
using FableCraft.Infrastructure.Llm;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;

using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace FableCraft.Application.NarrativeEngine.Agents;

internal sealed class MainCharacterTrackerAgent(
    IAgentKernel agentKernel,
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    IPluginFactory pluginFactory,
    KernelBuilderFactory kernelBuilderFactory) : BaseAgent(dbContextFactory, kernelBuilderFactory)
{
    protected override AgentName GetAgentName() => AgentName.MainCharacterTrackerAgent;

    public async Task Invoke(
        GenerationContext context,
        SceneTracker sceneTrackerResult,
        CancellationToken cancellationToken)
    {
        var kernelBuilder = await GetKernelBuilder(context);

        var systemPrompt = await BuildInstruction(context);
        var isFirstScene = (context.SceneContext?.Length ?? 0) == 0;

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(systemPrompt);

        var contextPrompt = $"""
                             {PromptSections.SceneTracker(context, sceneTrackerResult)}

                             {PromptSections.NewItems(context.NewItems)}

                             {PromptSections.PreviouslyCreatedContent(context)}

                             {PromptSections.MainCharacter(context)}

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
                             Previous trackers:
                             {string.Join("\n", context.SceneContext!
                                 .OrderByDescending(x => x.SequenceNumber)
                                 .Take(3)
                                 .OrderBy(x => x.SequenceNumber)
                                 .Select(x => x.Metadata!.Tracker!.MainCharacter!.MainCharacter.ToJsonString()))}

                             {PromptSections.MainCharacterTracker(context.SceneContext!)}

                             New scene content:
                             {PromptSections.SceneContent(context.NewScene?.Scene)}

                             Update the main_character_tracker based on the new scene.
                             """;
        }

        chatHistory.AddUserMessage(requestPrompt);

        var outputParser = ResponseParser.CreateJsonParser<CharacterDeltaTrackerOutput<MainCharacterTracker>>("tracker");
        var promptExecutionSettings = kernelBuilder.GetDefaultFunctionPromptExecutionSettings();
        var kernel = kernelBuilder.Create();
        var callerContext = new CallerContext(GetType(), context.AdventureId, context.NewSceneId);
        await pluginFactory.AddPluginAsync<WorldKnowledgePlugin>(kernel, context, callerContext);
        await pluginFactory.AddPluginAsync<MainCharacterNarrativePlugin>(kernel, context, callerContext);
        var kernelWithKg = kernel.Build();

        var tracker = await agentKernel.SendRequestAsync(
            chatHistory,
            outputParser,
            promptExecutionSettings,
            nameof(MainCharacterTrackerAgent),
            kernelWithKg,
            cancellationToken);
        context.NewTracker!.MainCharacter = new MainCharacterState
        {
            MainCharacter = tracker.Tracker,
            MainCharacterDescription = null
        };
    }

    private async Task<string> BuildInstruction(GenerationContext context)
    {
        var options = PromptSections.GetJsonOptions();
        var structure = context.TrackerStructure;
        var trackerPrompt = GetSystemPrompt(structure);

        var prompt = await GetPromptAsync(context);
        return PromptBuilder.ReplacePlaceholders(prompt,
            (PlaceholderNames.MainCharacterTrackerStructure, JsonSerializer.Serialize(trackerPrompt, options)),
            (PlaceholderNames.MainCharacterTrackerOutput, JsonSerializer.Serialize(GetOutputJson(structure), options)),
            (PlaceholderNames.CharacterName, context.MainCharacter.Name));
    }

    private static Dictionary<string, object> GetOutputJson(TrackerStructure structure) => TrackerExtensions.ConvertToOutputJson(structure.MainCharacter);

    private static Dictionary<string, object> GetSystemPrompt(TrackerStructure structure) => TrackerExtensions.ConvertToSystemJson(structure.MainCharacter);
}