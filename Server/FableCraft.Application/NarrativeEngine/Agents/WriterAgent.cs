using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Application.NarrativeEngine.Plugins;
using FableCraft.Application.NarrativeEngine.Workflow;
using FableCraft.Infrastructure.Clients;
using FableCraft.Infrastructure.Llm;

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

using Serilog;

using IKernelBuilder = FableCraft.Infrastructure.Llm.IKernelBuilder;

namespace FableCraft.Application.NarrativeEngine.Agents;

internal sealed class WriterAgent(
    IAgentKernel agentKernel,
    ILogger logger,
    IKernelBuilder kernelBuilder,
    IRagSearch ragSearch) : IProcessor
{
    private const int SceneContextCount = 15;

    public async Task Invoke(
        GenerationContext context,
        CancellationToken cancellationToken)
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
        var stringBuilder = new StringBuilder($"""
                                               <scene_direction>
                                               {JsonSerializer.Serialize(context.NewNarrativeDirection!.SceneDirection, options)}
                                               </scene_direction>

                                               <new_lore>
                                               {JsonSerializer.Serialize(context.NewLore, options)}
                                               </new_lore>

                                               <new_locations>
                                               {JsonSerializer.Serialize(context.NewLocations, options)}
                                               </new_locations>

                                               Newly created characters. Should be emulated as well as existing ones:
                                               <new_characters>
                                               {JsonSerializer.Serialize(context.NewCharacters, options)}
                                               </new_characters>
                                               """);
        stringBuilder.AppendLine($"""
                                  <main_character>
                                  {context.MainCharacter.Name}
                                  {context.MainCharacter.Description}
                                  </main_character>
                                  """);
        stringBuilder.AppendLine($"""
                                  <extra_context>
                                  {JsonSerializer.Serialize(context.ContextGathered, options)}
                                  </extra_context>
                                  """);
        if (context.SceneContext.Length > 0)
        {
            stringBuilder.AppendLine($"""
                                      <story_summary>
                                      {context.Summary}
                                      </story_summary>
                                      """);

            var lastScene = context.SceneContext.MaxBy(x => x.SequenceNumber);
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

        chatHistory.AddUserMessage(stringBuilder.ToString());

        var kernel = kernelBuilder.WithBase();
        var kgPlugin = new KnowledgeGraphPlugin(ragSearch, new CallerContext(GetType(), context.AdventureId));
        kernel.Plugins.Add(KernelPluginFactory.CreateFromObject(kgPlugin));
        var characterPlugin = new CharacterPlugin(agentKernel, logger, kernelBuilder, ragSearch);
        await characterPlugin.Setup(context);
        kernel.Plugins.Add(KernelPluginFactory.CreateFromObject(characterPlugin));
        Kernel kernelWithKg = kernel.Build();

        var outputFunc = new Func<string, GeneratedScene>(response =>
        {
            var match = Regex.Match(response, "<new_scene>(.*?)</new_scene>", RegexOptions.Singleline);
            if (match.Success)
            {
                return JsonSerializer.Deserialize<GeneratedScene>(match.Groups[1].Value.RemoveThinkingBlock().ExtractJsonFromMarkdown(), options)
                       ?? throw new InvalidOperationException();
            }

            throw new InvalidCastException("Failed to parse New Scene from response due to scene not being in correct tags.");
        });
        var newScene = await agentKernel.SendRequestAsync(
            chatHistory,
            outputFunc,
            kernelBuilder.GetDefaultFunctionPromptExecutionSettings(),
            cancellationToken,
            kernelWithKg);
        context.NewScene = newScene;
        context.GenerationProcessStep = GenerationProcessStep.SceneGenerationFinished;
    }

    private async static Task<string> BuildInstruction()
    {
        var promptPath = Path.Combine(
            AppContext.BaseDirectory,
            "NarrativeEngine",
            "Agents",
            "Prompts",
            "WriterPrompt.md"
        );

        return await File.ReadAllTextAsync(promptPath);
    }
}