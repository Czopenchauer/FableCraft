using FableCraft.Application.NarrativeEngine.Agents.Builders;
using FableCraft.Application.NarrativeEngine.Models;
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
    IRagClientFactory ragClientFactory,
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

                             {PromptSections.RecentScenesForCharacter(character, SceneContextCount)}

                             {previousContext}

                             Current simulation time period: {context.NewTracker?.Scene?.Time}
                             """;
        chatHistory.AddUserMessage(contextPrompt);

        var outputParser = ResponseParser.CreateJsonParser<CharacterContextGathererOutput>("output");
        var kernel = kernelBuilder.Create().Build();

        var output = await agentKernel.SendRequestAsync(
            chatHistory,
            outputParser,
            kernelBuilder.GetDefaultPromptExecutionSettings(),
            nameof(CharacterContextGatherer),
            kernel,
            cancellationToken);

        var callerContext = new CallerContext($"{GetType().Name}:{character.Name}", context.AdventureId, context.NewSceneId);
        try
        {
            var ragSearchClient = await ragClientFactory.CreateSearchClientForAdventure(context.AdventureId, cancellationToken);
            var worldContext = new List<ContextItem>(output.CarriedForward.WorldContext);
            var narrativeContext = new List<ContextItem>(output.CarriedForward.NarrativeContext);

            if (output.WorldQueries.Length > 0)
            {
                var worldQueries = output.WorldQueries.Select(q => q.Query).ToArray();
                var worldResults = await ragSearchClient.SearchAsync(
                    callerContext,
                    [RagClientExtensions.GetWorldDatasetName()],
                    worldQueries,
                    cancellationToken: cancellationToken);

                foreach (var result in worldResults)
                {
                    var content = string.Join("\n", result.Response.Results.Select(r => r.Text));
                    if (!string.IsNullOrWhiteSpace(content))
                    {
                        worldContext.Add(new ContextItem
                        {
                            Topic = result.Query,
                            Content = content
                        });
                    }
                }
            }

            if (output.NarrativeQueries.Length > 0)
            {
                var narrativeQueries = output.NarrativeQueries.Select(q => q.Query).ToArray();
                var characterDataset = RagClientExtensions.GetCharacterDatasetName(character.CharacterId);
                var narrativeResults = await ragSearchClient.SearchAsync(
                    callerContext,
                    [characterDataset],
                    narrativeQueries,
                    cancellationToken: cancellationToken);

                foreach (var result in narrativeResults)
                {
                    var content = string.Join("\n", result.Response.Results.Select(r => r.Text));
                    if (!string.IsNullOrWhiteSpace(content))
                    {
                        narrativeContext.Add(new ContextItem
                        {
                            Topic = result.Query,
                            Content = content
                        });
                    }
                }
            }

            var gatheredContext = new CharacterGatheredContext
            {
                WorldContext = worldContext.Select(x => new GatheredContextItem
                {
                    Topic = x.Topic,
                    Content = x.Content
                }).ToArray(),
                NarrativeContext = narrativeContext.Select(x => new GatheredContextItem
                {
                    Topic = x.Topic,
                    Content = x.Content
                }).ToArray(),
                AdditionalProperties = output.AdditionalData
            };

            logger.Information(
                "Character context gathered for {CharacterName}: {WorldCount} world items, {NarrativeCount} narrative items",
                character.Name,
                worldContext.Count,
                narrativeContext.Count);

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

        if (previousContext == null)
        {
            return string.Empty;
        }

        var worldTopics = previousContext.WorldContext.Select(c => $"- {c.Topic}: {c.Content}").ToArray();
        var narrativeTopics = previousContext.NarrativeContext.Select(c => $"- {c.Topic}: {c.Content}").ToArray();

        return $"""
                <previous_context_gathered>
                Context from previous simulation (consider carrying forward relevant items):
                World context:
                {(worldTopics.Length > 0 ? string.Join("\n", worldTopics) : "None")}

                Narrative context (character's own history):
                {(narrativeTopics.Length > 0 ? string.Join("\n", narrativeTopics) : "None")}
                </previous_context_gathered>
                """;
    }
}
