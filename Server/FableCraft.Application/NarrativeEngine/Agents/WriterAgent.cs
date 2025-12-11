using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Application.NarrativeEngine.Plugins;
using FableCraft.Application.NarrativeEngine.Workflow;
using FableCraft.Infrastructure.Clients;
using FableCraft.Infrastructure.Llm;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;

using Microsoft.SemanticKernel;

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

        var builder = ChatHistoryBuilder.Create()
            .WithSystemMessage(systemPrompt)
            .WithSceneDirection(context.NewNarrativeDirection!.SceneDirection)
            .WithContinuityCheck(context.NewNarrativeDirection!.ContinuityCheck)
            .WithSceneMetadata(context.NewNarrativeDirection!.SceneMetadata)
            .WithNewLore(context.NewLore)
            .WithNewLocations(context.NewLocations)
            .WithExistingCharacters(context.Characters)
            .WithNewCharacterRequests(context.NewNarrativeDirection.CreationRequests.Characters.ToArray(), "These character will be created after the scene is generated so emulation is not required for them. You have to emulate them yourself.")
            .WithMainCharacter(context.MainCharacter, context.LatestSceneContext?.Metadata.MainCharacterDescription)
            .WithMainCharacterTracker(context)
            .WithExtraContext(context.ContextGathered);

        if (hasSceneContext)
        {
            builder
                .WithStorySummary(context.Summary)
                .WithCurrentSceneTracker(context.SceneContext)
                .WithLastScenes(context.SceneContext, SceneContextCount)
                .WithPlayerAction(context.PlayerAction);
        }

        var chatHistory = builder.Build();

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
