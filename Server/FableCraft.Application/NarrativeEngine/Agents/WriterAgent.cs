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
        chatHistory.AddUserMessage(PromptSections.SceneDirection(context.NewNarrativeDirection!.SceneDirection));
        chatHistory.AddUserMessage(PromptSections.ContinuityCheck(context.NewNarrativeDirection!.ContinuityCheck));
        chatHistory.AddUserMessage(PromptSections.SceneMetadata(context.NewNarrativeDirection!.SceneMetadata));
        chatHistory.AddUserMessage(PromptSections.NewLore(context.NewLore));
        chatHistory.AddUserMessage(PromptSections.NewLocations(context.NewLocations));
        chatHistory.AddUserMessage(PromptSections.ExistingCharacters(context.Characters));
        chatHistory.AddUserMessage(PromptSections.NewCharacterRequests(context.NewNarrativeDirection.CreationRequests.Characters));
        chatHistory.AddUserMessage("These character will be created after the scene is generated so emulation is not required for them. You have to emulate them yourself.");
        chatHistory.AddUserMessage(PromptSections.MainCharacter(context.MainCharacter, context.LatestSceneContext?.Metadata.MainCharacterDescription));
        chatHistory.AddUserMessage(PromptSections.MainCharacterTracker(context.SceneContext));
        chatHistory.AddUserMessage(PromptSections.ExtraContext(context.ContextGathered));

        if (hasSceneContext)
        {
            chatHistory.AddUserMessage(PromptSections.StorySummary(context.Summary));
            chatHistory.AddUserMessage(PromptSections.CurrentSceneTracker(context.SceneContext));
            chatHistory.AddUserMessage(PromptSections.LastScenes(context.SceneContext, SceneContextCount));
            chatHistory.AddUserMessage(PromptSections.PlayerAction(context.PlayerAction));
        }

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
