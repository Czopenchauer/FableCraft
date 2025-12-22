using System.Text;
using System.Text.Json.Serialization;

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
using SearchResult = FableCraft.Application.NarrativeEngine.Models.SearchResult;

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
        await using ApplicationDbContext dbContext = await DbContextFactory.CreateDbContextAsync(cancellationToken);
        var systemPrompt = await GetPromptAsync(context);
        var hasSceneContext = context.SceneContext.Length > 0;

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(systemPrompt);

        var contextPrompt = $"""
                             {PromptSections.WorldSettings(context.WorldSettings)}

                             {PromptSections.MainCharacter(context)}

                             {(context.Characters.Count > 0 ? PromptSections.ExistingCharacters(context.Characters) : "")}

                             {PromptSections.CurrentStoryTracker(context.SceneContext)}
                             
                             {LoreGenerated(context)}

                             {(hasSceneContext ? PromptSections.RecentScenes(context.SceneContext, SceneContextCount) : "")}
                             """;
        chatHistory.AddUserMessage(contextPrompt);

        string requestPrompt;
        if (!hasSceneContext)
        {
            var adventure = await dbContext.Adventures
                .Select(x => new { x.Id, x.FirstSceneGuidance })
                .SingleAsync(x => x.Id == context.AdventureId, cancellationToken);

            requestPrompt = adventure.FirstSceneGuidance;
        }
        else
        {
            requestPrompt = PromptSections.LastNarrativeDirections(context.SceneContext);
        }

        chatHistory.AddUserMessage(requestPrompt);

        var outputParser = ResponseParser.CreateJsonParser<ContextToFetch>("output");
        Kernel kernel = kernelBuilder.Create().Build();

        ContextToFetch queries = await agentKernel.SendRequestAsync(
            chatHistory,
            outputParser,
            kernelBuilder.GetDefaultPromptExecutionSettings(),
            nameof(ContextGatherer),
            kernel,
            cancellationToken);

        var callerContext = new CallerContext(GetType(), context.AdventureId);
        try
        {
            var searchResults = await ragSearch.SearchAsync(
                callerContext,
                [RagClientExtensions.GetWorldDatasetName(context.AdventureId), RagClientExtensions.GetMainCharacterDatasetName(context.AdventureId)],
                queries.Queries,
                cancellationToken: cancellationToken);

            var baseSearch = searchResults.Select(x =>
            {
                var response = new StringBuilder();
                foreach (SearchResultItem searchResultItem in x.Response.Results)
                {
                    if (searchResultItem.DatasetName == RagClientExtensions.GetWorldDatasetName(context.AdventureId))
                    {
                        response.AppendLine($"""
                                             World Knowledge:
                                             {string.Join("\n", searchResultItem.Text)}
                                             """);
                    }
                    else
                    {
                        response.AppendLine($"""
                                             {context.MainCharacter.Name} Knowledge:
                                             {string.Join("\n", searchResultItem.Text)}
                                             """);
                    }
                }

                return new SearchResult
                {
                    Query = x.Query,
                    Response = response.ToString()
                };
            }).ToList();
            var previousLore = context.PreviouslyGeneratedLore.Select(x => new SearchResult()
            {
                Query = $"{x.Category}: {x.Title!}",
                Response = x.Content
            }).ToList();
            baseSearch.AddRange(previousLore);
            context.ContextGathered = new ContextBase
            {
                ContextBases = baseSearch.ToArray()
            };
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error filtering queries for adventure {AdventureId}", context.AdventureId);
        }
    }

    private class ContextToFetch
    {
        // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Local
        public string[] Queries { get; set; } = [];

        // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Local
        [JsonPropertyName("characters_to_fetch")]
        public string[] CharactersToFetch { get; set; } = [];
    }

    private string LoreGenerated(GenerationContext context)
    {
        if(context.PreviouslyGeneratedLore.Length > 0)
        {
            return $"""
                    Previously generated lore pieces. Don't query for these again!:
                    {string.Join("\n\n", context.PreviouslyGeneratedLore.Select(x => x.Content))}
                    """;
        }

        return string.Empty;
    }
}