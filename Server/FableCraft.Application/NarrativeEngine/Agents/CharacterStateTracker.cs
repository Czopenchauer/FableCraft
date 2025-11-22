using System.Text.Json;
using System.Text.RegularExpressions;

using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Infrastructure.Llm;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

using IKernelBuilder = FableCraft.Infrastructure.Llm.IKernelBuilder;

namespace FableCraft.Application.NarrativeEngine.Agents;

internal sealed class CharacterStateTracker(IAgentKernel agentKernel, ApplicationDbContext dbContext, IKernelBuilder kernelBuilder)
{
    public async Task<(CharacterTracker tracker, CharacterState state)> Invoke(Guid adventureId, CharacterContext context, GeneratedScene scene, CancellationToken cancellationToken)
    {
        var trackerStructure = await dbContext
            .Adventures
            .Select(x => new { x.Id, x.TrackerStructure })
            .SingleAsync(x => x.Id == adventureId, cancellationToken);

        var chatHistory = new ChatHistory();
        var systemPrompt = await BuildInstruction(trackerStructure.TrackerStructure, context.Name);
        chatHistory.AddSystemMessage(systemPrompt);
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true
        };

//         var prompt = $"""
//                          <previous_trackers>
//                          {string.Join("\n\n", context.SceneContext.TakeLast(5).Select(s => $"""
//                                                                                             CONTENT:
//                                                                                             {s.SceneContent}
//                                                                                             TRACKER:
//                                                                                             {JsonSerializer.Serialize(s.Metadata, options)}
//                                                                                             """))}
//                          </previous_trackers>
//
//                          <scene_content>
//                          {scene}
//                          </scene_content>
//                       """;
//         chatHistory.AddUserMessage(prompt);
        var instruction = "Update the tracker based on the new scene content and previous tracker state.";
        chatHistory.AddUserMessage(instruction);

        var outputFunc = new Func<string, (CharacterTracker tracker, CharacterState state)>(response =>
        {
            var match = Regex.Match(response, "<statistics>(.*?)</statistics>", RegexOptions.Singleline);
            CharacterTracker tracker;
            if (match.Success)
            {
                tracker = JsonSerializer.Deserialize<CharacterTracker>(match.Groups[1].Value.RemoveThinkingBlock().ExtractJsonFromMarkdown(), options) ?? throw new InvalidOperationException();
            }
            else
            {
                throw new InvalidOperationException("Failed to parse Tracker from response due to output not being in correct tags.");
            }

            match = Regex.Match(response, "<character_state>(.*?)</character_state>", RegexOptions.Singleline);
            CharacterState state;
            if (match.Success)
            {
                state = JsonSerializer.Deserialize<CharacterState>(match.Groups[1].Value.RemoveThinkingBlock().ExtractJsonFromMarkdown(), options) ?? throw new InvalidOperationException();
            }
            else
            {
                throw new InvalidOperationException("Failed to parse CharacterState from response due to output not being in correct tags.");
            }

            return (tracker, state);
        });
        var promptExecutionSettings = kernelBuilder.GetDefaultPromptExecutionSettings();
        promptExecutionSettings.FunctionChoiceBehavior = FunctionChoiceBehavior.None();
        return await agentKernel.SendRequestAsync(chatHistory, outputFunc, cancellationToken, promptExecutionSettings: promptExecutionSettings);
    }

    private async static Task<string> BuildInstruction(TrackerStructure structure, string characterName)
    {
        var promptPath = Path.Combine(
            AppContext.BaseDirectory,
            "NarrativeEngine",
            "Agents",
            "Prompts",
            "CharacterTrackerPrompt.md"
        );

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true
        };

        var prompt = await File.ReadAllTextAsync(promptPath);
        return prompt.Replace("{{character_tracker_format}}", JsonSerializer.Serialize(GetSystemPrompt(structure), options))
            .Replace("{{character_tracker}}", JsonSerializer.Serialize(GetOutputJson(structure), options))
            .Replace("[Target Character]", characterName);
    }

    private static Dictionary<string, object> GetOutputJson(TrackerStructure structure)
    {
        var charDict = ConvertFieldsToDict(structure.Characters);

        return charDict;

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
                        Value = GetDefaultValue(field)
                    };
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
        var charDict = ConvertFieldsToDict(structure.Characters);
        return charDict;

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