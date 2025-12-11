using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Application.NarrativeEngine.Plugins;
using FableCraft.Application.NarrativeEngine.Workflow;
using FableCraft.Infrastructure.Clients;
using FableCraft.Infrastructure.Llm;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;

using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

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

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(systemPrompt);
        chatHistory.AddUserMessage(PromptSections.LastSceneNarrativeDirection(lastScene?.Metadata.NarrativeMetadata));
        chatHistory.AddUserMessage(PromptSections.CurrentSceneTracker(context.SceneContext));
        chatHistory.AddUserMessage(PromptSections.MainCharacter(context.MainCharacter, context.LatestSceneContext?.Metadata.MainCharacterDescription));
        chatHistory.AddUserMessage(PromptSections.MainCharacterTracker(context.SceneContext));
        chatHistory.AddUserMessage(PromptSections.ExistingCharacters(context.Characters, context.ContextGathered?.RelevantCharacters));
        chatHistory.AddUserMessage(PromptSections.ExtraContext(context.ContextGathered?.ContextBases));

        if (hasSceneContext)
        {
            chatHistory.AddUserMessage(PromptSections.StorySummary(context.Summary));
            chatHistory.AddUserMessage(PromptSections.LastScenes(context.SceneContext, SceneContextCount));
            chatHistory.AddUserMessage(PromptSections.PlayerAction(context.PlayerAction));
        }
        else
        {
            var instruction = await dbContext.Adventures
                .Select(x => new { x.Id, x.FirstSceneGuidance })
                .SingleAsync(x => x.Id == context.AdventureId, cancellationToken: cancellationToken);
            chatHistory.AddUserMessage(PromptSections.InitialInstruction(instruction.FirstSceneGuidance));
        }

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
