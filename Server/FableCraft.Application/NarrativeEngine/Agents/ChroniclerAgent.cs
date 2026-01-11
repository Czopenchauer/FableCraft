using FableCraft.Application.NarrativeEngine.Agents.Builders;
using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Application.NarrativeEngine.Plugins;
using FableCraft.Application.NarrativeEngine.Plugins.Impl;
using FableCraft.Infrastructure.Clients;
using FableCraft.Infrastructure.Llm;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;

using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

using IKernelBuilder = FableCraft.Infrastructure.Llm.IKernelBuilder;

namespace FableCraft.Application.NarrativeEngine.Agents;

/// <summary>
/// The Chronicler is the story's memory and conscience. It watches what happens and understands
/// the narrative implications—tracking dramatic questions, promises, threads, stakes, windows,
/// and world momentum. It provides writer guidance for scene generation.
/// </summary>
internal sealed class ChroniclerAgent(
    IAgentKernel agentKernel,
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    KernelBuilderFactory kernelBuilderFactory,
    IPluginFactory pluginFactory) : BaseAgent(dbContextFactory, kernelBuilderFactory)
{
    private const int MaxScene = 20;

    protected override AgentName GetAgentName() => AgentName.ChroniclerAgent;

    public async Task<ChroniclerOutput> Invoke(
        GenerationContext context,
        SceneTracker sceneTracker,
        CancellationToken cancellationToken)
    {
        if (context.ChroniclerOutput is not null)
        {
            return context.ChroniclerOutput;
        }

        IKernelBuilder kernelBuilder = await GetKernelBuilder(context);

        var systemPrompt = await GetPromptAsync(context);
        var isFirstScene = (context.SceneContext?.Length ?? 0) == 0;

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(systemPrompt);

        var contextPrompt = BuildContextPrompt(context, sceneTracker, isFirstScene);
        chatHistory.AddUserMessage(contextPrompt);

        var requestPrompt = await BuildRequestPrompt(context, isFirstScene, cancellationToken);
        chatHistory.AddUserMessage(requestPrompt);

        Microsoft.SemanticKernel.IKernelBuilder kernel = kernelBuilder.Create();
        var callerContext = new CallerContext(GetType(), context.AdventureId, context.NewSceneId);
        await pluginFactory.AddPluginAsync<WorldKnowledgePlugin>(kernel, context, callerContext);
        await pluginFactory.AddPluginAsync<MainCharacterNarrativePlugin>(kernel, context, callerContext);
        Kernel kernelWithKg = kernel.Build();

        var outputParser = ResponseParser.CreateJsonParser<ChroniclerOutput>("chronicler", true);
        PromptExecutionSettings promptExecutionSettings = kernelBuilder.GetDefaultFunctionPromptExecutionSettings();

        return await agentKernel.SendRequestAsync(
            chatHistory,
            outputParser,
            promptExecutionSettings,
            nameof(ChroniclerAgent),
            kernelWithKg,
            cancellationToken);
    }

    private string BuildContextPrompt(GenerationContext context, SceneTracker sceneTracker, bool isFirstScene)
    {
        var previousChroniclerState = GetPreviousChroniclerState(context);
        var loreRequested = context.NewScene!.CreationRequests?.Lore != null
            ? $"""
               <lore_requested>
               This lore was already requested. Do not request it again.
               {string.Join("\n", context.NewScene!.CreationRequests?.Lore.ToJsonString() ?? string.Empty)}
               </lore_requested>
               """
            : string.Empty;

        return $"""
                {PromptSections.MainCharacter(context)}

                {PromptSections.ExistingCharacters(context.Characters)}

                {(!isFirstScene ? PromptSections.LastScenes(context.SceneContext!, MaxScene) : "")}

                {PromptSections.SceneTracker(context, sceneTracker)}

                {loreRequested}

                {previousChroniclerState}
                """;
    }

    private async Task<string> BuildRequestPrompt(GenerationContext context, bool isFirstScene, CancellationToken cancellationToken)
    {
        if (isFirstScene)
        {
            await using var dbContext = await DbContextFactory.CreateDbContextAsync(cancellationToken);
            var instruction = await dbContext.Adventures
                .Select(x => new { x.Id, x.FirstSceneGuidance })
                .SingleAsync(x => x.Id == context.AdventureId, cancellationToken);
            return $"""
                    {PromptSections.InitialInstruction(instruction.FirstSceneGuidance)}

                    {PromptSections.CurrentScene(context)}

                    It's the first scene of the adventure. Initialize the story state based on the scene content.
                    Identify any initial dramatic questions, promises, threads, or stakes that emerge from this opening.
                    """;
        }

        return $"""
                {PromptSections.CurrentScene(context)}

                Update the story state based on the current scene.
                Consider time elapsed between scenes for world momentum advancement.
                Generate writer guidance for the next scene.
                """;
    }

    private static string GetPreviousChroniclerState(GenerationContext context)
    {
        var previousState = context.SceneContext?
            .OrderByDescending(x => x.SequenceNumber)
            .FirstOrDefault()?.Metadata.ChroniclerState;

        if (previousState == null)
        {
            return string.Empty;
        }

        return $"""
                <previous_story_state>
                {previousState.ToJsonString()}
                </previous_story_state>
                """;
    }
}