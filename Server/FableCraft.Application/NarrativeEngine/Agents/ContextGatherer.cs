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

using IKernelBuilder = FableCraft.Infrastructure.Llm.IKernelBuilder;
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

    protected override AgentName GetAgentName() => AgentName.ContextGatherer;

    public async Task Invoke(
        GenerationContext context,
        CancellationToken cancellationToken)
    {
        if (context.ContextGathered != null)
        {
            logger.Information("Context already gathered for adventure {AdventureId}, skipping context gathering step.", context.AdventureId);
            return;
        }

        IKernelBuilder kernelBuilder = await GetKernelBuilder(context);
        var systemPrompt = await GetPromptAsync(context);

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(systemPrompt);

        var previousContext = GetPreviousContextSummary(context);
        var contextPrompt = $"""
                             {PromptSections.WorldSettings(context.WorldSettings)}

                             {PromptSections.MainCharacter(context)}

                             {(context.Characters.Count > 0 ? PromptSections.ExistingCharacters(context.Characters) : "")}

                             {PromptSections.CurrentStoryTracker(context.SceneContext)}

                             {LoreGenerated(context)}

                             {previousContext}

                             {(context.SceneContext.Length > 0 ? PromptSections.RecentScenes(context.SceneContext, SceneContextCount) : "")}

                             The latest scene that was just generated:
                             <latest_scene>
                             {context.NewScene?.Scene}
                             </latest_scene>
                             """;
        chatHistory.AddUserMessage(contextPrompt);

        var requestPrompt = $"""
                             {PromptSections.PreviousSceneGatheredContext(context)}

                             Based on the latest scene and narrative direction, analyze the context needs for the NEXT scene:
                             {PromptSections.SceneDirection(context.NewNarrativeDirection)}

                             {PromptSections.CurrentScene(context)}
                             """;

        chatHistory.AddUserMessage(requestPrompt);

        var outputParser = ResponseParser.CreateJsonParser<ContextGathererOutput>("output");
        Kernel kernel = kernelBuilder.Create().Build();

        ContextGathererOutput output = await agentKernel.SendRequestAsync(
            chatHistory,
            outputParser,
            kernelBuilder.GetDefaultPromptExecutionSettings(),
            nameof(ContextGatherer),
            kernel,
            cancellationToken);

        var callerContext = new CallerContext(GetType(), context.AdventureId);
        try
        {
            var worldContext = new List<ContextItem>(output.CarriedForward.WorldContext);
            var narrativeContext = new List<ContextItem>(output.CarriedForward.NarrativeContext);

            Task<SearchResult[]> worldQueryTasks = Task.FromResult(Array.Empty<SearchResult>());
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
                AnalysisSummary = output.AnalysisSummary,
                WorldContext = worldContext.ToArray(),
                NarrativeContext = narrativeContext.ToArray(),
                DroppedContext = output.DroppedContext
            };

            logger.Information(
                "Context gathered for adventure {AdventureId}: {WorldCount} world items, {NarrativeCount} narrative items, continuity: {Continuity}",
                context.AdventureId,
                worldContext.Count,
                narrativeContext.Count,
                output.AnalysisSummary.ContextContinuity);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error gathering context for adventure {AdventureId}", context.AdventureId);
        }
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

                Previous situation: {previousContext.AnalysisSummary.CurrentSituation}
                Context continuity: {previousContext.AnalysisSummary.ContextContinuity}

                World context:
                {(worldTopics.Length > 0 ? string.Join("\n", worldTopics) : "None")}

                Narrative context:
                {(narrativeTopics.Length > 0 ? string.Join("\n", narrativeTopics) : "None")}
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