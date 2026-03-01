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

using Serilog;

namespace FableCraft.Application.NarrativeEngine.Agents;

/// <summary>
///     Story Summary Agent - maintains rolling, compressed, character-specific summaries
///     of everything that happened before the 25-scene recent window.
///     Runs for both NPCs (using scene rewrites) and MC (using scene narratives).
/// </summary>
internal sealed class StorySummaryAgent(
    IAgentKernel agentKernel,
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    KernelBuilderFactory kernelBuilderFactory,
    IPluginFactory pluginFactory,
    ILogger logger) : BaseAgent(dbContextFactory, kernelBuilderFactory)
{
    protected override AgentName GetAgentName() => AgentName.StorySummaryAgent;

    /// <summary>
    ///     Invoke for NPC characters - uses scene rewrite as input.
    /// </summary>
    public async Task<StorySummaryOutput> InvokeForCharacter(
        GenerationContext context,
        CharacterContext characterContext,
        string agedOutSceneRewrite,
        int agedOutSceneNumber,
        string previousSummary,
        CancellationToken cancellationToken)
    {
        var kernelBuilder = await GetKernelBuilder(context);
        var systemPrompt = await GetPromptAsync(context);

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(systemPrompt);

        var contextPrompt = BuildContextPrompt(
            characterContext.Name,
            characterContext.Description,
            previousSummary);
        chatHistory.AddUserMessage(contextPrompt);

        var requestPrompt = BuildRequestPrompt(agedOutSceneRewrite, agedOutSceneNumber);
        chatHistory.AddUserMessage(requestPrompt);

        var outputParser = ResponseParser.CreateJsonParser<StorySummaryOutput>("story_summary", true);
        var promptExecutionSettings = kernelBuilder.GetDefaultFunctionPromptExecutionSettings();

        var kernelWithPlugins = kernelBuilder.Create();
        var callerContext = new CallerContext($"{nameof(StorySummaryAgent)}:{characterContext.Name}", context.AdventureId, context.NewSceneId);
        await pluginFactory.AddCharacterPluginAsync<CharacterNarrativePlugin>(kernelWithPlugins, context, callerContext, characterContext.CharacterId);
        var kernel = kernelWithPlugins.Build();

        var output = await agentKernel.SendRequestAsync(
            chatHistory,
            outputParser,
            promptExecutionSettings,
            $"{nameof(StorySummaryAgent)}:{characterContext.Name}",
            kernel,
            cancellationToken);

        logger.Information(
            "StorySummary for {CharacterName}: scene #{SceneNumber} aged out, summary length={Length}",
            characterContext.Name,
            agedOutSceneNumber,
            output.StorySummary?.Length ?? 0);

        return output;
    }

    /// <summary>
    ///     Invoke for MC - uses scene narrative as input.
    /// </summary>
    public async Task<StorySummaryOutput> InvokeForMc(
        GenerationContext context,
        string agedOutSceneNarrative,
        int agedOutSceneNumber,
        string previousSummary,
        CancellationToken cancellationToken)
    {
        var kernelBuilder = await GetKernelBuilder(context);
        var systemPrompt = await GetPromptAsync(context);

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(systemPrompt);

        var contextPrompt = BuildContextPrompt(
            context.MainCharacter.Name,
            context.MainCharacter.Description,
            previousSummary);
        chatHistory.AddUserMessage(contextPrompt);

        var requestPrompt = BuildRequestPrompt(agedOutSceneNarrative, agedOutSceneNumber);
        chatHistory.AddUserMessage(requestPrompt);

        var outputParser = ResponseParser.CreateJsonParser<StorySummaryOutput>("story_summary", true);
        var promptExecutionSettings = kernelBuilder.GetDefaultFunctionPromptExecutionSettings();

        var kernelWithPlugins = kernelBuilder.Create();
        var callerContext = new CallerContext($"{nameof(StorySummaryAgent)}:MC", context.AdventureId, context.NewSceneId);
        await pluginFactory.AddPluginAsync<MainCharacterNarrativePlugin>(kernelWithPlugins, context, callerContext);
        var kernel = kernelWithPlugins.Build();

        var output = await agentKernel.SendRequestAsync(
            chatHistory,
            outputParser,
            promptExecutionSettings,
            $"{nameof(StorySummaryAgent)}:MC",
            kernel,
            cancellationToken);

        logger.Information(
            "StorySummary for MC: scene #{SceneNumber} aged out, summary length={Length}",
            agedOutSceneNumber,
            output.StorySummary?.Length ?? 0);

        return output;
    }

    private static string BuildContextPrompt(
        string characterName,
        string characterDescription,
        string previousSummary)
    {
        var summarySection = string.IsNullOrEmpty(previousSummary)
            ? "No previous summary exists. This is the first scene falling off the window."
            : $"""
               <previous_summary>
               {previousSummary}
               </previous_summary>
               """;

        return $"""
                <character>
                Name: {characterName}
                Description: {characterDescription}
                </character>

                {summarySection}
                """;
    }

    private static string BuildRequestPrompt(string sceneContent, int sceneNumber)
    {
        return $"""
                Integrate the following scene (#{sceneNumber}) into the story summary. This scene is falling off the recent window and needs to be compressed into long-term memory.

                <scene_to_integrate>
                {sceneContent}
                </scene_to_integrate>
                """;
    }
}
