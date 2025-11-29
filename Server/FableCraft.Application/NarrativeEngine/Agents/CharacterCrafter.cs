using System.Text.Json;
using System.Text.RegularExpressions;

using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Infrastructure.Llm;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace FableCraft.Application.NarrativeEngine.Agents;

internal sealed class CharacterCrafter(IAgentKernel agentKernel, ApplicationDbContext dbContext)
{
    public async Task<CharacterContext> Invoke(
        Kernel kernel,
        NarrativeContext context,
        CharacterRequest request,
        CancellationToken cancellationToken)
    {
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

        var contextPrompt = $"""
                             <character_creation_context>
                             {JsonSerializer.Serialize(request, options)}
                             </character_creation_context>
                             """;
        chatHistory.AddUserMessage(contextPrompt);
        var outputFunc = new Func<string, (CharacterStats characterStats, string description, CharacterTracker tracker)>(response =>
        {
            var match = Regex.Match(response, "<character>(.*?)</character>", RegexOptions.Singleline);
            CharacterStats? characterStats = null;
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

            var descriptionMatch = Regex.Match(response, "<character_description>(.*?)</character_description>", RegexOptions.Singleline);
            if (descriptionMatch.Success)
            {
                var description = descriptionMatch.Groups[1].Value.RemoveThinkingBlock().ExtractJsonFromMarkdown();
                if (string.IsNullOrEmpty(description))
                {
                    throw new InvalidCastException("Failed to parse character description from response due to empty description or it not being in correct tags.");
                }

                return (characterStats!, description, tracker);
            }

            throw new InvalidCastException("Failed to parse description from response due to output not being in correct tags.");
        });

        var result = await agentKernel.SendRequestAsync(chatHistory, outputFunc, cancellationToken, kernel: kernel);
        return new CharacterContext
        {
            CharacterState = result.characterStats,
            Description = result.description,
            CharacterTracker = result.tracker,
            Name = result.characterStats.CharacterIdentity.FullName!
        };
    }

    private async static Task<string> BuildInstruction(TrackerStructure structure)
    {
        var promptPath = Path.Combine(
            AppContext.BaseDirectory,
            "NarrativeEngine",
            "Agents",
            "Prompts",
            "CharacterCrafterPrompt.md"
        );
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true
        };

        var prompt = await File.ReadAllTextAsync(promptPath);
        return prompt.Replace("{{character_tracker_format}}", JsonSerializer.Serialize(GetSystemPrompt(structure), options))
            .Replace("{{character_tracker}}", JsonSerializer.Serialize(GetOutputJson(structure), options));
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