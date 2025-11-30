using System.Text.Json;
using System.Text.RegularExpressions;

using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Application.NarrativeEngine.Plugins;
using FableCraft.Application.NarrativeEngine.Workflow;
using FableCraft.Infrastructure.Clients;
using FableCraft.Infrastructure.Llm;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

using IKernelBuilder = FableCraft.Infrastructure.Llm.IKernelBuilder;

namespace FableCraft.Application.NarrativeEngine.Agents;

internal sealed class NarrativeDirectorAgent(IAgentKernel agentKernel, IKernelBuilder kernelBuilder, IRagSearch ragSearch) : IProcessor
{
    private const int SceneContextCount = 20;

    public async Task Invoke(GenerationContext context, CancellationToken cancellationToken)
    {
        var chatHistory = new ChatHistory();
        var systemPrompt = await BuildInstruction();
        chatHistory.AddSystemMessage(systemPrompt);
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true
        };

        var lastScene = context.SceneContext.MaxBy(x => x.SequenceNumber);
        
        var stringBuilder = new System.Text.StringBuilder();
        if (lastScene != null)
        {
            var narrativeDirection = JsonSerializer.Serialize(lastScene.Metadata.NarrativeMetadata, options);
            var promptContext = $"""
                                 <last_scene_narrative_direction>
                                 {narrativeDirection}
                                 </last_scene_narrative_direction>
                                 """;

            stringBuilder.AppendLine(promptContext);
        }

        stringBuilder.AppendLine($"""
                                  <main_character>
                                  {context.MainCharacter.Name}
                                  {context.MainCharacter.Description}
                                  </main_character>
                                  """);
        if (context.SceneContext.Length > 0)
        {
            stringBuilder.AppendLine($"""
                                      <story_summary>
                                      {context.Summary}
                                      </story_summary>
                                      """);

            if (lastScene != null)
            {
                stringBuilder.AppendLine($"""
                                          <current_scene_tracker>
                                          {JsonSerializer.Serialize(lastScene.Metadata.Tracker, options)}
                                          </current_scene_tracker>
                                          """);
            }

            stringBuilder.AppendLine($"""
                                      <last_scenes>
                                      {string.Join("\n", context.SceneContext
                                          .OrderByDescending(x => x.SequenceNumber)
                                          .Take(SceneContextCount)
                                          .Select(x =>
                                              $"""
                                               SCENE NUMBER: {x.SequenceNumber}
                                               {x.SceneContent}
                                               {x.PlayerChoice}
                                               """))}
                                      </last_scenes>
                                      """);
        }

        stringBuilder.AppendLine($"""
                                 <extra_context>
                                 {JsonSerializer.Serialize(context.ContextGathered, options)}
                                 </extra_context>
                                 """);

        chatHistory.AddUserMessage(stringBuilder.ToString());
        chatHistory.AddUserMessage(context.PlayerAction);

        var kernel = kernelBuilder.WithBase();
        var kgPlugin = new KnowledgeGraphPlugin(ragSearch, new CallerContext(GetType(), context.AdventureId));
        kernel.Plugins.Add(KernelPluginFactory.CreateFromObject(kgPlugin));
        Kernel kernelWithKg = kernel.Build();
        var outputFunc = new Func<string, NarrativeDirectorOutput>(response =>
        {
            var match = Regex.Match(response, "<narrative_scene_directive>(.*?)</narrative_scene_directive>", RegexOptions.Singleline);
            if (match.Success)
            {
                return JsonSerializer.Deserialize<NarrativeDirectorOutput>(match.Groups[1].Value.RemoveThinkingBlock().ExtractJsonFromMarkdown(), options)
                       ?? throw new InvalidOperationException();
            }

            throw new InvalidCastException("Failed to parse NarrativeDirectorOutput from response due to output not being in correct tags.");
        });
        var narrativeOutput = await agentKernel.SendRequestAsync(chatHistory,
            outputFunc,
            promptExecutionSettings: kernelBuilder.GetDefaultFunctionPromptExecutionSettings(),
            cancellationToken,
            kernelWithKg);
        context.NewNarrativeDirection = narrativeOutput;
        context.GenerationProcessStep = GenerationProcessStep.NarrativeDirectionFinished;
    }

    private async static Task<string> BuildInstruction()
    {
        var promptPath = Path.Combine(
            AppContext.BaseDirectory,
            "NarrativeEngine",
            "Agents",
            "Prompts",
            "NarrativePrompt.md"
        );

        return await File.ReadAllTextAsync(promptPath);
    }
}