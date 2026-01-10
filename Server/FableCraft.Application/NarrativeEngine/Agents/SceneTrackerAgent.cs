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

using IKernelBuilder = FableCraft.Infrastructure.Llm.IKernelBuilder;

namespace FableCraft.Application.NarrativeEngine.Agents;

internal sealed class SceneTrackerAgent(
    IAgentKernel agentKernel,
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    KernelBuilderFactory kernelBuilderFactory,
    IPluginFactory pluginFactory) : BaseAgent(dbContextFactory, kernelBuilderFactory)
{
    protected override AgentName GetAgentName() => AgentName.SceneTrackerAgent;

    public async Task<SceneTracker> Invoke(GenerationContext context, CancellationToken cancellationToken)
    {
        IKernelBuilder kernelBuilder = await GetKernelBuilder(context);

        var systemPrompt = await BuildInstruction(context);
        var isFirstScene = (context.SceneContext?.Length ?? 0) == 0;

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(systemPrompt);

        var contextPrompt = $"""
                             {PromptSections.WorldSettings(context.PromptPath)}

                             {PromptSections.MainCharacter(context)}

                             {PromptSections.ExistingCharacters(context.Characters)}

                             {(!isFirstScene ? PromptSections.LastScenes(context.SceneContext!, 5) : "")}
                             
                             {(context.NewCharacters?.Count > 0 ? PromptSections.NewCharacters(context.NewCharacters) : "")}
                             
                             {(context.NewLocations?.Length > 0 ? PromptSections.NewLocations(context.NewLocations) : "")}
                             """;
        chatHistory.AddUserMessage(contextPrompt);

        string requestPrompt;
        if (isFirstScene)
        {
            requestPrompt = $"""
                             {(context.SceneContext?.Length < 1 ? PromptSections.AdventureStartTime(context.AdventureStartTime) : "")}
                             
                             {PromptSections.SceneContent(context.NewScene!.Scene)}

                             It's the first scene of the adventure. Initialize the tracker based on the scene content.
                             """;
        }
        else
        {
            requestPrompt = $"""

                             {(!isFirstScene ? PromptSections.PreviousSceneTrackers(context.SceneContext!) : "")}

                             THIS IS CURRENT SCENE! Update the story tracker based on the scene content and previous trackers.
                             {PromptSections.CurrentScene(context)}
                             """;
        }

        chatHistory.AddUserMessage(requestPrompt);

        Microsoft.SemanticKernel.IKernelBuilder kernel = kernelBuilder.Create();
        var callerContext = new CallerContext(GetType(), context.AdventureId);
        await pluginFactory.AddPluginAsync<WorldKnowledgePlugin>(kernel, context, callerContext);
        await pluginFactory.AddPluginAsync<MainCharacterNarrativePlugin>(kernel, context, callerContext);
        Kernel kernelWithKg = kernel.Build();

        var outputParser = ResponseParser.CreateJsonParser<SceneTracker>("scene_tracker", true);
        PromptExecutionSettings promptExecutionSettings = kernelBuilder.GetDefaultFunctionPromptExecutionSettings();

        return await agentKernel.SendRequestAsync(
            chatHistory,
            outputParser,
            promptExecutionSettings,
            nameof(SceneTrackerAgent),
            kernelWithKg,
            cancellationToken);
    }

    private async Task<string> BuildInstruction(GenerationContext context)
    {
        var structure = context.TrackerStructure;
        var prompt = await GetPromptAsync(context);
        return PromptBuilder.ReplacePlaceholders(prompt,
            (PlaceholderNames.SceneTrackerStructure, TrackerExtensions.ConvertToSystemJson(structure.Story).ToJsonString()),
            (PlaceholderNames.SceneTrackerOutput, TrackerExtensions.ConvertToOutputJson(structure.Story).ToJsonString()));
    }
}