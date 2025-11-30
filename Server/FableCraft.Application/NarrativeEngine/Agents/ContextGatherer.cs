using System.Text.Json;

using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Application.NarrativeEngine.Workflow;
using FableCraft.Infrastructure.Clients;
using FableCraft.Infrastructure.Llm;

using Microsoft.SemanticKernel.ChatCompletion;

using Serilog;

namespace FableCraft.Application.NarrativeEngine.Agents;

internal sealed class ContextGatherer(
    IAgentKernel agentKernel,
    IRagSearch ragSearch,
    ILogger logger,
    IKernelBuilder kernelBuilder) : IProcessor
{
    private const int SceneContextCount = 5;

    public async Task Invoke(
        GenerationContext context,
        CancellationToken cancellationToken)
    {
        var chatHistory = new ChatHistory();
        var systemPrompt = await BuildInstruction();
        chatHistory.AddSystemMessage(systemPrompt);
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true
        };

        var contextPrompt = $"""
                             <story_summary>
                             {context.Summary}
                             </story_summary>

                             <last_scenes>
                             {string.Join("\n", context.SceneContext
                                 .OrderByDescending(x => x.SequenceNumber)
                                 .Take(SceneContextCount)
                                 .Select(x =>
                                     $"""
                                      SCENE NUMBER: {x.SequenceNumber}
                                      {x.SceneContent}
                                      {x.PlayerChoice}
                                      """))}
                             </last_scenes>

                             <main_character>
                             {context.MainCharacter.Name}
                             {context.MainCharacter.Description}
                             </main_character>
                             """;
        chatHistory.AddUserMessage(contextPrompt);
        var outputFunc = new Func<string, string[]>(response =>
            JsonSerializer.Deserialize<string[]>(response.RemoveThinkingBlock().ExtractJsonFromMarkdown(), options) ?? throw new InvalidOperationException());
        var queries = await agentKernel.SendRequestAsync(chatHistory, outputFunc, kernelBuilder.GetDefaultPromptExecutionSettings(), cancellationToken);

        var callerContext = new CallerContext(GetType(), context.AdventureId);
        var tasks = queries
            .Select(async x =>
            {
                var searchResults = await ragSearch.SearchAsync(callerContext, x, cancellationToken: cancellationToken);
                return new ContextBase
                {
                    Query = x,
                    Response = string.Join("\n\n", searchResults)
                };
            })
            .ToList();

        List<ContextBase> results = [];
        foreach (var task in tasks)
        {
            try
            {
                results.Add(await task);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error gathering context for adventure {AdventureId}", context.AdventureId);
                // Best effort - continue with other tasks. Other agents can query the RAG system again if needed.
            }
        }

        context.ContextGathered = results;
        context.GenerationProcessStep = GenerationProcessStep.ContextGatheringFinished;
    }

    private async static Task<string> BuildInstruction()
    {
        var promptPath = Path.Combine(
            AppContext.BaseDirectory,
            "NarrativeEngine",
            "Agents",
            "Prompts",
            "ContextBuilderAgent.md"
        );

        return await File.ReadAllTextAsync(promptPath);
    }
}