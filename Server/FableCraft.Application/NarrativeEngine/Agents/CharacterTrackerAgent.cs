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

internal sealed class CharacterTrackerAgent(
    IAgentKernel agentKernel,
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    KernelBuilderFactory kernelBuilderFactory,
    IRagSearch ragSearch)
{
    public async Task<CharacterTracker> Invoke(
        GenerationContext generationContext,
        CharacterContext context,
        CancellationToken cancellationToken)
    {
        IKernelBuilder kernelBuilder = kernelBuilderFactory.Create(generationContext.LlmPreset);
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

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
                      <previous_statistics>
                      {JsonSerializer.Serialize(context.CharacterTracker, options)}
                      </previous_statistics>

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
                      {generationContext.NewScene?.Scene ?? generationContext.PlayerAction}
                      </current_scene>
                      """;
        chatHistory.AddUserMessage(prompt);
        var instruction = "Update the tracker statistics based on the new scene content and previous tracker state.";
        chatHistory.AddUserMessage(instruction);

        var outputFunc = new Func<string, CharacterTracker>(response =>
        {
            Match match = Regex.Match(response, "<character_tracker>(.*?)</character_tracker>", RegexOptions.Singleline);
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

            return tracker;
        });

        Microsoft.SemanticKernel.IKernelBuilder kernel = kernelBuilder.Create();
        var kgPlugin = new KnowledgeGraphPlugin(ragSearch, new CallerContext(GetType(), generationContext.AdventureId));
        kernel.Plugins.Add(KernelPluginFactory.CreateFromObject(kgPlugin));
        Kernel kernelWithKg = kernel.Build();
        PromptExecutionSettings promptExecutionSettings = kernelBuilder.GetDefaultFunctionPromptExecutionSettings();
        promptExecutionSettings.FunctionChoiceBehavior = FunctionChoiceBehavior.None();
        CharacterTracker result = await agentKernel.SendRequestAsync(chatHistory,
            outputFunc,
            promptExecutionSettings,
            nameof(CharacterTrackerAgent),
            kernelWithKg,
            cancellationToken);
        return result;
    }

    private async static Task<string> BuildInstruction(TrackerStructure structure, string characterName)
    {
        var promptPath = Path.Combine(
            AppContext.BaseDirectory,
            "NarrativeEngine",
            "Agents",
            "Prompts",
            "CharacterTrackerAgentPrompt.md"
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

            foreach (FieldDefinition field in fields)
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

            foreach (FieldDefinition field in fields)
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
