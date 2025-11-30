using System.Text.Json;
using System.Text.RegularExpressions;

using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Infrastructure.Llm;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;

using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

using IKernelBuilder = FableCraft.Infrastructure.Llm.IKernelBuilder;

namespace FableCraft.Application.NarrativeEngine.Agents;

internal sealed class TrackerAgent(IAgentKernel agentKernel, ApplicationDbContext dbContext, IKernelBuilder kernelBuilder)
{
    public async Task<Tracker> Invoke(GenerationContext context, CancellationToken cancellationToken)
    {
        var trackerStructure = await dbContext
            .Adventures
            .Select(x => new { x.Id, x.TrackerStructure })
            .SingleAsync(x => x.Id == context.AdventureId, cancellationToken);

        var chatHistory = new ChatHistory();
        var systemPrompt = await BuildInstruction(trackerStructure.TrackerStructure);
        chatHistory.AddSystemMessage(systemPrompt);
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true
        };
        var stringBuilder = new System.Text.StringBuilder();
        stringBuilder.AppendLine($"""
                                  <main_character>
                                  {context.MainCharacter.Name}
                                  {context.MainCharacter.Description}
                                  </main_character>
                                  """);
        if (context.SceneContext.Length == 0)
        {
            stringBuilder.AppendLine($"""
                                         <scene_content>
                                         {context.NewScene!.Scene}
                                         </scene_content>
                                      """);
            chatHistory.AddUserMessage(stringBuilder.ToString());
            var instruction = "It's the first scene of the adventure. Initialize the tracker based on the scene content and characters description.";
            chatHistory.AddUserMessage(instruction);
        }
        else
        {
            stringBuilder.AppendLine($"""
                                      <previous_trackers>
                                      {string.Join("\n\n", context.SceneContext.OrderByDescending(x => x.SequenceNumber).Take(1)
                                          .Select(s => $"""
                                                        {JsonSerializer.Serialize(s.Metadata.Tracker, options)}
                                                        """))}
                                      </previous_trackers>
                                      <last_scenes>
                                      {string.Join("\n", context.SceneContext
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
            chatHistory.AddUserMessage(stringBuilder.ToString());
            var instruction = "Update the tracker based on the new scene content and previous tracker state.";
            chatHistory.AddUserMessage(instruction);
        }

        var outputFunc = new Func<string, Tracker>(response =>
        {
            var match = Regex.Match(response, "<tracker>(.*?)</tracker>", RegexOptions.Singleline);
            if (match.Success)
            {
                return JsonSerializer.Deserialize<Tracker>(match.Groups[1].Value.RemoveThinkingBlock().ExtractJsonFromMarkdown(), options)
                       ?? throw new InvalidOperationException();
            }

            throw new InvalidCastException("Failed to parse Tracker from response due to output not being in correct tags.");
        });
        var promptExecutionSettings = kernelBuilder.GetDefaultPromptExecutionSettings();
        promptExecutionSettings.FunctionChoiceBehavior = FunctionChoiceBehavior.None();
        return await agentKernel.SendRequestAsync(chatHistory, outputFunc, promptExecutionSettings, cancellationToken);
    }

    private async static Task<string> BuildInstruction(TrackerStructure trackerStructure)
    {
        var promptPath = Path.Combine(
            AppContext.BaseDirectory,
            "NarrativeEngine",
            "Agents",
            "Prompts",
            "TrackerPrompt.md"
        );

        var promptTemplate = await File.ReadAllTextAsync(promptPath);
        var trackerPrompt = GetSystemPrompt(trackerStructure);

        var serializeOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true
        };
        return promptTemplate
            .Replace("{{story_prompt}}", JsonSerializer.Serialize(trackerPrompt[nameof(Tracker.Story)], serializeOptions))
            .Replace("{{main_character_prompt}}", JsonSerializer.Serialize(trackerPrompt[nameof(Tracker.MainCharacter)], serializeOptions))
            .Replace("{{secondary_character_prompt}}", JsonSerializer.Serialize(trackerPrompt[nameof(Tracker.Characters)], serializeOptions))
            .Replace("{{json_output_format}}", JsonSerializer.Serialize(GetOutputJson(trackerStructure), serializeOptions));
    }

    private static Dictionary<string, object> GetOutputJson(TrackerStructure structure)
    {
        var dictionary = new Dictionary<string, object>();
        var story = ConvertFieldsToDict(structure.Story);
        dictionary.Add(nameof(Tracker.Story), story);

        // CharactersPresent is a single FieldDefinition of type Array, so output as array
        dictionary.Add(nameof(Tracker.CharactersPresent), structure.CharactersPresent.DefaultValue ?? Array.Empty<string>());

        var mainCharStats = ConvertFieldsToDict(structure.MainCharacter);
        dictionary.Add(nameof(Tracker.MainCharacter), mainCharStats);

        var charDict = ConvertFieldsToDict(structure.Characters);

        dictionary.Add(nameof(Tracker.Characters), new object[] { charDict });

        return dictionary;

        Dictionary<string, object> ConvertFieldsToDict(params FieldDefinition[] fields)
        {
            var dict = new Dictionary<string, object>();

            foreach (var field in fields)
            {
                if (field is { Type: FieldType.ForEachObject, HasNestedFields: true })
                {
                    dict[field.Name] = ConvertFieldsToDict(field.NestedFields);
                }
                else if (field.DefaultValue != null)
                {
                    dict[field.Name] = GetDefaultValue(field);
                }
            }

            return dict;

            object GetDefaultValue(FieldDefinition field)
            {
                return field.Type switch
                       {
                           FieldType.Array => new object[1],
                           FieldType.Object => new { },
                           FieldType.String => "",
                           _ => throw new NotSupportedException($"Field type {field.Type} is not supported.")
                       };
            }
        }
    }

    private static Dictionary<string, object> GetSystemPrompt(TrackerStructure structure)
    {
        var dictionary = new Dictionary<string, object>();
        var story = ConvertFieldsToDict(structure.Story);
        dictionary.Add(nameof(Tracker.Story), story);

        dictionary.Add(nameof(Tracker.CharactersPresent),
            new
            {
                structure.CharactersPresent.Prompt,
                structure.CharactersPresent.DefaultValue,
                structure.CharactersPresent.ExampleValues
            });

        var mainCharStats = ConvertFieldsToDict(structure.MainCharacter);
        dictionary.Add(nameof(Tracker.MainCharacter), mainCharStats);

        var charDict = ConvertFieldsToDict(structure.Characters);
        dictionary.Add(nameof(Tracker.Characters), new object[] { charDict });

        return dictionary;

        Dictionary<string, object> ConvertFieldsToDict(params FieldDefinition[] fields)
        {
            var dict = new Dictionary<string, object>();

            foreach (var field in fields)
            {
                if (field is { Type: FieldType.ForEachObject, HasNestedFields: true })
                {
                    dict[field.Name] = ConvertFieldsToDict(field.NestedFields);
                }
                else if (field.DefaultValue != null)
                {
                    dict[field.Name] = new
                    {
                        field.Prompt,
                        field.DefaultValue,
                        field.ExampleValues
                    };
                }
            }

            return dict;
        }
    }
}