using System.Text.Json;

using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Application.NarrativeEngine.Workflow;
using FableCraft.Infrastructure.Clients;
using FableCraft.Infrastructure.Llm;

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

using Serilog;

using IKernelBuilder = FableCraft.Infrastructure.Llm.IKernelBuilder;

namespace FableCraft.Application.NarrativeEngine.Agents;

internal sealed class ContextGatherer(
    IAgentKernel agentKernel,
    IRagSearch ragSearch,
    ILogger logger,
    KernelBuilderFactory kernelBuilderFactory) : IProcessor
{
    private const int SceneContextCount = 10;

    public async Task Invoke(
        GenerationContext context,
        CancellationToken cancellationToken)
    {
        IKernelBuilder kernelBuilder = kernelBuilderFactory.Create(context.LlmPreset);
        var chatHistory = new ChatHistory();
        var systemPrompt = await BuildInstruction();
        chatHistory.AddSystemMessage(systemPrompt);
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true
        };

        if (context.SceneContext.Length == 0)
        {
            context.GenerationProcessStep = GenerationProcessStep.ContextGatheringFinished;
            return;
        }

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

                             <last_narrative_directions>
                                {string.Join("\n", context.SceneContext.OrderByDescending(y => y.SequenceNumber)
                                    .Take(1)
                                    .Select(z =>
                                        $"""
                                         {JsonSerializer.Serialize(z.Metadata.NarrativeMetadata, options)}
                                         """))}
                             </last_narrative_directions>

                             <main_character>
                             {context.MainCharacter.Name}
                             {context.MainCharacter.Description}
                             </main_character>
                             """;
        chatHistory.AddUserMessage(contextPrompt);
        var outputFunc = new Func<string, string[]>(response =>
            JsonSerializer.Deserialize<string[]>(response.RemoveThinkingBlock().ExtractJsonFromMarkdown(), options) ?? throw new InvalidOperationException());
        Kernel kernel = kernelBuilder.Create().Build();
        var queries = await agentKernel.SendRequestAsync(chatHistory,
            outputFunc,
            kernelBuilder.GetDefaultPromptExecutionSettings(),
            nameof(ContextGatherer),
            kernel,
            cancellationToken);

        var callerContext = new CallerContext(GetType(), context.AdventureId);
        try
        {
            var searchResults = await ragSearch.SearchAsync(callerContext, queries, cancellationToken: cancellationToken);
            context.ContextGathered = searchResults.Select(x => new ContextBase
            {
                Query = x.Query,
                Response = string.Join("\n\n", x.Response.Results)
            }).ToList();
            context.ContextGathered = new List<ContextBase>();
            context.GenerationProcessStep = GenerationProcessStep.ContextGatheringFinished;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error filtering queries for adventure {AdventureId}", context.AdventureId);
            // Best effort - proceed without filtered context
        }
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