using System.Text.Json;

using FableCraft.Application.NarrativeEngine.Agents.Builders;
using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Infrastructure.Llm;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;

using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

using Serilog;

namespace FableCraft.Application.NarrativeEngine.Agents;

internal sealed class ProgressionAgent(
    IAgentKernel agentKernel,
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    KernelBuilderFactory kernelBuilderFactory,
    ILogger logger) : BaseAgent(dbContextFactory, kernelBuilderFactory)
{
    private const string SkillsKey = "Skills";
    private const string AbilitiesKey = "Abilities";

    protected override AgentName GetAgentName() => AgentName.ProgressionAgent;

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
                             Previous tracker state:
                             {PromptSections.MainCharacterTracker(context.SceneContext!)}

                             New scene content:
                             {PromptSections.SceneContent(context)}

                             Update the main character's Skills and Abilities based on the new scene. Output ONLY changes to the Skills and Abilities arrays in the updates object.
                             """;
        chatHistory.AddUserMessage(requestPrompt);

        var outputParser = ResponseParser.CreateJsonParser<ProgressionDeltaOutput>("progression", true);
        var promptExecutionSettings = kernelBuilder.GetDefaultPromptExecutionSettings();
        var kernel = kernelBuilder.Create();
        var kernelWithKg = kernel.Build();

        var deltaOutput = await agentKernel.SendRequestAsync(
            chatHistory,
            outputParser,
            promptExecutionSettings,
            nameof(ProgressionAgent),
            kernelWithKg,
            cancellationToken);

        if (deltaOutput.NoProgression || deltaOutput.Updates.ValueKind is not JsonValueKind.Object)
        {
            logger.Information("ProgressionAgent: no progression for {Character}", context.MainCharacter.Name);
            return null;
        }

        logger.Information("ProgressionAgent delta produced for {Character}", context.MainCharacter.Name);
        return deltaOutput.Updates;
    }

    public async Task<JsonElement?> InvokeForCharacter(
        GenerationContext context,
        CharacterContext character,
        SceneTracker sceneTrackerResult,
        CancellationToken cancellationToken)
    {
        var characterSchema = TrackerExtensions.ConvertToSystemJson(context.TrackerStructure.Characters);

        if (!characterSchema.ContainsKey(SkillsKey) && !characterSchema.ContainsKey(AbilitiesKey))
        {
            logger.Information("ProgressionAgent: no Skills/Abilities schema for NPC {Character}, skipping", character.Name);
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
                             Previous tracker state:
                             {PromptSections.CharacterTrackerForContext(character)}

                             New scene content:
                             {lastSceneContent}

                             Update {character.Name}'s Skills and Abilities based on the new scene. Output ONLY changes to the Skills and Abilities arrays in the updates object.
                             """;
        chatHistory.AddUserMessage(requestPrompt);

        var outputParser = ResponseParser.CreateJsonParser<ProgressionDeltaOutput>("progression", true);
        var promptExecutionSettings = kernelBuilder.GetDefaultPromptExecutionSettings();
        var kernel = kernelBuilder.Create();
        var kernelWithKg = kernel.Build();

        var deltaOutput = await agentKernel.SendRequestAsync(
            chatHistory,
            outputParser,
            promptExecutionSettings,
            $"{nameof(ProgressionAgent)}:{character.Name}",
            kernelWithKg,
            cancellationToken);

        if (deltaOutput.NoProgression || deltaOutput.Updates.ValueKind is not JsonValueKind.Object)
        {
            logger.Information("ProgressionAgent: no progression for NPC {Character}", character.Name);
            return null;
        }

        logger.Information("ProgressionAgent delta produced for NPC {Character}", character.Name);
        return deltaOutput.Updates;
    }

    private async Task<string> BuildInstruction(GenerationContext context)
    {
        var options = PromptSections.GetJsonOptions();
        var mainCharSchema = TrackerExtensions.ConvertToSystemJson(context.TrackerStructure.MainCharacter);

        var skillsSchema = mainCharSchema.TryGetValue(SkillsKey, out var skills) ? skills : new { };
        var abilitiesSchema = mainCharSchema.TryGetValue(AbilitiesKey, out var abilities) ? abilities : new { };

        var prompt = await GetPromptAsync(context);
        return PromptBuilder.ReplacePlaceholders(prompt,
            (PlaceholderNames.SkillsSchema, JsonSerializer.Serialize(skillsSchema, options)),
            (PlaceholderNames.AbilitiesSchema, JsonSerializer.Serialize(abilitiesSchema, options)),
            (PlaceholderNames.CharacterName, context.MainCharacter.Name));
    }

    private async Task<string> BuildInstructionForCharacter(GenerationContext context, CharacterContext character)
    {
        var options = PromptSections.GetJsonOptions();
        var characterSchema = TrackerExtensions.ConvertToSystemJson(context.TrackerStructure.Characters);

        var skillsSchema = characterSchema.TryGetValue(SkillsKey, out var skills) ? skills : new { };
        var abilitiesSchema = characterSchema.TryGetValue(AbilitiesKey, out var abilities) ? abilities : new { };

        var prompt = await GetPromptAsync(context);
        return PromptBuilder.ReplacePlaceholders(prompt,
            (PlaceholderNames.SkillsSchema, JsonSerializer.Serialize(skillsSchema, options)),
            (PlaceholderNames.AbilitiesSchema, JsonSerializer.Serialize(abilitiesSchema, options)),
            (PlaceholderNames.CharacterName, character.Name));
    }
}
