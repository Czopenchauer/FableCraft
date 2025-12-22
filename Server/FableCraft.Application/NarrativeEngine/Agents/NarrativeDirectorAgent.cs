using FableCraft.Application.NarrativeEngine.Agents.Builders;
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

using static FableCraft.Infrastructure.Clients.RagClientExtensions;

using IKernelBuilder = FableCraft.Infrastructure.Llm.IKernelBuilder;

namespace FableCraft.Application.NarrativeEngine.Agents;

internal sealed class NarrativeDirectorAgent(
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    IAgentKernel agentKernel,
    KernelBuilderFactory kernelBuilderFactory,
    IRagSearch ragSearch,
    ILogger logger) : BaseAgent(dbContextFactory, kernelBuilderFactory), IProcessor
{
    private const int SceneContextCount = 20;

    protected override AgentName GetAgentName() => AgentName.NarrativeDirectorAgent;

    public async Task Invoke(GenerationContext context, CancellationToken cancellationToken)
    {
        if (context.NewNarrativeDirection != null)
        {
            logger.Information("Skipping NarrativeDirectorAgent because NewNarrativeDirection is already set.");
            return;
        }

        IKernelBuilder kernelBuilder = await GetKernelBuilder(context);
        await using ApplicationDbContext dbContext = await DbContextFactory.CreateDbContextAsync(cancellationToken);
        var systemPrompt = await GetPromptAsync(context);
        SceneContext? lastScene = context.SceneContext.MaxBy(x => x.SequenceNumber);
        var hasSceneContext = context.SceneContext.Length > 0;

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(systemPrompt);

        var contextPrompt = $"""
                             {PromptSections.WorldSettings(context.WorldSettings)}

                             {PromptSections.MainCharacter(context)}

                             {PromptSections.MainCharacterTracker(context.SceneContext)}

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
                             {PromptSections.LastSceneNarrativeDirection(lastScene?.Metadata.NarrativeMetadata.NarrativeTracking)}

                             {GetStyleGuide(context)}
                             
                             The {context.MainCharacter.Name} action in the last scene was:
                             {PromptSections.PlayerAction(context.PlayerAction)}

                             Generate the next narrative direction for the story based on the above information.
                             """;
        }
        else
        {
            var instruction = await dbContext.Adventures
                .Select(x => new { x.Id, x.FirstSceneGuidance, x.AuthorNotes })
                .SingleAsync(x => x.Id == context.AdventureId, cancellationToken);
            requestPrompt = PromptSections.InitialInstruction(instruction.FirstSceneGuidance, instruction.AuthorNotes ?? string.Empty);
        }

        chatHistory.AddUserMessage(requestPrompt);

        Microsoft.SemanticKernel.IKernelBuilder kernel = kernelBuilder.Create();
        var datasets = new List<string>
        {
            GetWorldDatasetName(context.AdventureId),
            GetMainCharacterDatasetName(context.AdventureId)
        };
        var kgPlugin = new KnowledgeGraphPlugin(ragSearch, new CallerContext(GetType(), context.AdventureId), datasets);
        kernel.Plugins.Add(KernelPluginFactory.CreateFromObject(kgPlugin));
        var characterState = new CharacterStatePlugin(context.Characters, logger);
        kernel.Plugins.Add(KernelPluginFactory.CreateFromObject(characterState));
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
    }

    private static string GetStyleGuide(GenerationContext context)
    {
        if (!string.IsNullOrEmpty(context.AuthorNotes))
        {
            return $"""
                    Style Guide for the adventure:
                    {context.AuthorNotes}
                   """;
        }

        return string.Empty;
    }
}