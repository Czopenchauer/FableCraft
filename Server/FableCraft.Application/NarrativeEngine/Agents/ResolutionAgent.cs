using FableCraft.Application.NarrativeEngine.Agents.Builders;
using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Application.NarrativeEngine.Workflow;
using FableCraft.Infrastructure.Llm;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;

using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

using Serilog;

namespace FableCraft.Application.NarrativeEngine.Agents;

internal sealed class ResolutionAgent(
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    IAgentKernel agentKernel,
    KernelBuilderFactory kernelBuilderFactory,
    ILogger logger) : BaseAgent(dbContextFactory, kernelBuilderFactory), IProcessor
{
    private const int SceneContextCount = 2;

    public async Task Invoke(GenerationContext context, CancellationToken cancellationToken)
    {
        if (context.NewResolution != null)
        {
            logger.Information("Skipping ResolutionAgent because NewResolution is already set.");
            return;
        }

        var kernelBuilder = await GetKernelBuilder(context);
        var systemPrompt = await GetPromptAsync(context);
        var hasSceneContext = context.SceneContext.Length > 0;

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(systemPrompt);

        var contextPrompt = $"""
                             {PromptSections.WorldSettings(context.PromptPath)}

                             {PromptSections.MainCharacter(context)}

                             {PromptSections.MainCharacterTracker(context.SceneContext)}

                             {PromptSections.ExistingCharacters(context.Characters)}

                             {PromptSections.CurrentSceneTracker(context)}

                             {(hasSceneContext ? PromptSections.LastScenes(context.SceneContext, SceneContextCount) : "")}
                             """;
        chatHistory.AddUserMessage(contextPrompt);

        string requestPrompt;
        if (hasSceneContext)
        {
            requestPrompt = $"""
                             The player action to resolve:
                             {PromptSections.PlayerAction(context.PlayerAction)}

                             Resolve this action and output the result.
                             """;
        }
        else
        {
            // First scene - no player action to resolve, return empty resolution
            context.NewResolution = string.Empty;
            logger.Information("First scene - no action to resolve, setting empty resolution");
            return;
        }

        chatHistory.AddUserMessage(requestPrompt);

        var kernelSkBuilder = kernelBuilder.Create();
        var kernel = kernelSkBuilder.Build();

        var outputParser = ResponseParser.CreateTextParser("resolution");

        var resolutionOutput = await agentKernel.SendRequestAsync(
            chatHistory,
            outputParser,
            kernelBuilder.GetDefaultPromptExecutionSettings(),
            nameof(ResolutionAgent),
            kernel,
            cancellationToken);

        context.NewResolution = resolutionOutput;
    }

    protected override AgentName GetAgentName() => AgentName.ResolutionAgent;
}