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

using Serilog;

namespace FableCraft.Application.NarrativeEngine.Agents;

internal sealed class MainCharacterTrackerAgent(
    IAgentKernel agentKernel,
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    KernelBuilderFactory kernelBuilderFactory,
    ILogger logger) : BaseAgent(dbContextFactory, kernelBuilderFactory)
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
                             {PromptSections.Context(context)}

                             {PromptSections.SceneTracker(context, sceneTrackerResult)}

                             {PromptSections.NewItems(context.NewItems)}

                             {PromptSections.PreviouslyCreatedContent(context)}

                             {PromptSections.MainCharacter(context)}

                             {(!isFirstScene ? PromptSections.LastScenes(context.SceneContext!, 5) : "")}
                             """;
        chatHistory.AddUserMessage(contextPrompt);

        MainCharacterTracker resultTracker;

        if (isFirstScene)
        {
            // First scene: use full tracker output (no previous state to merge with)
            resultTracker = await ProcessFirstScene(context, chatHistory, kernelBuilder, cancellationToken);
        }
        else
        {
            // Subsequent scenes: use delta output and merge with previous state
            resultTracker = await ProcessDeltaUpdate(context, chatHistory, kernelBuilder, sceneTrackerResult, cancellationToken);
        }

        context.NewTracker!.MainCharacter = new MainCharacterState
        {
            MainCharacter = resultTracker,
            MainCharacterDescription = null
        };
    }

    private async Task<MainCharacterTracker> ProcessFirstScene(
        GenerationContext context,
        ChatHistory chatHistory,
        FableCraft.Infrastructure.Llm.IKernelBuilder kernelBuilder,
        CancellationToken cancellationToken)
    {
        var requestPrompt = $"""
                             {PromptSections.SceneContent(context)}

                             It's the first scene of the adventure. Initialize the tracker based on the scene content and characters description.
                             """;
        chatHistory.AddUserMessage(requestPrompt);

        var outputParser = ResponseParser.CreateJsonParser<CharacterDeltaTrackerOutput<MainCharacterTracker>>("tracker");
        var promptExecutionSettings = kernelBuilder.GetDefaultPromptExecutionSettings();
        var kernel = kernelBuilder.Create();
        var kernelWithKg = kernel.Build();

        var tracker = await agentKernel.SendRequestAsync(
            chatHistory,
            outputParser,
            promptExecutionSettings,
            nameof(MainCharacterTrackerAgent),
            kernelWithKg,
            cancellationToken);

        return tracker.Tracker;
    }

    private async Task<MainCharacterTracker> ProcessDeltaUpdate(
        GenerationContext context,
        ChatHistory chatHistory,
        FableCraft.Infrastructure.Llm.IKernelBuilder kernelBuilder,
        SceneTracker sceneTrackerResult,
        CancellationToken cancellationToken)
    {
        // Get the previous tracker state
        var previousTracker = context.SceneContext!
            .OrderByDescending(x => x.SequenceNumber)
            .First()
            .Metadata!.Tracker!.MainCharacter!.MainCharacter
            ?? throw new InvalidOperationException("Previous main character tracker state is null");

        var requestPrompt = $"""
                             Previous tracker state:
                             {previousTracker.ToJsonString()}

                             {PromptSections.MainCharacterTracker(context.SceneContext!)}

                             New scene content:
                             {PromptSections.SceneContent(context)}

                             Update the main_character_tracker based on the new scene. Output ONLY the fields that changed in the updates object.
                             """;
        chatHistory.AddUserMessage(requestPrompt);

        var outputParser = ResponseParser.CreateJsonParser<MainCharacterDeltaOutput>("tracker");
        var promptExecutionSettings = kernelBuilder.GetDefaultPromptExecutionSettings();
        var kernel = kernelBuilder.Create();
        var kernelWithKg = kernel.Build();

        var deltaOutput = await agentKernel.SendRequestAsync(
            chatHistory,
            outputParser,
            promptExecutionSettings,
            nameof(MainCharacterTrackerAgent),
            kernelWithKg,
            cancellationToken);

        // Merge delta updates with previous state
        var mergedTracker = TrackerMerger.Merge(previousTracker, deltaOutput.Updates);

        logger.Information(
            "MainCharacterTracker delta merge completed. Updates applied: {UpdateCount} fields",
            CountUpdates(deltaOutput.Updates));

        return mergedTracker;
    }

    private static int CountUpdates(JsonElement updates)
    {
        if (updates.ValueKind != JsonValueKind.Object)
            return 0;

        return updates.EnumerateObject().Count();
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