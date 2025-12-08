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

using Serilog;

using IKernelBuilder = FableCraft.Infrastructure.Llm.IKernelBuilder;

namespace FableCraft.Application.NarrativeEngine.Agents;

internal sealed class NarrativeDirectorAgent(IAgentKernel agentKernel, KernelBuilderFactory kernelBuilderFactory, IRagSearch ragSearch, ILogger logger) : IProcessor
{
    private const int SceneContextCount = 20;

    public async Task Invoke(GenerationContext context, CancellationToken cancellationToken)
    {
        IKernelBuilder kernelBuilder = kernelBuilderFactory.Create(context.ComplexPreset);
        var chatHistory = new ChatHistory();
        var systemPrompt = await BuildInstruction();
        chatHistory.AddSystemMessage(systemPrompt);
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true
        };

        SceneContext? lastScene = context.SceneContext.MaxBy(x => x.SequenceNumber);

        if (lastScene != null)
        {
            var narrativeDirection = JsonSerializer.Serialize(lastScene.Metadata.NarrativeMetadata, options);
            chatHistory.AddUserMessage($"""
                                        <last_scene_narrative_direction>
                                        {narrativeDirection}
                                        </last_scene_narrative_direction>
                                        """);
        }

        chatHistory.AddUserMessage($"""
                                    <main_character>
                                    {context.MainCharacter.Name}
                                    {context.MainCharacter.Description}
                                    </main_character>
                                    """);

        var lastTracker = context.SceneContext.Where(x => x.Metadata.Tracker != null).OrderByDescending(x => x.SequenceNumber).FirstOrDefault()?.Metadata.Tracker;
        if (lastTracker != null)
        {
            chatHistory.AddUserMessage($"""
                                        <current_scene_tracker>
                                        {JsonSerializer.Serialize(lastTracker, options)}
                                        </current_scene_tracker>
                                        """);
        }

        if (context.SceneContext.Length > 0)
        {
            chatHistory.AddUserMessage($"""
                                        <story_summary>
                                        {context.Summary}
                                        </story_summary>
                                        """);

            chatHistory.AddUserMessage($"""
                                        <extra_context>
                                        {JsonSerializer.Serialize(context.ContextGathered, options)}
                                        </extra_context>
                                        """);

            chatHistory.AddUserMessage($"""
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

        chatHistory.AddUserMessage($"""
                                    <player_action>
                                    {context.PlayerAction}
                                    </player_action>
                                    """);

        Microsoft.SemanticKernel.IKernelBuilder kernel = kernelBuilder.Create();
        var kgPlugin = new KnowledgeGraphPlugin(ragSearch, new CallerContext(GetType(), context.AdventureId));
        kernel.Plugins.Add(KernelPluginFactory.CreateFromObject(kgPlugin));
        var characterPlugin = new CharacterPlugin(agentKernel, logger, kernelBuilderFactory, ragSearch);
        await characterPlugin.Setup(context);
        kernel.Plugins.Add(KernelPluginFactory.CreateFromObject(characterPlugin));
        Kernel kernelWithKg = kernel.Build();
        var outputFunc = new Func<string, NarrativeDirectorOutput>(response =>
        {
            Match match = Regex.Match(response, "<narrative_scene_directive>(.*?)</narrative_scene_directive>", RegexOptions.Singleline);
            if (match.Success)
            {
                return JsonSerializer.Deserialize<NarrativeDirectorOutput>(match.Groups[1].Value.RemoveThinkingBlock().ExtractJsonFromMarkdown(), options)
                       ?? throw new InvalidOperationException();
            }

            throw new InvalidCastException("Failed to parse NarrativeDirectorOutput from response due to output not being in correct tags.");
        });
        NarrativeDirectorOutput narrativeOutput = await agentKernel.SendRequestAsync(chatHistory,
            outputFunc,
            kernelBuilder.GetDefaultFunctionPromptExecutionSettings(),
            nameof(NarrativeDirectorAgent),
            kernelWithKg,
            cancellationToken);
        context.NewNarrativeDirection = narrativeOutput;
        context.GenerationProcessStep = GenerationProcessStep.NarrativeDirectionFinished;
    }

    private static Task<string> BuildInstruction()
    {
        return PromptBuilder.BuildPromptAsync("NarrativePrompt.md");
    }
}