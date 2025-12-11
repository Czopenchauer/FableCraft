using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Application.NarrativeEngine.Plugins;
using FableCraft.Application.NarrativeEngine.Workflow;
using FableCraft.Infrastructure.Clients;
using FableCraft.Infrastructure.Llm;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;

using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;

using Serilog;

using IKernelBuilder = FableCraft.Infrastructure.Llm.IKernelBuilder;

namespace FableCraft.Application.NarrativeEngine.Agents;

internal sealed class NarrativeDirectorAgent(
    ApplicationDbContext dbContext,
    IAgentKernel agentKernel,
    KernelBuilderFactory kernelBuilderFactory,
    IRagSearch ragSearch,
    ILogger logger) : IProcessor
{
    private const int SceneContextCount = 20;

    public async Task Invoke(GenerationContext context, CancellationToken cancellationToken)
    {
        IKernelBuilder kernelBuilder = kernelBuilderFactory.Create(context.ComplexPreset);
        var systemPrompt = await PromptBuilder.BuildPromptAsync("NarrativePrompt.md");
        var lastScene = context.SceneContext.MaxBy(x => x.SequenceNumber);
        var hasSceneContext = context.SceneContext.Length > 0;

        var builder = ChatHistoryBuilder.Create()
            .WithSystemMessage(systemPrompt)
            .WithLastSceneNarrativeDirection(lastScene?.Metadata.NarrativeMetadata)
            .WithStoryTracker(context)
            .WithMainCharacter(context.MainCharacter, context.LatestSceneContext?.Metadata.MainCharacterDescription)
            .WithMainCharacterTracker(context)
            .WithExistingCharacters(context.Characters, context.ContextGathered?.RelevantCharacters ?? [])
            .WithExtraContext(context.ContextGathered);

        if (hasSceneContext)
        {
            builder
                .WithStorySummary(context.Summary)
                .WithLastScenes(context.SceneContext, SceneContextCount)
                .WithPlayerAction(context.PlayerAction);
        }
        else
        {
            var instruction = await dbContext.Adventures
                .Select(x => new { x.Id, x.FirstSceneGuidance })
                .SingleAsync(x => x.Id == context.AdventureId, cancellationToken: cancellationToken);
            builder.WithInitialInstruction(instruction.FirstSceneGuidance);
        }

        var chatHistory = builder.Build();

        Microsoft.SemanticKernel.IKernelBuilder kernel = kernelBuilder.Create();
        var kgPlugin = new KnowledgeGraphPlugin(ragSearch, new CallerContext(GetType(), context.AdventureId));
        kernel.Plugins.Add(KernelPluginFactory.CreateFromObject(kgPlugin));
        var characterPlugin = new CharacterPlugin(agentKernel, logger, kernelBuilderFactory, ragSearch);
        await characterPlugin.Setup(context);
        kernel.Plugins.Add(KernelPluginFactory.CreateFromObject(characterPlugin));
        Kernel kernelWithKg = kernel.Build();

        var outputParser = ResponseParser.CreateJsonParser<NarrativeDirectorOutput>("narrative_scene_directive");

        NarrativeDirectorOutput narrativeOutput = await agentKernel.SendRequestAsync(
            chatHistory,
            outputParser,
            kernelBuilder.GetDefaultFunctionPromptExecutionSettings(),
            nameof(NarrativeDirectorAgent),
            kernelWithKg,
            cancellationToken);

        context.NewNarrativeDirection = narrativeOutput;
        context.GenerationProcessStep = GenerationProcessStep.NarrativeDirectionFinished;
    }
}
