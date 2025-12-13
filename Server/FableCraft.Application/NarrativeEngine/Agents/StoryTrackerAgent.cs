using System.Text.Json;

using FableCraft.Application.NarrativeEngine.Agents.Builders;
using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Application.NarrativeEngine.Plugins;
using FableCraft.Infrastructure.Clients;
using FableCraft.Infrastructure.Llm;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;

using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

using IKernelBuilder = FableCraft.Infrastructure.Llm.IKernelBuilder;

namespace FableCraft.Application.NarrativeEngine.Agents;

internal sealed class StoryTrackerAgent(
    IAgentKernel agentKernel,
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    KernelBuilderFactory kernelBuilderFactory,
    IRagSearch ragSearch) : BaseAgent(dbContextFactory, kernelBuilderFactory)
{
    protected override AgentName GetAgentName() => AgentName.StoryTrackerAgent;

    public async Task<StoryTracker> Invoke(GenerationContext context, CancellationToken cancellationToken)
    {
        IKernelBuilder kernelBuilder = await GetKernelBuilder(context);

        var systemPrompt = await BuildInstruction(context);
        var isFirstScene = (context.SceneContext?.Length ?? 0) == 0;

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(systemPrompt);

        var contextPrompt = $"""
                             {PromptSections.MainCharacter(context)}

                             {PromptSections.ExistingCharacters(context.Characters)}

                             {(!isFirstScene ? PromptSections.LastScenes(context.SceneContext!, 5) : "")}
                             """;
        chatHistory.AddUserMessage(contextPrompt);

        string requestPrompt;
        if (isFirstScene)
        {
            requestPrompt = $"""
                             {PromptSections.SceneContent(context.NewScene!.Scene)}

                             It's the first scene of the adventure. Initialize the tracker based on the scene content.
                             """;
        }
        else
        {
            requestPrompt = $"""
                             {(context.NewCharacters?.Length > 0 ? PromptSections.NewCharacters(context.NewCharacters) : "")}

                             {(context.SceneContext?.Length == 1 ? PromptSections.AdventureStartTime(context.AdventureStartTime) : "")}

                             {(context.NewLocations?.Length > 0 ? PromptSections.NewLocations(context.NewLocations) : "")}

                             {(!isFirstScene ? PromptSections.PreviousStoryTrackers(context.SceneContext!) : "")}

                             THIS IS CURRENT SCENE! Update the story tracker based on the scene content and previous trackers.
                             {PromptSections.CurrentScene(context.NewScene!.Scene)}
                             """;
        }

        chatHistory.AddUserMessage(requestPrompt);

        Microsoft.SemanticKernel.IKernelBuilder kernel = kernelBuilder.Create();
        var kgPlugin = new KnowledgeGraphPlugin(ragSearch, new CallerContext(GetType(), context.AdventureId));
        kernel.Plugins.Add(KernelPluginFactory.CreateFromObject(kgPlugin));
        Kernel kernelWithKg = kernel.Build();

        var outputParser = ResponseParser.CreateJsonParser<StoryTracker>("tracker", true);
        PromptExecutionSettings promptExecutionSettings = kernelBuilder.GetDefaultFunctionPromptExecutionSettings();

        return await agentKernel.SendRequestAsync(
            chatHistory,
            outputParser,
            promptExecutionSettings,
            nameof(StoryTrackerAgent),
            kernelWithKg,
            cancellationToken);
    }

    private async Task<string> BuildInstruction(GenerationContext context)
    {
        JsonSerializerOptions options = PromptSections.GetJsonOptions();
        var structure = context.TrackerStructure;
        var trackerPrompt = GetSystemPrompt(structure);

        var prompt = await GetPromptAsync(context);
        return PromptBuilder.ReplacePlaceholders(prompt,
            (PlaceholderNames.StoryTrackerStructure, JsonSerializer.Serialize(trackerPrompt, options)),
            (PlaceholderNames.StoryTrackerOutput, JsonSerializer.Serialize(GetOutputJson(structure), options)));
    }

    private static Dictionary<string, object> GetOutputJson(TrackerStructure structure)
    {
        return TrackerExtensions.ConvertToOutputJson(structure.Story);
    }

    private static Dictionary<string, object> GetSystemPrompt(TrackerStructure structure)
    {
        return TrackerExtensions.ConvertToSystemJson(structure.Story);
    }
}