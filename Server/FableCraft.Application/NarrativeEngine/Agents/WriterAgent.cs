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

internal sealed class WriterAgent : BaseAgent, IProcessor
{
    private readonly IAgentKernel _agentKernel;
    private readonly ILogger _logger;
    private readonly IRagSearch _ragSearch;

    public WriterAgent(IAgentKernel agentKernel,
        ILogger logger,
        IDbContextFactory<ApplicationDbContext> dbContextFactory,
        KernelBuilderFactory kernelBuilderFactory,
        IRagSearch ragSearch) : base(dbContextFactory, kernelBuilderFactory)
    {
        _agentKernel = agentKernel;
        _logger = logger;
        _ragSearch = ragSearch;
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
                             {PromptSections.WorldSettings(context.WorldSettings)}

                             {PromptSections.MainCharacter(context)}

                             {PromptSections.MainCharacterTracker(context.SceneContext)}

                             {PromptSections.ExistingCharacters(context.Characters)}

                             {PromptSections.Context(context.ContextGathered)}

                             {(hasSceneContext ? PromptSections.CurrentStoryTracker(context.SceneContext) : "")}

                             {(hasSceneContext ? PromptSections.LastScenes(context.SceneContext, SceneContextCount) : "")}
                             """;
        chatHistory.AddUserMessage(contextPrompt);

        var requestPrompt = $"""
                             Your new instructions:
                             {PromptSections.SceneDirection(context.NewNarrativeDirection!.WriterInstructions)}

                             {PromptSections.NewLore(context.NewLore)}

                             {PromptSections.NewLocations(context.NewLocations)}

                             {PromptSections.NewItems(context.NewItems)}

                             These characters will be created after the scene is generated so emulation is not required for them. You have to emulate them yourself:
                             {PromptSections.NewCharacterRequests(context.NewNarrativeDirection.CreationRequests.Characters)}
                             
                             {GetStyleGuide(context)}

                             {(hasSceneContext ? PromptSections.PlayerAction(context.PlayerAction) : "")}

                             Generate a detailed scene based on the above direction and context. Make sure to follow the scene direction instructions closely.
                             """;
        chatHistory.AddUserMessage(requestPrompt);

        Microsoft.SemanticKernel.IKernelBuilder kernel = kernelBuilder.Create();
        var datasets = new List<string>
        {
            GetWorldDatasetName(context.AdventureId),
            GetMainCharacterDatasetName(context.AdventureId)
        };
        var kgPlugin = new KnowledgeGraphPlugin(_ragSearch, new CallerContext(GetType(), context.AdventureId), datasets);
        kernel.Plugins.Add(KernelPluginFactory.CreateFromObject(kgPlugin));
        var characterPlugin = new CharacterPlugin(_agentKernel, _logger, DbContextFactory, KernelBuilderFactory, _ragSearch);
        await characterPlugin.Setup(context);
        kernel.Plugins.Add(KernelPluginFactory.CreateFromObject(characterPlugin));
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