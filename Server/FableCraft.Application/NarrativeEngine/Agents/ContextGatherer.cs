using FableCraft.Application.NarrativeEngine.Agents.Builders;
using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Application.NarrativeEngine.Workflow;
using FableCraft.Infrastructure.Clients;
using FableCraft.Infrastructure.Llm;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;

using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

using Serilog;

using SearchResult = FableCraft.Infrastructure.Clients.SearchResult;

namespace FableCraft.Application.NarrativeEngine.Agents;

internal sealed class ContextGatherer(
    IAgentKernel agentKernel,
    IRagSearch ragSearch,
    ILogger logger,
    KernelBuilderFactory kernelBuilderFactory,
    IDbContextFactory<ApplicationDbContext> dbContextFactory) : BaseAgent(dbContextFactory, kernelBuilderFactory), IProcessor
{
    private const int SceneContextCount = 10;

    public async Task Invoke(
        GenerationContext context,
        CancellationToken cancellationToken)
    {
        if (context.ContextGathered != null)
        {
            logger.Information("Context already gathered for adventure {AdventureId}, skipping context gathering step.", context.AdventureId);
            return;
        }

        var kernelBuilder = await GetKernelBuilder(context);
        var systemPrompt = await GetPromptAsync(context);

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(systemPrompt);

        var previousContext = GetPreviousContextSummary(context);
        var contextPrompt = $"""
                             {PromptSections.WorldSettings(context.PromptPath)}

                             {PromptSections.MainCharacter(context)}

                             {(context.Characters.Count > 0 ? PromptSections.ExistingCharacters(context.Characters) : "")}

                             {PromptSections.CurrentSceneTracker(context)}

                             {LoreGenerated(context)}

                             {previousContext}

                             {CharacterRoster(context)}

                             {(context.SceneContext.Length > 0 ? PromptSections.RecentScenes(context.SceneContext, SceneContextCount) : "")}

                             The latest scene that was just generated:
                             <latest_scene>
                             {context.NewScene?.Scene}
                             </latest_scene>
                             """;
        chatHistory.AddUserMessage(contextPrompt);

        var requestPrompt = $"""
                             {PromptSections.PreviousSceneGatheredContext(context)}

                             {PromptSections.CurrentScene(context)}
                             """;

        chatHistory.AddUserMessage(requestPrompt);

        var outputParser = ResponseParser.CreateJsonParser<ContextGathererOutput>("output");
        var kernel = kernelBuilder.Create().Build();

        var output = await agentKernel.SendRequestAsync(
            chatHistory,
            outputParser,
            kernelBuilder.GetDefaultPromptExecutionSettings(),
            nameof(ContextGatherer),
            kernel,
            cancellationToken);

        var callerContext = new CallerContext(GetType(), context.AdventureId, context.NewSceneId);
        try
        {
            var worldContext = new List<ContextItem>(output.CarriedForward.WorldContext);
            var narrativeContext = new List<ContextItem>(output.CarriedForward.NarrativeContext);

            var worldQueryTasks = Task.FromResult(Array.Empty<SearchResult>());
            if (output.WorldQueries.Length > 0)
            {
                var worldQueries = output.WorldQueries.Select(q => q.Query).ToArray();
                worldQueryTasks = ragSearch.SearchAsync(
                    callerContext,
                    [RagClientExtensions.GetWorldDatasetName(context.AdventureId)],
                    worldQueries,
                    cancellationToken: cancellationToken);
            }

            if (output.NarrativeQueries.Length > 0)
            {
                var narrativeQueries = output.NarrativeQueries.Select(q => q.Query).ToArray();
                var narrativeResults = await ragSearch.SearchAsync(
                    callerContext,
                    [RagClientExtensions.GetMainCharacterDatasetName(context.AdventureId)],
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

            var worldResults = await worldQueryTasks;
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

            context.ContextGathered = new ContextBase
            {
                WorldContext = worldContext.ToArray(),
                NarrativeContext = narrativeContext.ToArray(),
                AdditionalData = output.AdditionalData,
                BackgroundRoster = output.BackgroundRoster
            };

            logger.Information(
                "Context gathered for adventure {AdventureId}: {WorldCount} world items, {NarrativeCount} narrative items",
                context.AdventureId,
                worldContext.Count,
                narrativeContext.Count);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error gathering context for adventure {AdventureId}", context.AdventureId);
        }
    }

    protected override AgentName GetAgentName() => AgentName.ContextGatherer;

    private static string CharacterRoster(GenerationContext context)
    {
        if (context.BackgroundCharacters.Count > 0)
        {
            var characters = context.BackgroundCharacters.Select(z => $"Name: {z.Name}, Identity: {z.Identity}, Last Location: {z.LastLocation}, Last SeenTime: {z.LastSeenTime}");
            return $"""
                    ### Background Character Registry
                    {string.Join("\n- ", characters)}
                    """;
        }

        return string.Empty;
    }

    private static string GetPreviousContextSummary(GenerationContext context)
    {
        var lastScene = context.SceneContext.MaxBy(x => x.SequenceNumber);
        var previousContext = lastScene?.Metadata.GatheredContext;
        if (previousContext == null)
        {
            return string.Empty;
        }

        var worldTopics = previousContext.WorldContext.Select(c => $"- {c.Topic}: {c.Content}").ToArray();
        var narrativeTopics = previousContext.NarrativeContext.Select(c => $"- {c.Topic}: {c.Content}").ToArray();

        return $"""
                <previous_context_gathered>
                Context from previous scene generation (consider carrying forward relevant items):
                World context:
                {(worldTopics.Length > 0 ? string.Join("\n", worldTopics) : "None")}

                Narrative context:
                {(narrativeTopics.Length > 0 ? string.Join("\n", narrativeTopics) : "None")}

                Additional data:
                {context.ContextGathered?.AdditionalData}
                </previous_context_gathered>
                """;
    }

    private static string LoreGenerated(GenerationContext context)
    {
        if (context.PreviouslyGeneratedLore.Length > 0)
        {
            return $"""
                    Previously generated lore pieces (will be added to world context automatically):
                    {string.Join("\n", context.PreviouslyGeneratedLore.Select(x => $"- {x.Category}: {x.Title}"))}
                    """;
        }

        return string.Empty;
    }
}