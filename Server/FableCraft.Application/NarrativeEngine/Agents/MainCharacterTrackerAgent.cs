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

internal sealed class MainCharacterTrackerAgent(
    IAgentKernel agentKernel,
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    KernelBuilderFactory kernelBuilderFactory,
    IRagSearch ragSearch) : BaseAgent(dbContextFactory, kernelBuilderFactory)
{
    protected override AgentName GetAgentName() => AgentName.MainCharacterTrackerAgent;

    public async Task<CharacterDeltaTrackerOutput> Invoke(
        GenerationContext context,
        StoryTracker storyTrackerResult,
        CancellationToken cancellationToken)
    {
        IKernelBuilder kernelBuilder = await GetKernelBuilder(context);

        var systemPrompt = await BuildInstruction(context);
        var isFirstScene = (context.SceneContext?.Length ?? 0) == 0;

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(systemPrompt);

        var contextPrompt = $"""
                             {PromptSections.WorldSettings(context.WorldSettings)}

                             {PromptSections.StoryTracker(storyTrackerResult, true)}

                             {PromptSections.NewItems(context.NewItems)}

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
                             {PromptSections.MainCharacterTracker(context.SceneContext!)}
                             
                             New scene content:
                             {PromptSections.SceneContent(context.NewScene?.Scene)}

                             Update the main_character_tracker based on the new scene.
                             """;
        }

        chatHistory.AddUserMessage(requestPrompt);

        var outputParser = ResponseParser.CreateJsonParser<CharacterDeltaTrackerOutput>("tracker");
        PromptExecutionSettings promptExecutionSettings = kernelBuilder.GetDefaultFunctionPromptExecutionSettings();
        Microsoft.SemanticKernel.IKernelBuilder kernel = kernelBuilder.Create();
        var datasets = new List<string>
        {
            RagClientExtensions.GetWorldDatasetName(context.AdventureId),
            RagClientExtensions.GetMainCharacterDatasetName(context.AdventureId)
        };
        var kgPlugin = new KnowledgeGraphPlugin(ragSearch, new CallerContext(GetType(), context.AdventureId), datasets);
        kernel.Plugins.Add(KernelPluginFactory.CreateFromObject(kgPlugin));
        Kernel kernelWithKg = kernel.Build();

        return await agentKernel.SendRequestAsync(
            chatHistory,
            outputParser,
            promptExecutionSettings,
            nameof(MainCharacterTrackerAgent),
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
            (PlaceholderNames.MainCharacterTrackerStructure, JsonSerializer.Serialize(trackerPrompt, options)),
            (PlaceholderNames.MainCharacterTrackerOutput, JsonSerializer.Serialize(GetOutputJson(structure), options)));
    }

    private static Dictionary<string, object> GetOutputJson(TrackerStructure structure)
    {
        return TrackerExtensions.ConvertToOutputJson(structure.MainCharacter);
    }

    private static Dictionary<string, object> GetSystemPrompt(TrackerStructure structure)
    {
        return TrackerExtensions.ConvertToSystemJson(structure.MainCharacter);
    }
}