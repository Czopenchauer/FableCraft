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
        var systemPrompt = await PromptBuilder.BuildPromptAsync("WriterPrompt.md");
        var hasSceneContext = context.SceneContext.Length > 0;

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(systemPrompt);

        var contextPrompt = $"""
                             {PromptSections.MainCharacter(context.MainCharacter, context.LatestSceneContext?.Metadata.MainCharacterDescription)}

                             {PromptSections.MainCharacterTracker(context.SceneContext)}

                             {PromptSections.ExistingCharacters(context.Characters)}

                             {PromptSections.Context(context.ContextGathered)}

                             {(hasSceneContext ? PromptSections.StorySummary(context.Summary) : "")}

                             {(hasSceneContext ? PromptSections.CurrentStoryTracker(context.SceneContext) : "")}

                             {(hasSceneContext ? PromptSections.LastScenes(context.SceneContext, SceneContextCount) : "")}
                             """;
        chatHistory.AddUserMessage(contextPrompt);

        var requestPrompt = $"""
                             {PromptSections.SceneDirection(context.NewNarrativeDirection!.SceneDirection)}

                             {PromptSections.ContinuityCheck(context.NewNarrativeDirection!.ContinuityCheck)}

                             {PromptSections.SceneMetadata(context.NewNarrativeDirection!.SceneMetadata)}

                             {PromptSections.NewLore(context.NewLore)}

                             {PromptSections.NewLocations(context.NewLocations)}

                             These characters will be created after the scene is generated so emulation is not required for them. You have to emulate them yourself:
                             {PromptSections.NewCharacterRequests(context.NewNarrativeDirection.CreationRequests.Characters)}

                             {(hasSceneContext ? PromptSections.PlayerAction(context.PlayerAction) : "")}
                             """;
        chatHistory.AddUserMessage(requestPrompt);

        Microsoft.SemanticKernel.IKernelBuilder kernel = kernelBuilder.Create();
        var kgPlugin = new KnowledgeGraphPlugin(ragSearch, new CallerContext(GetType(), context.AdventureId));
        kernel.Plugins.Add(KernelPluginFactory.CreateFromObject(kgPlugin));
        var characterPlugin = new CharacterPlugin(agentKernel, logger, kernelBuilderFactory, ragSearch);
        await characterPlugin.Setup(context);
        kernel.Plugins.Add(KernelPluginFactory.CreateFromObject(characterPlugin));
        Kernel kernelWithKg = kernel.Build();

        var outputParser = ResponseParser.CreateJsonParser<GeneratedScene>("new_scene");

        GeneratedScene newScene = await agentKernel.SendRequestAsync(
            chatHistory,
            outputParser,
            kernelBuilder.GetDefaultFunctionPromptExecutionSettings(),
            nameof(WriterAgent),
            kernelWithKg,
            cancellationToken);

        context.NewScene = newScene;
        context.GenerationProcessStep = GenerationProcessStep.SceneGenerationFinished;
    }
}