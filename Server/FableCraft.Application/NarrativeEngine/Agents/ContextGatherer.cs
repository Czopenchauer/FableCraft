using System.Text.Json.Serialization;

using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Application.NarrativeEngine.Workflow;
using FableCraft.Infrastructure.Clients;
using FableCraft.Infrastructure.Llm;
using FableCraft.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;

using Serilog;

using IKernelBuilder = FableCraft.Infrastructure.Llm.IKernelBuilder;
using SearchResult = FableCraft.Application.NarrativeEngine.Models.SearchResult;

namespace FableCraft.Application.NarrativeEngine.Agents;

internal sealed class ContextGatherer(
    IAgentKernel agentKernel,
    IRagSearch ragSearch,
    ILogger logger,
    KernelBuilderFactory kernelBuilderFactory,
    ApplicationDbContext dbContext) : IProcessor
{
    private const int SceneContextCount = 10;

    public async Task Invoke(
        GenerationContext context,
        CancellationToken cancellationToken)
    {
        IKernelBuilder kernelBuilder = kernelBuilderFactory.Create(context.ComplexPreset);
        var systemPrompt = await PromptBuilder.BuildPromptAsync("ContextBuilderAgent.md");
        var hasSceneContext = context.SceneContext.Length > 0;

        var builder = ChatHistoryBuilder.Create()
            .WithSystemMessage(systemPrompt)
            .WithStoryTracker(context);

        if (context.Characters.Count > 0)
        {
            builder.WithExistingCharacters(context.Characters);
        }

        if (!hasSceneContext)
        {
            var adventure = await dbContext.Adventures
                .Select(x => new { x.Id, x.FirstSceneGuidance })
                .SingleAsync(x => x.Id == context.AdventureId, cancellationToken);

            builder
                .WithUserMessage(adventure.FirstSceneGuidance)
                .WithMainCharacter(context.MainCharacter, context.LatestSceneContext?.Metadata.MainCharacterDescription);
        }
        else
        {
            builder
                .WithLastNarrativeDirections(context.SceneContext, 1)
                .WithMainCharacter(context.MainCharacter, context.LatestSceneContext?.Metadata.MainCharacterDescription)
                .WithMainCharacterTracker(context)
                .WithCurrentSceneTracker(context.SceneContext)
                .WithRecentScenes(context.SceneContext, SceneContextCount);
        }

        var chatHistory = builder.Build();

        var outputParser = ResponseParser.CreateJsonParser<ContextToFetch>("output");
        Kernel kernel = kernelBuilder.Create().Build();

        var queries = await agentKernel.SendRequestAsync(
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
            context.GenerationProcessStep = GenerationProcessStep.ContextGatheringFinished;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error filtering queries for adventure {AdventureId}", context.AdventureId);
        }
    }

    private class ContextToFetch
    {
        public string[] Queries { get; set; } = [];

        [JsonPropertyName("characters_to_fetch")]
        public string[] CharactersToFetch { get; set; } = [];
    }
}