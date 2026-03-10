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
///     Clinical Assessor Agent - Pass 2 of character reflection split.
///     Evaluates whether scene events produced durable shifts in a character's psychological
///     state, relationships, or self-concept. Writes in neutral, clinical third-person language.
///     Receives scene_rewrite from Pass 1 (ExperientialNarratorAgent) as "biased testimony".
///     Output:
///     - identity: Complete CharacterStats snapshot, or null if no changes
///     - relationships: Complete relationship snapshots for those that changed
///     Note: Does NOT produce scene_rewrite or memory (scene_rewrite is preserved from Pass 1).
/// </summary>
internal sealed class ClinicalAssessorAgent(
    IAgentKernel agentKernel,
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    KernelBuilderFactory kernelBuilderFactory,
    ILogger logger,
    IPluginFactory pluginFactory) : BaseAgent(dbContextFactory, kernelBuilderFactory)
{
    protected override AgentName GetAgentName() => AgentName.ClinicalAssessorAgent;

    public async Task<ClinicalAssessorOutput> Invoke(
        GenerationContext generationContext,
        CharacterContext context,
        string sceneRewrite,
        SceneTracker sceneTrackerResult,
        CancellationToken cancellationToken)
    {
        var kernelBuilder = await GetKernelBuilder(generationContext);

        var systemPrompt = await BuildInstruction(generationContext, context);

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(systemPrompt);

        var contextMessage = BuildContextMessage(generationContext, context);
        chatHistory.AddUserMessage(contextMessage);

        var requestPrompt = $"""
                             ## Current Scene (NEW - Evaluate This)

                             The following is {context.Name}'s subjective experience of the scene that just occurred, produced by the Experiential Narrator. This is what you must evaluate for durable changes to their psychological state or relationships.

                             The character's identity, tracker, and relationships provided in context represent their state BEFORE this scene.

                             <scene_context>
                             Time: {sceneTrackerResult.Time}
                             Location: {sceneTrackerResult.Location}
                             Characters Present: {string.Join(", ", sceneTrackerResult.CharactersPresent ?? [])}
                             </scene_context>

                             Assess whether THIS scene produced durable changes:

                             <current_scene>
                             {sceneRewrite}
                             </current_scene>
                             """;
        chatHistory.AddUserMessage(requestPrompt);

        var outputParser = ResponseParser.CreateJsonParser<ClinicalAssessorOutput>("character_assessment", true);

        var kernel = kernelBuilder.Create();
        var callerContext = new CallerContext($"{nameof(ClinicalAssessorAgent)}:{context.Name}", generationContext.AdventureId, generationContext.NewSceneId);
        await pluginFactory.AddCharacterPluginAsync<CharacterNarrativePlugin>(kernel, generationContext, callerContext, context.CharacterId);
        var kernelWithKg = kernel.Build();

        var promptExecutionSettings = kernelBuilder.GetDefaultFunctionPromptExecutionSettings();
        var output = await agentKernel.SendRequestAsync(
            chatHistory,
            outputParser,
            promptExecutionSettings,
            $"{nameof(ClinicalAssessorAgent)}:{context.Name}",
            kernelWithKg,
            cancellationToken);

        logger.Information(
            "ClinicalAssessor for {CharacterName}: identity_updated={IdentityUpdated}, relationships_updated={RelationshipsCount}",
            context.Name,
            output.Identity != null,
            output.Relationships.Length);

        return output;
    }

    private string BuildContextMessage(GenerationContext generationContext, CharacterContext context)
    {
        var jsonOptions = PromptSections.GetJsonOptions(true);

        var relationshipsText = string.Join("\n\n",
            context.Relationships.Select(r => $"""
                                               <relationship toward="{r.TargetCharacterName}">
                                               {r.Data.ToJsonString(jsonOptions)}
                                               </relationship>
                                               """));
        var contextMessage = $"""
                              <character_identity>
                              {context.CharacterState.ToJsonString(jsonOptions)}
                              </character_identity>

                              <character_tracker>
                              {context.CharacterTracker?.ToJsonString(jsonOptions) ?? "No physical state tracked."}
                              </character_tracker>

                              <relationships_on_scene>
                              {(string.IsNullOrEmpty(relationshipsText) ? "No established relationships with characters present." : relationshipsText)}
                              </relationships_on_scene>

                              {PromptSections.CharacterStorySummary(context)}

                              {PromptSections.RecentScenesForCharacter(context, count: CharacterAgent.SceneContext)}
                              """;

        return contextMessage;
    }

    private async Task<string> BuildInstruction(
        GenerationContext generationContext,
        CharacterContext characterContext)
    {
        var prompt = await GetPromptAsync(generationContext);
        prompt = prompt.Replace(PlaceholderNames.CharacterName, characterContext.Name);

        return prompt;
    }
}