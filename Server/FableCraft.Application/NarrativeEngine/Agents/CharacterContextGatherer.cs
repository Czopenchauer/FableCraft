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
///     Gathers character-specific context from the knowledge graph after simulation.
///     Queries the world dataset and character's narrative dataset to build context
///     for the next simulation invocation.
/// </summary>
internal sealed class CharacterContextGatherer(
    IAgentKernel agentKernel,
    ILogger logger,
    IPluginFactory pluginFactory,
    KernelBuilderFactory kernelBuilderFactory,
    IDbContextFactory<ApplicationDbContext> dbContextFactory) : BaseAgent(dbContextFactory, kernelBuilderFactory)
{
    private const int SceneContextCount = 15;

    public async Task<CharacterGatheredContext> Invoke(
        GenerationContext context,
        CharacterContext character,
        CancellationToken cancellationToken)
    {
        var kernelBuilder = await GetKernelBuilder(context);
        var systemPrompt = await GetPromptAsync(context);

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(systemPrompt);

        var previousContext = GetPreviousContextSummary(character);
        var contextPrompt = $"""
                             ## Character Identity
                             <character>
                             Name: {character.Name}
                             Location: {character.CharacterTracker?.Location}
                             Importance: {character.Importance}
                             {character.Description}
                             </character>

                             {PromptSections.CharacterStateContext(character)}

                             {PromptSections.CharacterStorySummary(character)}

                             {PromptSections.RecentScenesForCharacter(character, SceneContextCount)}

                             {previousContext}

                             Current simulation time period: {context.NewTracker?.Scene?.Time}
                             """;
        chatHistory.AddUserMessage(contextPrompt);

        var callerContext = new CallerContext($"{GetType().Name}:{character.Name}", context.AdventureId, context.NewSceneId);
        var skKernelBuilder = kernelBuilder.Create();

        await pluginFactory.AddPluginAsync<WorldKnowledgePlugin>(skKernelBuilder, context, callerContext);
        await pluginFactory.AddCharacterPluginAsync<CharacterNarrativePlugin>(skKernelBuilder, context, callerContext, character.CharacterId);

        var kernel = skKernelBuilder.Build();
        var outputParser = ResponseParser.CreateTextParser("context");

        try
        {
            var contextText = await agentKernel.SendRequestAsync(
                chatHistory,
                outputParser,
                kernelBuilder.GetDefaultPromptExecutionSettings(),
                nameof(CharacterContextGatherer),
                kernel,
                cancellationToken);

            var gatheredContext = new CharacterGatheredContext
            {
                Context = contextText,
                AdditionalProperties = new Dictionary<string, object>()
            };

            logger.Information(
                "Character context gathered for {CharacterName}: {ContextLength} chars",
                character.Name,
                contextText.Length);

            return gatheredContext;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error gathering character context for {CharacterName}", character.Name);
            throw;
        }
    }

    protected override AgentName GetAgentName() => AgentName.ContextGatherer;

    private static string GetPreviousContextSummary(CharacterContext character)
    {
        var previousContext = character.SceneRewrites
            .OrderByDescending(x => x.SequenceNumber)
            .FirstOrDefault()?.GatheredContext;

        if (previousContext == null || string.IsNullOrEmpty(previousContext.Context))
        {
            return string.Empty;
        }

        return $"""
                <previous_context_gathered>
                Context from previous simulation (consider carrying forward relevant items):
                {previousContext.Context}
                </previous_context_gathered>
                """;
    }
}