using System.Text.Json;
using System.Text.RegularExpressions;

using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Infrastructure.Llm;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel.ChatCompletion;

namespace FableCraft.Application.NarrativeEngine.Agents;

internal sealed class TrackerAgent(IAgentKernel agentKernel, ApplicationDbContext dbContext)
{
    public async Task<Tracker> Invoke(NarrativeContext context, GeneratedScene scene, CancellationToken cancellationToken)
    {
        var trackerStructure = await dbContext
            .Adventures
            .Select(x => new { x.Id, x.TrackerStructure })
            .SingleAsync(x => x.Id == context.AdventureId, cancellationToken);

        var chatHistory = new ChatHistory();
        var systemPrompt = await BuildInstruction(trackerStructure.TrackerStructure);
        chatHistory.AddSystemMessage(systemPrompt);
        if (context.SceneContext.Length == 0)
        {
            chatHistory.AddUserMessage(context.CommonContext);
            var prompt = $"""
                         <scene_content>
                         {scene}
                         </scene_content>
                      """;
            chatHistory.AddUserMessage(prompt);
            var instruction = "It's the first scene of the adventure. Initialize the tracker based on the scene content and characters description.";
            chatHistory.AddUserMessage(instruction);
        }
        else
        {
            
        }

        var outputFunc = new Func<string, Tracker>(response =>
        {
            var match = Regex.Match(response, "<tracker>(.*?)</tracker>", RegexOptions.Singleline);
            if (match.Success)
            {
                return JsonSerializer.Deserialize<Tracker>(match.Groups[1].Value) ?? throw new InvalidOperationException();
            }

            throw new InvalidCastException("Failed to parse Tracker from response due to output not being in correct tags.");
        });

        return await agentKernel.SendRequestAsync(chatHistory, outputFunc, cancellationToken);
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

        return promptTemplate
            .Replace("{{story_prompt}}", JsonSerializer.Serialize(trackerPrompt[nameof(Tracker.Story)]))
            .Replace("{{main_character_prompt}}", JsonSerializer.Serialize(trackerPrompt[nameof(Tracker.MainCharacter)]))
            .Replace("{{secondary_character_prompt}}", JsonSerializer.Serialize(trackerPrompt[nameof(Tracker.Characters)]))
            .Replace("{{json_output_format}}", JsonSerializer.Serialize(GetOutputJson(trackerStructure)));
    }

    private static Dictionary<string, object> GetOutputJson(TrackerStructure structure)
    {
        var dictionary = new Dictionary<string, object>();
        var story = ConvertFieldsToDict(structure.Story);
        dictionary.Add(nameof(Tracker.Story), story);

        var characterPresent = ConvertFieldsToDict(structure.CharactersPresent);
        dictionary.Add(nameof(Tracker.CharactersPresent), characterPresent);

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
        var dictionary = new Dictionary<string, object>();
        var story = ConvertFieldsToDict(structure.Story);
        dictionary.Add(nameof(Tracker.Story), story);

        var characterPresent = ConvertFieldsToDict(structure.CharactersPresent);
        dictionary.Add(nameof(Tracker.CharactersPresent), characterPresent);

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