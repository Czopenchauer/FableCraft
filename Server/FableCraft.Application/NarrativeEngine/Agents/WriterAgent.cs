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

                             {PromptSections.NewLore(context.NewLore)}

                             {PromptSections.NewLocations(context.NewLocations)}

                             {PromptSections.NewItems(context.NewItems)}

                             These characters will be created after the scene is generated so emulation is not required for them. You have to emulate them yourself:
                             {PromptSections.NewCharacterRequests(context.NewNarrativeDirection?.CreationRequests.Characters)}

                             {PromptSections.ExistingCharacters(context.Characters)}

                             {PromptSections.Context(context)}

                             {PromptSections.CurrentStoryTracker(context)}

                             {PromptSections.PreviousCharacterObservations(context.SceneContext)}

                             {(hasSceneContext ? PromptSections.LastScenes(context.SceneContext, SceneContextCount) : "")}
                             """;
        chatHistory.AddUserMessage(contextPrompt);

        var requestPrompt = $"""
                             {GetStyleGuide(context)}

                             Your new instructions:
                             {PromptSections.SceneDirection(context.NewNarrativeDirection!.WriterInstructions)}

                             {(hasSceneContext ? PromptSections.PlayerAction(context.PlayerAction) : "")}

                             Generate a detailed scene based on the above direction and context. Make sure to follow the scene direction instructions closely.
                             """;
        chatHistory.AddUserMessage(requestPrompt);

        Microsoft.SemanticKernel.IKernelBuilder kernel = kernelBuilder.Create();
        var callerContext = new CallerContext(GetType(), context.AdventureId);
        var worldPlugin = new WorldKnowledgePlugin(_ragSearch, callerContext);
        kernel.Plugins.Add(KernelPluginFactory.CreateFromObject(worldPlugin));
        var mainCharacterPlugin = new MainCharacterNarrativePlugin(_ragSearch, callerContext);
        kernel.Plugins.Add(KernelPluginFactory.CreateFromObject(mainCharacterPlugin));
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
        if (File.Exists(Path.Combine(context.PromptPath, "StoryBible.md")))
        {
            var content = File.ReadAllText(Path.Combine(context.PromptPath, "StoryBible.md"));
            return $"""
                     <story_bible>
                     {content}
                     </story_bible>
                    """;
        }

        return string.Empty;
    }
}