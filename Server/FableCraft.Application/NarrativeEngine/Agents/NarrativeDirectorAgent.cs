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
        SceneContext? lastScene = context.SceneContext.MaxBy(x => x.SequenceNumber);
        var hasSceneContext = context.SceneContext.Length > 0;

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(systemPrompt);

        var contextPrompt = $"""
                             {PromptSections.MainCharacter(context.MainCharacter, context.LatestSceneContext?.Metadata.MainCharacterDescription)}

                             {PromptSections.MainCharacterTrackerPreGeneration(context.SceneContext)}

                             {PromptSections.ExistingCharacters(context.Characters, context.ContextGathered?.RelevantCharacters)}

                             {PromptSections.Context(context.ContextGathered)}

                             {PromptSections.CurrentStoryTracker(context.SceneContext)}

                             {(hasSceneContext ? PromptSections.LastScenes(context.SceneContext, SceneContextCount) : "")}
                             """;
        chatHistory.AddUserMessage(contextPrompt);

        string requestPrompt;
        if (hasSceneContext)
        {
            requestPrompt = $"""
                             {PromptSections.LastSceneNarrativeDirection(lastScene?.Metadata.NarrativeMetadata)}

                             The {context.MainCharacter.Name} action in the last scene was:
                             {PromptSections.PlayerAction(context.PlayerAction)}
                             
                             Generate the next narrative direction for the story based on the above information.
                             """;
        }
        else
        {
            var instruction = await dbContext.Adventures
                .Select(x => new { x.Id, x.FirstSceneGuidance })
                .SingleAsync(x => x.Id == context.AdventureId, cancellationToken);
            requestPrompt = PromptSections.InitialInstruction(instruction.FirstSceneGuidance);
        }

        chatHistory.AddUserMessage(requestPrompt);

        Microsoft.SemanticKernel.IKernelBuilder kernel = kernelBuilder.Create();
        var kgPlugin = new KnowledgeGraphPlugin(ragSearch, new CallerContext(GetType(), context.AdventureId));
        kernel.Plugins.Add(KernelPluginFactory.CreateFromObject(kgPlugin));
        var characterStatePlugin = new CharacterStatePlugin(context.Characters, logger);
        kernel.Plugins.Add(KernelPluginFactory.CreateFromObject(characterStatePlugin));
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