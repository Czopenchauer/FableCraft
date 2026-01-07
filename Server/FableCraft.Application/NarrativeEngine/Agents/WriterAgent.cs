using FableCraft.Application.NarrativeEngine.Agents.Builders;
using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Application.NarrativeEngine.Plugins;
using FableCraft.Application.NarrativeEngine.Plugins.Impl;
using FableCraft.Application.NarrativeEngine.Workflow;
using FableCraft.Infrastructure.Clients;
using FableCraft.Infrastructure.Llm;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;

using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

using IKernelBuilder = FableCraft.Infrastructure.Llm.IKernelBuilder;

namespace FableCraft.Application.NarrativeEngine.Agents;

internal sealed class WriterAgent : BaseAgent, IProcessor
{
    private readonly IAgentKernel _agentKernel;
    private readonly IPluginFactory _pluginFactory;

    public WriterAgent(IAgentKernel agentKernel,
        IDbContextFactory<ApplicationDbContext> dbContextFactory,
        KernelBuilderFactory kernelBuilderFactory,
        IPluginFactory pluginFactory) : base(dbContextFactory, kernelBuilderFactory)
    {
        _agentKernel = agentKernel;
        _pluginFactory = pluginFactory;
    }

    private const int SceneContextCount = 15;

    protected override AgentName GetAgentName() => AgentName.WriterAgent;

    public async Task Invoke(
        GenerationContext context,
        CancellationToken cancellationToken)
    {
        IKernelBuilder kernelBuilder = await GetKernelBuilder(context);
        var systemPrompt = await GetPromptAsync(context);
        var hasSceneContext = context.SceneContext.Length > 0;

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(systemPrompt);

        var contextPrompt = $"""
                             {PromptSections.CharacterForEmulation(context.Characters)}

                             {PromptSections.MainCharacter(context)}

                             {PromptSections.MainCharacterTracker(context.SceneContext)}

                             {PromptSections.Context(context)}

                             {PromptSections.CurrentStoryTracker(context)}

                             {PromptSections.PreviousCharacterObservations(context.SceneContext)}

                             {(hasSceneContext ? PromptSections.LastScenes(context.SceneContext, SceneContextCount) : "")}
                             """;
        chatHistory.AddUserMessage(contextPrompt);

        string requestPrompt;
        if (!hasSceneContext)
        {
            await using var dbContext = await DbContextFactory.CreateDbContextAsync(cancellationToken);

            var instruction = await dbContext.Adventures
                .Select(x => new { x.Id, x.FirstSceneGuidance })
                .SingleAsync(x => x.Id == context.AdventureId, cancellationToken);
            requestPrompt = $"""
                             {PromptSections.ResolutionOutput(context.NewResolution)}

                             {PromptSections.InitialInstruction(instruction.FirstSceneGuidance)}

                             Generate a detailed scene based on the above resolution and context.
                             """;
        }
        else
        {
            requestPrompt = $"""
                             {PromptSections.ChroniclerGuidance(context.SceneContext)}

                             {GetPendingMcInteractions(context)}

                             {PromptSections.ResolutionOutput(context.NewResolution)}

                             {PromptSections.PlayerAction(context.PlayerAction)}

                             {PromptSections.ActionResolution(context)}

                             Generate a detailed scene based on the above resolution and context.
                             """;
        }

        chatHistory.AddUserMessage(requestPrompt);

        Microsoft.SemanticKernel.IKernelBuilder kernel = kernelBuilder.Create();
        var callerContext = new CallerContext(GetType(), context.AdventureId);
        await _pluginFactory.AddPluginAsync<WorldKnowledgePlugin>(kernel, context, callerContext);
        await _pluginFactory.AddPluginAsync<MainCharacterNarrativePlugin>(kernel, context, callerContext);
        await _pluginFactory.AddPluginAsync<CharacterEmulationPlugin>(kernel, context, callerContext);
        Kernel kernelWithKg = kernel.Build();

        var outputParser = ResponseParser.CreateJsonParser<GeneratedScene>("scene_output");

        GeneratedScene newScene = await _agentKernel.SendRequestAsync(
            chatHistory,
            outputParser,
            kernelBuilder.GetDefaultFunctionPromptExecutionSettings(),
            nameof(WriterAgent),
            kernelWithKg,
            cancellationToken);

        context.NewScene = newScene;
    }

    /// <summary>
    /// Builds a prompt section for pending MC interactions from characters who want to seek the MC.
    /// Characters with high/immediate urgency should be woven into the upcoming scene.
    /// </summary>
    private static string GetPendingMcInteractions(GenerationContext context)
    {
        var pendingInteractions = context.Characters
            .Where(c => c.SimulationMetadata?.PendingMcInteraction?.ExtensionData != null)
            .Select(c => new
            {
                Character = c.Name,
                Data = c.SimulationMetadata!.PendingMcInteraction!.ExtensionData!
            })
            .ToList();

        if (pendingInteractions.Count == 0)
        {
            return string.Empty;
        }

        var formatted = string.Join("\n\n",
            pendingInteractions.Select(p => $"""
                                             **{p.Character}**)
                                             {p.Data}
                                             """.Trim()));

        return $"""
                <pending_mc_interactions>
                The following characters have decided to seek out the MC. Consider weaving them into the scene based on urgency:
                {formatted}
                </pending_mc_interactions>
                """;
    }
}