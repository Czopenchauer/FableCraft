using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

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
    ApplicationDbContext dbContext) : IProcessor
{
    private const int SceneContextCount = 10;

    public async Task Invoke(
        GenerationContext context,
        CancellationToken cancellationToken)
    {
        IKernelBuilder kernelBuilder = kernelBuilderFactory.Create(context.ComplexPreset);
        var chatHistory = new ChatHistory();
        var systemPrompt = await BuildInstruction();
        chatHistory.AddSystemMessage(systemPrompt);
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true
        };

        if (context.Characters.Count > 0)
        {
            chatHistory.AddUserMessage($"""
                                        <existing_characters>
                                        {string.Join("\n\n", context.Characters.Select(c => $"""
                                                                                             <character>
                                                                                             Name: {c.Name}
                                                                                             {c.Description}
                                                                                             </character>
                                                                                             """))}
                                        </existing_characters>
                                        """);
        }

        if (context.SceneContext.Length == 0)
        {
            var adventure = await dbContext.Adventures.Select(x => new { x.Id, x.FirstSceneGuidance }).SingleAsync(x => x.Id == context.AdventureId, cancellationToken);
            chatHistory.AddUserMessage(
                $"""
                 {adventure.FirstSceneGuidance}
                 """);
            chatHistory.AddUserMessage($"""
                                        <main_character>
                                        {context.MainCharacter.Name}
                                        {context.LatestSceneContext?.Metadata.MainCharacterDescription ?? context.MainCharacter.Description}
                                        </main_character>
                                        """);
        }
        else
        {
            chatHistory.AddUserMessage($"""
                                        <last_narrative_directions>
                                           {string.Join("\n", context.SceneContext.OrderByDescending(y => y.SequenceNumber)
                                               .Take(1)
                                               .Select(z =>
                                                   $"""
                                                    {JsonSerializer.Serialize(z.Metadata.NarrativeMetadata, options)}
                                                    """))}
                                        </last_narrative_directions>
                                        """);

            chatHistory.AddUserMessage($"""
                                        <main_character>
                                        {context.MainCharacter.Name}
                                        {context.MainCharacter.Description}
                                        </main_character>
                                        """);
            var lastTracker = context.SceneContext
                .Where(x => x.Metadata.Tracker != null)
                .OrderByDescending(x => x.SequenceNumber)
                .FirstOrDefault()?.Metadata.Tracker;

            if (lastTracker != null)
            {
                chatHistory.AddUserMessage($"""
                                            <current_scene_tracker>
                                            {JsonSerializer.Serialize(lastTracker, options)}
                                            </current_scene_tracker>
                                            """);
            }

            foreach (SceneContext sceneContext in context.SceneContext
                         .OrderByDescending(x => x.SequenceNumber)
                         .Take(SceneContextCount))
            {
                chatHistory.AddUserMessage(
                    $"""
                     <last_scene_{sceneContext.SequenceNumber}>
                     {sceneContext.SceneContent}
                     {sceneContext.PlayerChoice}
                     </last_scene_{sceneContext.SequenceNumber}>
                     """);
            }
        }

        var outputFunc = new Func<string, ContextToFetch>(response =>
        {
            Match match = Regex.Match(response, "<output>(.*?)</output>", RegexOptions.Singleline);
            if (match.Success)
            {
                return JsonSerializer.Deserialize<ContextToFetch>(match.Groups[1].Value.RemoveThinkingBlock().ExtractJsonFromMarkdown(), options)
                       ?? throw new InvalidOperationException();
            }

            throw new InvalidCastException("Failed to parse LocationGenerationResult from response due to output not being in correct tags.");
        });
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
            var searchResults = await ragSearch.SearchAsync(callerContext, queries.Queries, cancellationToken: cancellationToken);
            context.ContextGathered = new ContextBase
            {
                ContextBases = searchResults.Select(x => new SearchResult()
                {
                    Query = x.Query,
                    Response = string.Join("\n\n", x.Response.Results)
                }).ToArray(),
                RelevantCharacters = context.Characters.Where(x => queries.CharactersToFetch.Contains(x.CharacterState.CharacterIdentity.FullName)).ToArray()
            };
            context.GenerationProcessStep = GenerationProcessStep.ContextGatheringFinished;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error filtering queries for adventure {AdventureId}", context.AdventureId);
            // Best effort - proceed without filtered context
        }
    }

    private static Task<string> BuildInstruction()
    {
        return PromptBuilder.BuildPromptAsync("ContextBuilderAgent.md");
    }

    private class ContextToFetch
    {
        public string[] Queries { get; set; } = [];

        [JsonPropertyName("characters_to_fetch")]
        public string[] CharactersToFetch { get; set; } = [];
    }
}