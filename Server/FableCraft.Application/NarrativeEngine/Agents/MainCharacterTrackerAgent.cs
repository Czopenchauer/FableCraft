using System.Text;
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

internal sealed class MainCharacterTrackerAgent(IAgentKernel agentKernel, IDbContextFactory<ApplicationDbContext> dbContextFactory, KernelBuilderFactory kernelBuilderFactory)
{
    public async Task<CharacterTracker> Invoke(GenerationContext context, Tracker storyTrackerResult, CancellationToken cancellationToken)
    {
        IKernelBuilder kernelBuilder = kernelBuilderFactory.Create(context.LlmPreset);
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var trackerStructure = await dbContext
            .Adventures
            .Select(x => new { x.Id, x.TrackerStructure, x.AdventureStartTime })
            .SingleAsync(x => x.Id == context.AdventureId, cancellationToken);

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
        var stringBuilder = new StringBuilder();

        string instruction;
        chatHistory.AddUserMessage($"""
                                    <story_tracker>
                                    {JsonSerializer.Serialize(storyTrackerResult, options)}
                                    </story_tracker>
                                    """);
        
        stringBuilder.AppendLine($"""
                                  <main_character>
                                  {context.MainCharacter.Name}
                                  {context.MainCharacter.Description}
                                  </main_character>
                                  """);
        if ((context.SceneContext?.Length ?? 0) == 0)
        {
            stringBuilder.AppendLine($"""
                                         <scene_content>
                                         {context.NewScene?.Scene}
                                         </scene_content>
                                      """);
            chatHistory.AddUserMessage(stringBuilder.ToString());
            instruction = "It's the first scene of the adventure. Initialize the tracker based on the scene content and characters description.";
            chatHistory.AddUserMessage(instruction);
        }
        else
        {
            stringBuilder.AppendLine($"""
                                      <previous_trackers>
                                      {string.Join("\n\n", (context.SceneContext ?? Array.Empty<SceneContext>()).OrderByDescending(x => x.SequenceNumber).Where(x => x.Metadata.Tracker != null).Take(1)
                                          .Select(s => $"""
                                                        {JsonSerializer.Serialize(s.Metadata.Tracker, options)}
                                                        """))}
                                      </previous_trackers>
                                      <last_scenes>
                                      {string.Join("\n", (context.SceneContext ?? Array.Empty<SceneContext>())
                                          .OrderByDescending(x => x.SequenceNumber)
                                          .Take(5)
                                          .Select(x =>
                                              $"""
                                               SCENE NUMBER: {x.SequenceNumber}
                                               {x.SceneContent}
                                               {x.PlayerChoice}
                                               """))}
                                      </last_scenes>
                                      """);
            instruction = "Update the tracker based on the new scene content and previous tracker state.";
        }

        chatHistory.AddUserMessage(stringBuilder.ToString());
        chatHistory.AddUserMessage(instruction);
        var outputFunc = new Func<string, CharacterTracker>(response =>
        {
            Match match = Regex.Match(response, "<tracker>(.*?)</tracker>", RegexOptions.Singleline);
            if (match.Success)
            {
                return JsonSerializer.Deserialize<CharacterTracker>(match.Groups[1].Value.RemoveThinkingBlock().ExtractJsonFromMarkdown(), options)
                       ?? throw new InvalidOperationException();
            }

            throw new InvalidCastException("Failed to parse Tracker from response due to output not being in correct tags.");
        });
        PromptExecutionSettings promptExecutionSettings = kernelBuilder.GetDefaultPromptExecutionSettings();
        promptExecutionSettings.FunctionChoiceBehavior = FunctionChoiceBehavior.None();
        Kernel kernel = kernelBuilder.Create().Build();
        return await agentKernel.SendRequestAsync(chatHistory, outputFunc, promptExecutionSettings, nameof(MainCharacterTrackerAgent), kernel, cancellationToken);
    }

    private async static Task<string> BuildInstruction(TrackerStructure trackerStructure)
    {
        var promptTemplate = await PromptBuilder.BuildPromptAsync("MainCharacterTrackerPrompt.md");
        var trackerPrompt = GetSystemPrompt(trackerStructure);

        var serializeOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true
        };
        return promptTemplate
            .Replace("{{main_character_prompt}}", JsonSerializer.Serialize(trackerPrompt, serializeOptions))
            .Replace("{{json_output_format}}", JsonSerializer.Serialize(GetOutputJson(trackerStructure), serializeOptions));
    }

    private static Dictionary<string, object> GetOutputJson(TrackerStructure structure)
    {
        var mainCharStats = TrackerExtensions.ConvertToOutputJson(structure.MainCharacter);
        return mainCharStats;
    }

    private static Dictionary<string, object> GetSystemPrompt(TrackerStructure structure)
    {
        var mainCharStats = TrackerExtensions.ConvertToSystemJson(structure.MainCharacter);
        return mainCharStats;
    }
}