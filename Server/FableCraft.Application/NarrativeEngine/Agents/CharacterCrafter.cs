using System.Text.Json;
using System.Text.RegularExpressions;

using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Application.NarrativeEngine.Plugins;
using FableCraft.Infrastructure.Clients;
using FableCraft.Infrastructure.Llm;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;

using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

using IKernelBuilder = FableCraft.Infrastructure.Llm.IKernelBuilder;

namespace FableCraft.Application.NarrativeEngine.Agents;

internal sealed class CharacterCrafter(
    IAgentKernel agentKernel,
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    KernelBuilderFactory kernelBuilderFactory,
    IRagSearch ragSearch)
{
    public async Task<CharacterContext> Invoke(
        GenerationContext context,
        CharacterRequest request,
        CancellationToken cancellationToken)
    {
        IKernelBuilder kernelBuilder = kernelBuilderFactory.Create(context.ComplexPreset);
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var chatHistory = new ChatHistory();
        var trackerStructure = await dbContext
            .Adventures
            .Select(x => new { x.Id, x.TrackerStructure })
            .SingleAsync(x => x.Id == context.AdventureId, cancellationToken);

        var systemPrompt = await BuildInstruction(trackerStructure.TrackerStructure);
        chatHistory.AddSystemMessage(systemPrompt);
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true
        };

        Microsoft.SemanticKernel.IKernelBuilder kernel = kernelBuilder.Create();
        var kgPlugin = new KnowledgeGraphPlugin(ragSearch, new CallerContext(GetType(), context.AdventureId));
        kernel.Plugins.Add(KernelPluginFactory.CreateFromObject(kgPlugin));
        Kernel kernelWithKg = kernel.Build();

        chatHistory.AddUserMessage($"""
                                    <character_creation_context>
                                    {JsonSerializer.Serialize(request, options)}
                                    </character_creation_context>
                                    """);

        chatHistory.AddUserMessage($"""
                                    <context>
                                    {JsonSerializer.Serialize(context.ContextGathered, options)}
                                    </context>
                                    """);

        chatHistory.AddUserMessage($"""
                                    <previous_scene>
                                    {context.SceneContext.OrderByDescending(x => x.SequenceNumber).FirstOrDefault()?.SceneContent ?? string.Empty}
                                    </previous_scene>
                                    """);

        if (context.NewScene != null)
        {
            chatHistory.AddUserMessage($"""
                                        <current_scene>
                                        {context.NewScene.Scene}
                                        </current_scene>
                                        """);
        }

        var outputFunc =
            new Func<string, (CharacterStats characterStats, string description, CharacterTracker tracker, CharacterDevelopmentTracker developmentTracker)>(response =>
            {
                Match match = Regex.Match(response, "<character>(.*?)</character>", RegexOptions.Singleline);
                CharacterStats? characterStats;
                if (match.Success)
                {
                    characterStats = JsonSerializer.Deserialize<CharacterStats>(match.Groups[1].Value.RemoveThinkingBlock().ExtractJsonFromMarkdown(), options)
                                     ?? throw new InvalidOperationException();
                }
                else
                {
                    throw new InvalidOperationException("Failed to parse CharacterStats from response due to stats not being in correct tags.");
                }

                match = Regex.Match(response, "<character_statistics>(.*?)</character_statistics>", RegexOptions.Singleline);
                CharacterTracker tracker;
                if (match.Success)
                {
                    tracker = JsonSerializer.Deserialize<CharacterTracker>(match.Groups[1].Value.RemoveThinkingBlock().ExtractJsonFromMarkdown(), options)
                              ?? throw new InvalidOperationException();
                }
                else
                {
                    throw new InvalidOperationException("Failed to parse CharacterTracker from response due to tracker not being in correct tags.");
                }

                match = Regex.Match(response, "<character_development>(.*?)</character_development>", RegexOptions.Singleline);
                CharacterDevelopmentTracker developmentTracker;
                if (match.Success)
                {
                    developmentTracker = JsonSerializer.Deserialize<CharacterDevelopmentTracker>(match.Groups[1].Value.RemoveThinkingBlock().ExtractJsonFromMarkdown(), options)
                                         ?? throw new InvalidOperationException();
                }
                else
                {
                    throw new InvalidOperationException("Failed to parse CharacterTracker from response due to tracker not being in correct tags.");
                }

                Match descriptionMatch = Regex.Match(response, "<character_description>(.*?)</character_description>", RegexOptions.Singleline);
                if (descriptionMatch.Success)
                {
                    var description = descriptionMatch.Groups[1].Value.RemoveThinkingBlock().ExtractJsonFromMarkdown();
                    if (string.IsNullOrEmpty(description))
                    {
                        throw new InvalidCastException("Failed to parse character description from response due to empty description or it not being in correct tags.");
                    }

                    return (characterStats, description, tracker, developmentTracker);
                }

                throw new InvalidCastException("Failed to parse description from response due to output not being in correct tags.");
            });

        (CharacterStats characterStats, string description, CharacterTracker tracker, CharacterDevelopmentTracker characterDevelopmentTracker) result =
            await agentKernel.SendRequestAsync(chatHistory,
                outputFunc,
                kernelBuilder.GetDefaultFunctionPromptExecutionSettings(),
                nameof(CharacterCrafter),
                kernelWithKg,
                cancellationToken);
        return new CharacterContext
        {
            CharacterId = Guid.NewGuid(),
            CharacterState = result.characterStats,
            Description = result.description,
            CharacterTracker = result.tracker,
            DevelopmentTracker = result.characterDevelopmentTracker,
            Name = result.characterStats.CharacterIdentity.FullName!,
            SequenceNumber = 1
        };
    }

    private async static Task<string> BuildInstruction(TrackerStructure structure)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true
        };

        var prompt = await PromptBuilder.BuildPromptAsync("CharacterCrafterPrompt.md");
        return prompt.Replace("{{character_tracker_format}}", JsonSerializer.Serialize(GetSystemPrompt(structure), options))
            .Replace("{{character_tracker}}", JsonSerializer.Serialize(GetOutputJson(structure), options))
            .Replace("{{character_development_format}}", JsonSerializer.Serialize(TrackerExtensions.ConvertToSystemJson(structure.CharacterDevelopment!), options))
            .Replace("{{character_development}}", JsonSerializer.Serialize(TrackerExtensions.ConvertToOutputJson(structure.CharacterDevelopment!), options));
    }

    private static Dictionary<string, object> GetOutputJson(TrackerStructure structure)
    {
        var charDict = TrackerExtensions.ConvertToOutputJson(structure.Characters);

        return charDict;
    }

    private static Dictionary<string, object> GetSystemPrompt(TrackerStructure structure)
    {
        var charDict = TrackerExtensions.ConvertToSystemJson(structure.Characters);
        return charDict;
    }
}