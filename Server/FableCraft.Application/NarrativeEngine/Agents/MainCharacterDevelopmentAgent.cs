using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Infrastructure.Llm;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;

using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

using IKernelBuilder = FableCraft.Infrastructure.Llm.IKernelBuilder;

namespace FableCraft.Application.NarrativeEngine.Agents;

internal sealed class MainCharacterDevelopmentAgent(
    IAgentKernel agentKernel,
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    KernelBuilderFactory kernelBuilderFactory)
{
    public async Task<CharacterDevelopmentTracker?> Invoke(GenerationContext context,
        Tracker storyTrackerResult,
        CancellationToken cancellationToken)
    {
        IKernelBuilder kernelBuilder = kernelBuilderFactory.Create(context.ComplexPreset);
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var trackerStructure = await dbContext
            .Adventures
            .Select(x => new { x.Id, x.TrackerStructure })
            .SingleAsync(x => x.Id == context.AdventureId, cancellationToken);

        // If no main character development structure is defined, return null
        if (trackerStructure.TrackerStructure.MainCharacterDevelopment == null ||
            trackerStructure.TrackerStructure.MainCharacterDevelopment.Length == 0)
        {
            return null;
        }

        var chatHistory = new ChatHistory();
        var systemPrompt = await BuildInstruction(trackerStructure.TrackerStructure);
        chatHistory.AddSystemMessage(systemPrompt);
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        var previousTracker = context.SceneContext
            ?.OrderByDescending(x => x.SequenceNumber)
            .FirstOrDefault()?.Metadata.Tracker;

        chatHistory.AddUserMessage($"""
                                    <story_tracker>
                                    {JsonSerializer.Serialize(storyTrackerResult, options)}
                                    </story_tracker>
                                    """);
        var prompt = $"""
                      <main_character_tracker>
                      {JsonSerializer.Serialize(previousTracker?.MainCharacter, options)}
                      <main_character_tracker>
                      <previous_development>
                      {JsonSerializer.Serialize(previousTracker?.MainCharacterDevelopment, options)}
                      </previous_development>

                      <recent_scenes>
                      {string.Join("\n\n---\n\n", (context.SceneContext ?? Array.Empty<SceneContext>())
                          .OrderByDescending(x => x.SequenceNumber)
                          .TakeLast(3)
                          .Select(s => $"""
                                        SCENE NUMBER: {s.SequenceNumber}
                                        {s.SceneContent}
                                        {s.PlayerChoice}
                                        """))}
                      </recent_scenes>

                      <current_scene>
                      {context.NewScene?.Scene}
                      </current_scene>
                      """;
        chatHistory.AddUserMessage(prompt);
        var instruction = "Update the main character's development tracker based on the new scene content and previous development state.";
        chatHistory.AddUserMessage(instruction);

        var outputFunc = new Func<string, CharacterDevelopmentTracker>(response =>
        {
            Match match = Regex.Match(response, "<tracker>(.*?)</tracker>", RegexOptions.Singleline);
            CharacterDevelopmentTracker tracker;
            if (match.Success)
            {
                tracker = JsonSerializer.Deserialize<CharacterDevelopmentTracker>(match.Groups[1].Value.RemoveThinkingBlock().ExtractJsonFromMarkdown(), options)
                          ?? throw new InvalidOperationException();
            }
            else
            {
                throw new InvalidOperationException("Failed to parse MainCharacterDevelopmentTracker from response due to output not being in correct tags.");
            }

            return tracker;
        });

        PromptExecutionSettings promptExecutionSettings = kernelBuilder.GetDefaultPromptExecutionSettings();
        promptExecutionSettings.FunctionChoiceBehavior = FunctionChoiceBehavior.None();
        Kernel kernel = kernelBuilder.Create().Build();
        CharacterDevelopmentTracker result = await agentKernel.SendRequestAsync(chatHistory,
            outputFunc,
            promptExecutionSettings,
            nameof(MainCharacterDevelopmentAgent),
            kernel,
            cancellationToken);
        return result;
    }

    private async static Task<string> BuildInstruction(TrackerStructure structure)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true
        };

        var prompt = await PromptBuilder.BuildPromptAsync("MainCharacterDevelopmentAgentPrompt.md");
        return prompt.Replace("{{main_character_prompt}}", JsonSerializer.Serialize(GetSystemPrompt(structure), options))
            .Replace("{{json_output_format}}", JsonSerializer.Serialize(GetOutputJson(structure), options));
    }

    private static Dictionary<string, object> GetOutputJson(TrackerStructure structure)
    {
        if (structure.MainCharacterDevelopment == null || structure.MainCharacterDevelopment.Length == 0)
        {
            return new Dictionary<string, object>();
        }

        var devDict = TrackerExtensions.ConvertToOutputJson(structure.MainCharacterDevelopment);
        return devDict;
    }

    private static Dictionary<string, object> GetSystemPrompt(TrackerStructure structure)
    {
        if (structure.MainCharacterDevelopment == null || structure.MainCharacterDevelopment.Length == 0)
        {
            return new Dictionary<string, object>();
        }

        var devDict = TrackerExtensions.ConvertToSystemJson(structure.MainCharacterDevelopment);
        return devDict;
    }
}
