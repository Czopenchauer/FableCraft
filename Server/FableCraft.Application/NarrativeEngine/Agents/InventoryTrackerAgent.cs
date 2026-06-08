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

internal sealed class InventoryTrackerAgent(
    IAgentKernel agentKernel,
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    IPluginFactory pluginFactory,
    KernelBuilderFactory kernelBuilderFactory,
    ILogger logger) : BaseAgent(dbContextFactory, kernelBuilderFactory)
{
    private const string CarriedKey = "Carried";
    private const string AssetsKey = "Assets";

    protected override AgentName GetAgentName() => AgentName.InventoryTrackerAgent;

    public async Task<JsonElement?> Invoke(
        GenerationContext context,
        SceneTracker sceneTrackerResult,
        CancellationToken cancellationToken)
    {
        var kernelBuilder = await GetKernelBuilder(context);

        var systemPrompt = await BuildInstruction(context);

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(systemPrompt);

        var contextPrompt = $"""
                             {PromptSections.Context(context)}

                             {PromptSections.SceneTracker(context, sceneTrackerResult)}

                             {PromptSections.LastScenes(context.SceneContext!, 5)}
                             """;
        chatHistory.AddUserMessage(contextPrompt);

        var requestPrompt = $"""
                             {PromptSections.MainCharacterTracker(context.SceneContext!)}

                             New scene content:
                             {PromptSections.SceneContent(context)}

                             Update the main character's Carried and Assets based on the new scene. Output ONLY changes to Carried and/or Assets in the updates object.
                             """;
        chatHistory.AddUserMessage(requestPrompt);

        var outputParser = ResponseParser.CreateJsonParser<InventoryDeltaOutput>("inventory", true);
        var promptExecutionSettings = kernelBuilder.GetDefaultFunctionPromptExecutionSettings();
        var kernel = kernelBuilder.Create();
        var callerContext = new CallerContext(GetType().Name, context.AdventureId, context.NewSceneId);
        await pluginFactory.AddPluginAsync<WorldKnowledgePlugin>(kernel, context, callerContext);
        await pluginFactory.AddPluginAsync<MainCharacterNarrativePlugin>(kernel, context, callerContext);
        var kernelWithKg = kernel.Build();

        var deltaOutput = await agentKernel.SendRequestAsync(
            chatHistory,
            outputParser,
            promptExecutionSettings,
            nameof(InventoryTrackerAgent),
            kernelWithKg,
            cancellationToken);

        if (deltaOutput.NoInventoryChange || deltaOutput.Updates.ValueKind is not JsonValueKind.Object)
        {
            logger.Information("InventoryTrackerAgent: no inventory change for {Character}", context.MainCharacter.Name);
            return null;
        }

        logger.Information("InventoryTrackerAgent delta produced for {Character}", context.MainCharacter.Name);
        return deltaOutput.Updates;
    }

    public async Task<JsonElement?> InvokeForCharacter(
        GenerationContext context,
        CharacterContext character,
        SceneTracker sceneTrackerResult,
        CancellationToken cancellationToken)
    {
        var characterSchema = TrackerExtensions.ConvertToSystemJson(context.TrackerStructure.Characters);

        if (!characterSchema.ContainsKey(CarriedKey) && !characterSchema.ContainsKey(AssetsKey))
        {
            logger.Information("InventoryTrackerAgent: no Carried/Assets schema for NPC {Character}, skipping", character.Name);
            return null;
        }

        var kernelBuilder = await GetKernelBuilder(context);

        var systemPrompt = await BuildInstructionForCharacter(context, character);

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(systemPrompt);

        var contextPrompt = $"""
                             {PromptSections.Context(context)}

                             {PromptSections.SceneTracker(context, sceneTrackerResult)}

                             {PromptSections.RecentScenesForCharacter(character, count: 5)}
                             """;
        chatHistory.AddUserMessage(contextPrompt);

        var lastSceneContent = character.SceneRewrites
            .OrderByDescending(x => x.SequenceNumber)
            .FirstOrDefault()?.Content ?? string.Empty;

        var requestPrompt = $"""
                             {PromptSections.CharacterTrackerForContext(character)}

                             New scene content:
                             {lastSceneContent}

                             Update {character.Name}'s Carried and Assets based on the new scene. Output ONLY changes to Carried and/or Assets in the updates object.
                             """;
        chatHistory.AddUserMessage(requestPrompt);

        var outputParser = ResponseParser.CreateJsonParser<InventoryDeltaOutput>("inventory", true);
        var promptExecutionSettings = kernelBuilder.GetDefaultFunctionPromptExecutionSettings();
        var kernel = kernelBuilder.Create();
        var callerContext = new CallerContext($"{nameof(InventoryTrackerAgent)}:{character.Name}", context.AdventureId, context.NewSceneId);
        await pluginFactory.AddPluginAsync<WorldKnowledgePlugin>(kernel, context, callerContext);
        await pluginFactory.AddCharacterPluginAsync<CharacterNarrativePlugin>(kernel, context, callerContext, character.CharacterId);
        var kernelWithKg = kernel.Build();

        var deltaOutput = await agentKernel.SendRequestAsync(
            chatHistory,
            outputParser,
            promptExecutionSettings,
            $"{nameof(InventoryTrackerAgent)}:{character.Name}",
            kernelWithKg,
            cancellationToken);

        if (deltaOutput.NoInventoryChange || deltaOutput.Updates.ValueKind is not JsonValueKind.Object)
        {
            logger.Information("InventoryTrackerAgent: no inventory change for NPC {Character}", character.Name);
            return null;
        }

        logger.Information("InventoryTrackerAgent delta produced for NPC {Character}", character.Name);
        return deltaOutput.Updates;
    }

    private async Task<string> BuildInstruction(GenerationContext context)
    {
        var options = PromptSections.GetJsonOptions();
        var mainCharSchema = TrackerExtensions.ConvertToSystemJson(context.TrackerStructure.MainCharacter);

        var carriedSchema = mainCharSchema.TryGetValue(CarriedKey, out var carried) ? carried : new { };
        var assetsSchema = mainCharSchema.TryGetValue(AssetsKey, out var assets) ? assets : new { };

        var prompt = await GetPromptAsync(context);
        return PromptBuilder.ReplacePlaceholders(prompt,
            (PlaceholderNames.CarriedSchema, JsonSerializer.Serialize(carriedSchema, options)),
            (PlaceholderNames.AssetsSchema, JsonSerializer.Serialize(assetsSchema, options)),
            (PlaceholderNames.CharacterName, context.MainCharacter.Name));
    }

    private async Task<string> BuildInstructionForCharacter(GenerationContext context, CharacterContext character)
    {
        var options = PromptSections.GetJsonOptions();
        var characterSchemaObj = TrackerExtensions.ConvertToSystemJson(context.TrackerStructure.Characters);

        var carriedSchema = characterSchemaObj.TryGetValue(CarriedKey, out var carried) ? carried : new { };
        var assetsSchema = characterSchemaObj.TryGetValue(AssetsKey, out var assets) ? assets : new { };

        var prompt = await GetPromptAsync(context);
        return PromptBuilder.ReplacePlaceholders(prompt,
            (PlaceholderNames.CarriedSchema, JsonSerializer.Serialize(carriedSchema, options)),
            (PlaceholderNames.AssetsSchema, JsonSerializer.Serialize(assetsSchema, options)),
            (PlaceholderNames.CharacterName, character.Name));
    }
}