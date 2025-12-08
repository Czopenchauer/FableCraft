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
    KernelBuilderFactory kernelBuilderFactory,
    IRagSearch ragSearch) : IProcessor
{
    private const int SceneContextCount = 15;

    public async Task Invoke(
        GenerationContext context,
        CancellationToken cancellationToken)
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
        
        chatHistory.AddUserMessage($"""
                                    <scene_direction>
                                    {JsonSerializer.Serialize(context.NewNarrativeDirection!.SceneDirection, options)}
                                    </scene_direction>
                                    
                                    <continuity_check>
                                    {JsonSerializer.Serialize(context.NewNarrativeDirection!.ContinuityCheck, options)}
                                    </continuity_check>
                                    
                                    <scene_metadata>
                                    {JsonSerializer.Serialize(context.NewNarrativeDirection!.SceneMetadata, options)}
                                    </scene_metadata>
                                    """);

        chatHistory.AddUserMessage($"""
                                    <new_lore>
                                    {JsonSerializer.Serialize(context.NewLore ?? Array.Empty<GeneratedLore>(), options)}
                                    </new_lore>
                                    """);

        chatHistory.AddUserMessage($"""
                                    <new_locations>
                                    {JsonSerializer.Serialize(context.NewLocations ?? Array.Empty<LocationGenerationResult>(), options)}
                                    </new_locations>
                                    """);

        chatHistory.AddUserMessage($"""
                                    These character will be created after the scene is generated so emulation is not required for them. You have to emulate them yourself.
                                    <new_characters_requests>
                                    {JsonSerializer.Serialize(context.NewNarrativeDirection.CreationRequests.Characters, options)}
                                    </new_characters_requests>
                                    """);

        chatHistory.AddUserMessage($"""
                                    <main_character>
                                    {context.MainCharacter.Name}
                                    {context.MainCharacter.Description}
                                    </main_character>
                                    """);

        chatHistory.AddUserMessage($"""
                                    <extra_context>
                                    {JsonSerializer.Serialize(context.ContextGathered, options)}
                                    </extra_context>
                                    """);

        if (context.SceneContext.Length > 0)
        {
            chatHistory.AddUserMessage($"""
                                        <story_summary>
                                        {context.Summary}
                                        </story_summary>
                                        """);

            SceneContext? lastScene = context.SceneContext.Where(x => x.Metadata.Tracker != null).OrderByDescending(x => x.SequenceNumber).FirstOrDefault();
            if (lastScene != null)
            {
                chatHistory.AddUserMessage($"""
                                            <current_scene_tracker>
                                            {JsonSerializer.Serialize(lastScene.Metadata.Tracker, options)}
                                            </current_scene_tracker>
                                            """);
            }

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

        var outputFunc = new Func<string, GeneratedScene>(response =>
        {
            Match match = Regex.Match(response, "<new_scene>(.*?)</new_scene>", RegexOptions.Singleline);
            if (match.Success)
            {
                return JsonSerializer.Deserialize<GeneratedScene>(match.Groups[1].Value.RemoveThinkingBlock().ExtractJsonFromMarkdown(), options)
                       ?? throw new InvalidOperationException();
            }

            throw new InvalidCastException("Failed to parse New Scene from response due to scene not being in correct tags.");
        });
        GeneratedScene newScene = await agentKernel.SendRequestAsync(
            chatHistory,
            outputFunc,
            kernelBuilder.GetDefaultFunctionPromptExecutionSettings(),
            nameof(WriterAgent),
            kernelWithKg,
            cancellationToken);
        context.NewScene = newScene;
        context.GenerationProcessStep = GenerationProcessStep.SceneGenerationFinished;
    }

    private static Task<string> BuildInstruction()
    {
        return PromptBuilder.BuildPromptAsync("WriterPrompt.md");
    }
}