using System.Text.Json;
using System.Text.Json.Serialization;

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

internal sealed class CharacterTrackerAgent(
    IAgentKernel agentKernel,
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    KernelBuilderFactory kernelBuilderFactory,
    IPluginFactory pluginFactory,
    ILogger logger) : BaseAgent(dbContextFactory, kernelBuilderFactory)
{
    protected override AgentName GetAgentName() => AgentName.CharacterTrackerAgent;

    private const int TrackerCount = 1;

    public async Task<(CharacterTracker Tracker, bool IsDead)> InvokeAfterSimulation(
        GenerationContext generationContext,
        CharacterContext context,
        CharacterContext newCharacter,
        SceneTracker sceneTrackerResult,
        CancellationToken cancellationToken)
    {
        var previousTracker = await GetPreviousTracker(context, cancellationToken);

        var requestPrompt = $"""
                             {PromptSections.CharacterStateContext(context)}

                             New scenes content:
                             {string.Join("\n\n", newCharacter.SceneRewrites.Select(z => $"{z.SceneTracker.ToJsonString()}\n{z.Content}"))}

                             Update the character_tracker based on the scenes. Output ONLY the fields that changed in the updates object.
                             """;
        return await InvokeInternal(requestPrompt, previousTracker, generationContext, context, sceneTrackerResult, cancellationToken);
    }

    public async Task<(CharacterTracker Tracker, bool IsDead)> Invoke(
        GenerationContext generationContext,
        CharacterContext context,
        string newScene,
        SceneTracker sceneTrackerResult,
        CancellationToken cancellationToken)
    {
        var previousTracker = await GetPreviousTracker(context, cancellationToken);

        var requestPrompt = $"""
                             {PromptSections.CharacterStateContext(context)}

                             New scene content:
                             {newScene}

                             Update the character_tracker based on the new scene. Output ONLY the fields that changed in the updates object.
                             """;
        return await InvokeInternal(requestPrompt, previousTracker, generationContext, context, sceneTrackerResult, cancellationToken);
    }

    private async Task<CharacterTracker> GetPreviousTracker(CharacterContext context, CancellationToken cancellationToken)
    {
        await using var dbContext = await DbContextFactory.CreateDbContextAsync(cancellationToken);
        var previousTrackers = await dbContext.CharacterStates
            .Where(z => z.CharacterId == context.CharacterId)
            .OrderByDescending(z => z.SequenceNumber)
            .Take(TrackerCount)
            .ToArrayAsync(cancellationToken);

        if (previousTrackers.Length == 0)
        {
            return context.CharacterTracker!;
        }

        return previousTrackers
            .OrderByDescending(x => x.SequenceNumber)
            .First()
            .Tracker;
    }

    private async Task<(CharacterTracker Tracker, bool IsDead)> InvokeInternal(
        string request,
        CharacterTracker previousTracker,
        GenerationContext generationContext,
        CharacterContext context,
        SceneTracker sceneTrackerResult,
        CancellationToken cancellationToken)
    {
        var kernelBuilder = await GetKernelBuilder(generationContext);

        var systemPrompt = await BuildInstruction(generationContext, context.Name);

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(systemPrompt);

        var contextPrompt = $"""
                             Previous tracker state:
                             {previousTracker.ToJsonString()}

                             {PromptSections.WorldContext(generationContext)}

                             {PromptSections.SceneTracker(generationContext, sceneTrackerResult)}

                             {PromptSections.NewItems(generationContext.NewItems)}

                             {PromptSections.PreviouslyCreatedContent(generationContext)}

                             {PromptSections.RecentScenesForCharacter(context, count: 4)}
                             """;
        chatHistory.AddUserMessage(contextPrompt);

        chatHistory.AddUserMessage(request);

        var kernel = kernelBuilder.Create();
        var callerContext = new CallerContext($"{nameof(CharacterTrackerAgent)}:{context.Name}", generationContext.AdventureId, generationContext.NewSceneId);
        await pluginFactory.AddPluginAsync<WorldKnowledgePlugin>(kernel, generationContext, callerContext);
        await pluginFactory.AddCharacterPluginAsync<CharacterNarrativePlugin>(kernel, generationContext, callerContext, context.CharacterId);
        var kernelWithKg = kernel.Build();

        var promptExecutionSettings = kernelBuilder.GetDefaultFunctionPromptExecutionSettings();

        var (deltaOutput, isDead) = await agentKernel.SendRequestAsync(
            chatHistory,
            CreateOutputParser(),
            promptExecutionSettings,
            $"{nameof(CharacterTrackerAgent)}:{context.Name}",
            kernelWithKg,
            cancellationToken);

        var mergedTracker = TrackerMerger.Merge(previousTracker, deltaOutput.Updates);

        logger.Information(
            "CharacterTracker delta merge completed for {CharacterName}. Updates applied: {UpdateCount} fields",
            context.Name,
            CountUpdates(deltaOutput.Updates));

        return (mergedTracker, isDead);
    }

    private static Func<string, (CharacterDeltaOutput Delta, bool IsDead)> CreateOutputParser()
    {
        return response =>
        {
            var deltaOutput = ResponseParser.ExtractJson<CharacterDeltaOutput>(response, "tracker");
            var status = ResponseParser.ExtractJson<CharacterStatus>(response, "status");
            return (deltaOutput, status.IsDead);
        };
    }

    private static int CountUpdates(JsonElement updates)
    {
        if (updates.ValueKind != JsonValueKind.Object)
            return 0;

        return updates.EnumerateObject().Count();
    }

    private class CharacterStatus
    {
        [JsonPropertyName("is_dead")]
        public bool IsDead { get; set; }
    }

    private async Task<string> BuildInstruction(GenerationContext context, string characterName)
    {
        var options = PromptSections.GetJsonOptions();
        var structure = context.TrackerStructure;
        var prompt = await GetPromptAsync(context);
        return PromptBuilder.ReplacePlaceholders(prompt,
            (PlaceholderNames.CharacterTrackerStructure, JsonSerializer.Serialize(GetSystemPrompt(structure), options)),
            (PlaceholderNames.CharacterTrackerOutput, JsonSerializer.Serialize(GetOutputJson(structure), options)),
            (PlaceholderNames.CharacterName, characterName));
    }

    private static Dictionary<string, object> GetOutputJson(TrackerStructure structure) => TrackerExtensions.ConvertToOutputJson(structure.Characters);

    private static Dictionary<string, object> GetSystemPrompt(TrackerStructure structure) => TrackerExtensions.ConvertToSystemJson(structure.Characters);
}