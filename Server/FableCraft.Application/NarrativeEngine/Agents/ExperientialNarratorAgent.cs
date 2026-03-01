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

/// <summary>
///     Experiential Narrator Agent - Pass 1 of character reflection split.
///     Produces the character's subjective scene rewrite and death status.
///     Does NOT produce identity/relationship updates (that's Pass 2: CharacterReflectionAgent).
/// </summary>
internal sealed class ExperientialNarratorAgent(
    IAgentKernel agentKernel,
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    KernelBuilderFactory kernelBuilderFactory,
    ILogger logger,
    IPluginFactory pluginFactory) : BaseAgent(dbContextFactory, kernelBuilderFactory)
{
    protected override AgentName GetAgentName() => AgentName.ExperientialNarratorAgent;

    public async Task<ExperientialNarratorOutput> Invoke(
        GenerationContext generationContext,
        CharacterContext context,
        SceneTracker sceneTrackerResult,
        CancellationToken cancellationToken)
    {
        var kernelBuilder = await GetKernelBuilder(generationContext);

        var systemPrompt = await BuildInstruction(generationContext, context, sceneTrackerResult);

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(systemPrompt);

        var recentScenesContext = PromptSections.RecentScenesForCharacter(context, count: 20);
        if (!string.IsNullOrEmpty(recentScenesContext))
        {
            chatHistory.AddUserMessage(recentScenesContext);
        }

        var contextPrompt = $"""
                             {PromptSections.WorldContext(generationContext)}

                             {PromptSections.CharacterEmulationOutputs(generationContext, context.Name)}
                             """;
        chatHistory.AddUserMessage(contextPrompt);

        var requestPrompt = $"""
                             Process the following scene from {context.Name}'s perspective.

                             <scene>
                             {generationContext.NewScene?.Scene}
                             </scene>
                             """;
        chatHistory.AddUserMessage(requestPrompt);

        var outputParser = ResponseParser.CreateJsonParser<ExperientialNarratorOutput>("experiential_narrator", true);

        var kernel = kernelBuilder.Create();
        var callerContext = new CallerContext($"{nameof(ExperientialNarratorAgent)}:{context.Name}", generationContext.AdventureId, generationContext.NewSceneId);
        await pluginFactory.AddCharacterPluginAsync<CharacterNarrativePlugin>(kernel, generationContext, callerContext, context.CharacterId);
        await pluginFactory.AddPluginAsync<WorldKnowledgePlugin>(kernel, generationContext, callerContext);
        var kernelWithKg = kernel.Build();

        var promptExecutionSettings = kernelBuilder.GetDefaultFunctionPromptExecutionSettings();
        var output = await agentKernel.SendRequestAsync(
            chatHistory,
            outputParser,
            promptExecutionSettings,
            $"{nameof(ExperientialNarratorAgent)}:{context.Name}",
            kernelWithKg,
            cancellationToken);

        logger.Information(
            "ExperientialNarrator for {CharacterName}: is_dead={IsDead}, scene_rewrite length={Length}",
            context.Name,
            output.IsDead,
            output.SceneRewrite?.Length ?? 0);

        return output;
    }

    private async Task<string> BuildInstruction(
        GenerationContext generationContext,
        CharacterContext characterContext,
        SceneTracker sceneTrackerResult)
    {
        var prompt = await GetPromptAsync(generationContext);
        var jsonOptions = PromptSections.GetJsonOptions(true);

        prompt = prompt.Replace(PlaceholderNames.CharacterName, characterContext.Name);

        prompt = prompt.Replace("{{character_identity}}", characterContext.CharacterState.ToJsonString(jsonOptions));

        prompt = prompt.Replace("{{character_tracker}}",
            characterContext.CharacterTracker?.ToJsonString(jsonOptions) ?? "No physical state tracked.");

        var charactersPresent = sceneTrackerResult.CharactersPresent ?? [];
        var relationshipsOnScene = FormatRelationshipsOnScene(characterContext, charactersPresent, jsonOptions);
        prompt = prompt.Replace("{{relationships_on_scene}}", relationshipsOnScene);

        // Story summary placeholder - empty for now until StorySummaryAgent is implemented
        prompt = prompt.Replace("{{story_summary}}", string.Empty);

        return prompt;
    }

    private static string FormatRelationshipsOnScene(
        CharacterContext characterContext,
        string[] charactersPresent,
        System.Text.Json.JsonSerializerOptions jsonOptions)
    {
        if (characterContext.Relationships.Count == 0 || charactersPresent.Length == 0)
        {
            return "No established relationships with characters present.";
        }

        var presentSet = new HashSet<string>(charactersPresent, StringComparer.OrdinalIgnoreCase);
        var relevantRelationships = characterContext.Relationships
            .Where(r => presentSet.Contains(r.TargetCharacterName))
            .ToList();

        if (relevantRelationships.Count == 0)
        {
            return "No established relationships with characters present.";
        }

        var sb = new System.Text.StringBuilder();
        foreach (var rel in relevantRelationships)
        {
            sb.AppendLine($"### {rel.TargetCharacterName}");
            sb.AppendLine($"**Dynamic:** {rel.Dynamic}");
            if (rel.Data.Count > 0)
            {
                sb.AppendLine($"**Details:** {rel.Data.ToJsonString(jsonOptions)}");
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }
}
