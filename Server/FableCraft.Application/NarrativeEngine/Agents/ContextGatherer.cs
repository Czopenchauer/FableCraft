using System.Text.Json.Serialization;

using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Application.NarrativeEngine.Workflow;
using FableCraft.Infrastructure.Clients;
using FableCraft.Infrastructure.Llm;
using FableCraft.Infrastructure.Persistence;

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

    protected override string GetName() => nameof(ContextGatherer);

    public async Task Invoke(
        GenerationContext context,
        CancellationToken cancellationToken)
    {
        IKernelBuilder kernelBuilder = await GetKernelBuilder(context.AdventureId);
        await using ApplicationDbContext dbContext = await DbContextFactory.CreateDbContextAsync(cancellationToken);
        var systemPrompt = await PromptBuilder.BuildPromptAsync("ContextBuilderAgent.md");
        var hasSceneContext = context.SceneContext.Length > 0;

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(systemPrompt);

        var contextPrompt = $"""
                             {PromptSections.MainCharacter(context)}

                             {(context.Characters.Count > 0 ? PromptSections.ExistingCharacters(context.Characters) : "")}

                             {PromptSections.CurrentStoryTracker(context.SceneContext)}

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
            var searchResults = await ragSearch.SearchAsync(callerContext, queries.Queries, cancellationToken: cancellationToken);
            context.ContextGathered = new ContextBase
            {
                ContextBases = searchResults.Select(x => new SearchResult
                {
                    Query = x.Query,
                    Response = string.Join("\n\n", x.Response.Results)
                }).ToArray(),
                RelevantCharacters = context.Characters
                    .Where(x => queries.CharactersToFetch.Contains(x.CharacterState.CharacterIdentity.FullName))
                    .ToArray()
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
        public string[] CharactersToFetch { get; set;} = [];
    }
}