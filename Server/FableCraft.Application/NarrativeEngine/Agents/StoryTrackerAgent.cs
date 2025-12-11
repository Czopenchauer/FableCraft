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

internal sealed class StoryTrackerAgent(IAgentKernel agentKernel, IDbContextFactory<ApplicationDbContext> dbContextFactory, KernelBuilderFactory kernelBuilderFactory)
{
    public async Task<Tracker> Invoke(GenerationContext context, CancellationToken cancellationToken)
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
        chatHistory.AddUserMessage($"""
                                     <main_character>
                                     {context.MainCharacter.Name}
                                     {context.LatestSceneContext?.Metadata.MainCharacterDescription ?? context.MainCharacter.Description}
                                     </main_character>
                                     """);

        chatHistory.AddUserMessage($"""
                                     <characters>
                                     {string.Join("\n\n", context.Characters.Select(c => $"""
                                                                                          <character>
                                                                                          Name: {c.Name}
                                                                                          {c.Description}
                                                                                          </character>
                                                                                          """))}
                                     </characters>
                                     """);

        chatHistory.AddUserMessage($"""
                                     <current_scene>
                                     {context.NewScene!.Scene}
                                     </current_scene>
                                     """);

        if (context.NewCharacters?.Length > 0)
        {
            chatHistory.AddUserMessage($"""
                                         <new_characters>
                                         <character>
                                         {string.Join("\n\n", context.NewCharacters.Select(c => $"""
                                                                                                 {c.Name}
                                                                                                 {c.Description}
                                                                                                 """))}
                                         </character>
                                         </new_characters>
                                         """);
        }

        if (context.SceneContext.Length == 1)
        {
            chatHistory.AddUserMessage($"""
                                         <adventure_start_time>
                                         {trackerStructure.AdventureStartTime}
                                         </adventure_start_time>
                                         """);
        }

        if (context.NewLocations?.Length > 0)
        {
            chatHistory.AddUserMessage($"""
                                         <new_locations>
                                         {context.NewLocations}
                                         </new_locations>
                                         """);
        }

        string instruction;
        if ((context.SceneContext?.Length ?? 0) == 0)
        {
            chatHistory.AddUserMessage($"""
                                         <scene_content>
                                         {context.NewScene.Scene}
                                         </scene_content>
                                         """);
            instruction = "It's the first scene of the adventure. Initialize the tracker based on the scene content and characters description.";
            chatHistory.AddUserMessage(instruction);
        }
        else
        {
            chatHistory.AddUserMessage($"""
                                         <previous_trackers>
                                         {string.Join("\n\n", (context.SceneContext ?? Array.Empty<SceneContext>()).OrderByDescending(x => x.SequenceNumber).Where(x => x.Metadata.Tracker != null).Take(1)
                                             .Select(s => $"""
                                                           {JsonSerializer.Serialize(s.Metadata.Tracker?.Story, options)}
                                                           """))}
                                         </previous_trackers>
                                         """);

            chatHistory.AddUserMessage($"""
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
            chatHistory.AddUserMessage(instruction);
        }
        var outputFunc = new Func<string, Tracker>(response =>
        {
            Match match = Regex.Match(response, "<tracker>(.*?)</tracker>", RegexOptions.Singleline);
            if (match.Success)
            {
                return JsonSerializer.Deserialize<Tracker>(match.Groups[1].Value.RemoveThinkingBlock().ExtractJsonFromMarkdown(), options)
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
        var promptTemplate = await PromptBuilder.BuildPromptAsync("StoryTrackerPrompt.md");
        var trackerPrompt = GetSystemPrompt(trackerStructure);

        var serializeOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true
        };
        return promptTemplate
            .Replace("{{field_update_logic}}", JsonSerializer.Serialize(trackerPrompt[nameof(Tracker.Story)], serializeOptions))
            .Replace("{{json_output_format}}", JsonSerializer.Serialize(GetOutputJson(trackerStructure), serializeOptions));
    }

    private static Dictionary<string, object> GetOutputJson(TrackerStructure structure)
    {
        var dictionary = new Dictionary<string, object>();
        var story = TrackerExtensions.ConvertToOutputJson(structure.Story);
        dictionary.Add(nameof(Tracker.Story), story);
        dictionary.Add(nameof(Tracker.CharactersPresent), structure.CharactersPresent.DefaultValue ?? Array.Empty<string>());

        return dictionary;
    }

    private static Dictionary<string, object> GetSystemPrompt(TrackerStructure structure)
    {
        var dictionary = new Dictionary<string, object>();
        var story = TrackerExtensions.ConvertToSystemJson(structure.Story);
        dictionary.Add(nameof(Tracker.Story), story);

        dictionary.Add(nameof(Tracker.CharactersPresent),
            new
            {
                structure.CharactersPresent.Prompt,
                structure.CharactersPresent.DefaultValue,
                structure.CharactersPresent.ExampleValues
            });

        return dictionary;
    }
}