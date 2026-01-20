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

namespace FableCraft.Application.NarrativeEngine.Agents;

internal sealed class CharacterTrackerAgent(
    IAgentKernel agentKernel,
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    KernelBuilderFactory kernelBuilderFactory,
    IPluginFactory pluginFactory) : BaseAgent(dbContextFactory, kernelBuilderFactory)
{
    protected override AgentName GetAgentName() => AgentName.CharacterTrackerAgent;

    public async Task<(CharacterTracker Tracker, bool IsDead)> InvokeAfterSimulation(
        GenerationContext generationContext,
        CharacterContext context,
        CharacterContext newCharacter,
        SceneTracker sceneTrackerResult,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await DbContextFactory.CreateDbContextAsync(cancellationToken);
        var previousTrackers = await dbContext.CharacterStates.Where(z => z.CharacterId == context.CharacterId).OrderByDescending(z => z.SequenceNumber).Take(2)
            .ToArrayAsync(cancellationToken);

        var requestPrompt = $"""
                             Previous trackers:
                             {string.Join("\n", previousTrackers.OrderBy(x => x.SequenceNumber).Select(x => $"{x.Tracker.ToJsonString()}"))}

                             {PromptSections.CharacterStateContext(context)}

                             New scenes content:
                             {string.Join("\n\n", newCharacter.SceneRewrites.Select(z => $"{z.SceneTracker.ToJsonString()}\n{z.Content}"))}

                             Update the character_tracker based on the scenes scenes!
                             """;
        return await InvokeInternal(requestPrompt, generationContext, context, sceneTrackerResult, cancellationToken);
    }
    
    public async Task<(CharacterTracker Tracker, bool IsDead)> Invoke(
        GenerationContext generationContext,
        CharacterContext context,
        CharacterContext newCharacter,
        SceneTracker sceneTrackerResult,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await DbContextFactory.CreateDbContextAsync(cancellationToken);
        var previousTrackers = await dbContext.CharacterStates.Where(z => z.CharacterId == context.CharacterId).OrderByDescending(z => z.SequenceNumber).Take(2)
            .ToArrayAsync(cancellationToken);
        var requestPrompt = $"""
                             Previous trackers:
                             {string.Join("\n", previousTrackers.OrderBy(x => x.SequenceNumber).Select(x => $"{x.Tracker.ToJsonString()}"))}

                             {PromptSections.CharacterStateContext(context)}

                             New scene content:
                             {newCharacter.SceneRewrites.First().Content}

                             Update the character_tracker based on the new scene.
                             """;
        return await InvokeInternal(requestPrompt, generationContext, context, sceneTrackerResult, cancellationToken);
    }

    private async Task<(CharacterTracker Tracker, bool IsDead)> InvokeInternal(
        string request,
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
                             {PromptSections.WorldSettings(generationContext.PromptPath)}

                             {PromptSections.SceneTracker(generationContext, sceneTrackerResult)}

                             {PromptSections.NewItems(generationContext.NewItems)}

                             {PromptSections.RecentScenesForCharacter(context)}
                             """;
        chatHistory.AddUserMessage(contextPrompt);

        chatHistory.AddUserMessage(request);

        var outputParser = CreateOutputParser();

        var kernel = kernelBuilder.Create();
        var callerContext = new CallerContext(GetType(), generationContext.AdventureId, generationContext.NewSceneId);
        await pluginFactory.AddPluginAsync<WorldKnowledgePlugin>(kernel, generationContext, callerContext);
        await pluginFactory.AddCharacterPluginAsync<CharacterNarrativePlugin>(kernel, generationContext, callerContext, context.CharacterId);
        var kernelWithKg = kernel.Build();

        var promptExecutionSettings = kernelBuilder.GetDefaultFunctionPromptExecutionSettings();
        promptExecutionSettings.FunctionChoiceBehavior = FunctionChoiceBehavior.None();

        var trackerDelta = await agentKernel.SendRequestAsync(
            chatHistory,
            outputParser,
            promptExecutionSettings,
            nameof(CharacterTrackerAgent),
            kernelWithKg,
            cancellationToken);

        return trackerDelta;
    }

    private static Func<string, (CharacterTracker Tracker, bool IsDead)>
        CreateOutputParser()
    {
        return response =>
        {
            var characterStats = ResponseParser.ExtractJson<CharacterStatus>(response, "status");
            var tracker = ResponseParser.ExtractJson<CharacterDeltaTrackerOutput<CharacterTracker>>(response, "tracker");

            return (tracker.Tracker, characterStats.IsDead);
        };
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