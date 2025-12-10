using System.Text.Json;
using System.Text.Json.Serialization;
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

internal sealed class CharacterDevelopmentAgent(
    IAgentKernel agentKernel,
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    KernelBuilderFactory kernelBuilderFactory,
    IRagSearch ragSearch)
{
    public async Task<CharacterDevelopmentTracker?> Invoke(GenerationContext generationContext,
        CharacterContext context,
        Tracker storyTrackerResult,
        CancellationToken cancellationToken)
    {
        IKernelBuilder kernelBuilder = kernelBuilderFactory.Create(generationContext.ComplexPreset);
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var trackerStructure = await dbContext
            .Adventures
            .Select(x => new { x.Id, x.TrackerStructure })
            .SingleAsync(x => x.Id == generationContext.AdventureId, cancellationToken);

        // If no development structure is defined, return null
        if (trackerStructure.TrackerStructure.CharacterDevelopment == null ||
            trackerStructure.TrackerStructure.CharacterDevelopment.Length == 0)
        {
            return null;
        }

        var chatHistory = new ChatHistory();
        var systemPrompt = await BuildInstruction(trackerStructure.TrackerStructure, context.Name);
        chatHistory.AddSystemMessage(systemPrompt);
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        chatHistory.AddUserMessage($"""
                                    <story_tracker>
                                    {JsonSerializer.Serialize(storyTrackerResult, options)}
                                    </story_tracker>
                                    """);
        var prompt = $"""
                      <previous_tracker>
                      {JsonSerializer.Serialize(context.CharacterTracker, options)}
                      </previous_tracker>
                      <previous_development>
                      {JsonSerializer.Serialize(context.DevelopmentTracker, options)}
                      </previous_development>
                      <previous_character_state>
                      {JsonSerializer.Serialize(context.CharacterState, options)}
                      </previous_character_state>

                      CRITICAL! These scenes are written from the perspective of the main character {generationContext.MainCharacter.Name}. Before updating the tracker, rewrite these scenes from the perspective of the character {context.Name}. Make sure to include ONLY their thoughts, feelings, knowledge, and reactions to the events happening in each scene.
                      <recent_scenes>
                      {string.Join("\n\n---\n\n", (generationContext.SceneContext ?? Array.Empty<SceneContext>())
                          .OrderByDescending(x => x.SequenceNumber)
                          .TakeLast(3)
                          .Select(s => $"""
                                        SCENE NUMBER: {s.SequenceNumber}
                                        {s.SceneContent}
                                        {s.PlayerChoice}
                                        """))}
                      </recent_scenes>

                      <current_scene>
                      {generationContext.NewScene!.Scene}
                      </current_scene>
                      """;
        chatHistory.AddUserMessage(prompt);
        var instruction = "Update the character development tracker based on the new scene content and previous development state.";
        chatHistory.AddUserMessage(instruction);

        var outputFunc = new Func<string, CharacterDevelopmentTracker>(response =>
        {
            Match match = Regex.Match(response, "<character_development>(.*?)</character_development>", RegexOptions.Singleline);
            CharacterDevelopmentTracker tracker;
            if (match.Success)
            {
                tracker = JsonSerializer.Deserialize<CharacterDevelopmentTracker>(match.Groups[1].Value.RemoveThinkingBlock().ExtractJsonFromMarkdown(), options)
                          ?? throw new InvalidOperationException();
            }
            else
            {
                throw new InvalidOperationException("Failed to parse CharacterDevelopmentTracker from response due to output not being in correct tags.");
            }

            return tracker;
        });

        Microsoft.SemanticKernel.IKernelBuilder kernel = kernelBuilder.Create();
        var kgPlugin = new KnowledgeGraphPlugin(ragSearch, new CallerContext(GetType(), generationContext.AdventureId));
        kernel.Plugins.Add(KernelPluginFactory.CreateFromObject(kgPlugin));
        Kernel kernelWithKg = kernel.Build();
        PromptExecutionSettings promptExecutionSettings = kernelBuilder.GetDefaultFunctionPromptExecutionSettings();
        promptExecutionSettings.FunctionChoiceBehavior = FunctionChoiceBehavior.None();
        CharacterDevelopmentTracker result = await agentKernel.SendRequestAsync(chatHistory,
            outputFunc,
            promptExecutionSettings,
            nameof(CharacterDevelopmentAgent),
            kernelWithKg,
            cancellationToken);
        return result;
    }

    private async static Task<string> BuildInstruction(TrackerStructure structure, string characterName)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true
        };

        var prompt = await PromptBuilder.BuildPromptAsync("CharacterDevelopmentAgentPrompt.md");
        return prompt.Replace("{{character_development_structure}}", JsonSerializer.Serialize(GetSystemPrompt(structure), options))
            .Replace("{{character_development}}", JsonSerializer.Serialize(GetOutputJson(structure), options))
            .Replace("{CHARACTER_NAME}", characterName);
    }

    private static Dictionary<string, object> GetOutputJson(TrackerStructure structure)
    {
        if (structure.CharacterDevelopment == null || structure.CharacterDevelopment.Length == 0)
        {
            return new Dictionary<string, object>();
        }

        var devDict = TrackerExtensions.ConvertToOutputJson(structure.CharacterDevelopment);
        return devDict;
    }

    private static Dictionary<string, object> GetSystemPrompt(TrackerStructure structure)
    {
        if (structure.CharacterDevelopment == null || structure.CharacterDevelopment.Length == 0)
        {
            return new Dictionary<string, object>();
        }

        var devDict = TrackerExtensions.ConvertToSystemJson(structure.CharacterDevelopment);
        return devDict;
    }
}
