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

internal sealed class CharacterStateTracker(
    IAgentKernel agentKernel,
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    IKernelBuilder kernelBuilder,
    IRagSearch ragSearch)
{
    public async Task<CharacterContext> Invoke(
        GenerationContext generationContext,
        CharacterContext context,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var trackerStructure = await dbContext
            .Adventures
            .Select(x => new { x.Id, x.TrackerStructure })
            .SingleAsync(x => x.Id == generationContext.AdventureId, cancellationToken);

        var chatHistory = new ChatHistory();
        var systemPrompt = await BuildInstruction(trackerStructure.TrackerStructure, context.Name);
        chatHistory.AddSystemMessage(systemPrompt);
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true
        };

        var prompt = $"""
                      <previous_character_state>
                      {JsonSerializer.Serialize(context.CharacterState, options)}
                      </previous_character_state>

                      <previous_statistics>
                      {JsonSerializer.Serialize(context.CharacterTracker, options)}
                      </previous_statistics>

                      <recent_scenes>
                      {string.Join("\n\n---\n\n", (generationContext.SceneContext ?? Array.Empty<SceneContext>())
                          .OrderByDescending(x => x.SequenceNumber)
                          .TakeLast(3)
                          .Select(s => $"""
                                        Character on scene: {s.Metadata.Tracker.CharactersPresent}
                                        SCENE NUMBER: {s.SequenceNumber}
                                        {s.SceneContent}
                                        {s.PlayerChoice}
                                        """))}
                      </recent_scenes>

                      <current_scene>
                      {generationContext.NewScene?.Scene ?? generationContext.PlayerAction}
                      </current_scene>
                      """;
        chatHistory.AddUserMessage(prompt);
        var instruction = "Update the tracker based on the new scene content and previous tracker state.";
        chatHistory.AddUserMessage(instruction);

        var outputFunc = new Func<string, (CharacterTracker tracker, CharacterStats state)>(response =>
        {
            var match = Regex.Match(response, "<character_state>(.*?)</character_state>", RegexOptions.Singleline);
            CharacterStats state;
            if (match.Success)
            {
                state = JsonSerializer.Deserialize<CharacterStats>(match.Groups[1].Value.RemoveThinkingBlock().ExtractJsonFromMarkdown(), options)
                        ?? throw new InvalidOperationException();
            }
            else
            {
                throw new InvalidOperationException("Failed to parse CharacterState from response due to output not being in correct tags.");
            }

            match = Regex.Match(response, "<character_tracker>(.*?)</character_tracker>", RegexOptions.Singleline);
            CharacterTracker tracker;
            if (match.Success)
            {
                tracker = JsonSerializer.Deserialize<CharacterTracker>(match.Groups[1].Value.RemoveThinkingBlock().ExtractJsonFromMarkdown(), options)
                          ?? throw new InvalidOperationException();
            }
            else
            {
                throw new InvalidOperationException("Failed to parse Tracker from response due to output not being in correct tags.");
            }

            return (tracker, state);
        });

        var kernel = kernelBuilder.WithBase();
        var kgPlugin = new KnowledgeGraphPlugin(ragSearch, new CallerContext(GetType(), generationContext.AdventureId));
        kernel.Plugins.Add(KernelPluginFactory.CreateFromObject(kgPlugin));
        Kernel kernelWithKg = kernel.Build();
        var promptExecutionSettings = kernelBuilder.GetDefaultFunctionPromptExecutionSettings();
        promptExecutionSettings.FunctionChoiceBehavior = FunctionChoiceBehavior.None();
        var result = await agentKernel.SendRequestAsync(chatHistory,
            outputFunc,
            promptExecutionSettings: promptExecutionSettings,
            nameof(CharacterStateTracker),
            cancellationToken,
            kernelWithKg);
        return new CharacterContext
        {
            CharacterId = context.CharacterId,
            CharacterTracker = result.tracker,
            CharacterState = result.state,
            Name = context.Name,
            Description = context.Description,
            SequenceNumber = context.SequenceNumber + 1
        };
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
        return prompt.Replace("{{character_tracker_structure}}", JsonSerializer.Serialize(GetSystemPrompt(structure), options))
            .Replace("{{character_tracker}}", JsonSerializer.Serialize(GetOutputJson(structure), options))
            .Replace("{CHARACTER_NAME}", characterName);
    }

    private static Dictionary<string, object> GetOutputJson(TrackerStructure structure)
    {
        var charDict = ConvertFieldsToDict(structure.Characters);

        // Ensure Name field is always included since it's required by CharacterTracker
        if (!charDict.ContainsKey("Name"))
        {
            charDict["Name"] = "";
        }

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