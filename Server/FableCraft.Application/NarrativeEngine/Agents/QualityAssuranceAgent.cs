using FableCraft.Application.NarrativeEngine.Agents.Builders;
using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Infrastructure.Llm;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;

using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace FableCraft.Application.NarrativeEngine.Agents;

internal sealed class QualityAssuranceAgent(
    IAgentKernel agentKernel,
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    KernelBuilderFactory kernelBuilderFactory) : BaseAgent(dbContextFactory, kernelBuilderFactory)
{
    protected override AgentName GetAgentName() => AgentName.QualityAssuranceAgent;

    public async Task<QaReviewOutput> Invoke(
        GenerationContext context,
        CancellationToken cancellationToken)
    {
        var kernelBuilder = await GetKernelBuilder(context);

        var systemPrompt = await GetPromptAsync(context);
        systemPrompt = systemPrompt.Replace(PlaceholderNames.CharacterName, context.MainCharacter.Name);

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(systemPrompt);

        var contextPrompt = $"""
                             {PromptSections.MainCharacter(context)}

                             {PromptSections.MainCharacterTracker(context.SceneContext)}

                             {PromptSections.Context(context)}

                             {PromptSections.CurrentSceneTracker(context)}

                             {PromptSections.McStorySummary(context)}
                             """;
        chatHistory.AddUserMessage($"""
                                    {PromptSections.BackgroundCharacterProfiles(context.BackgroundCharacters)}
                                    """);
        chatHistory.AddUserMessage(contextPrompt);

        foreach (string se in context.SceneContext
                     .OrderByDescending(x => x.SequenceNumber)
                     .Take(SceneContextCount)
                     .OrderBy(x => x.SequenceNumber)
                     .Select(x => $"""
                                   Time: {x.Metadata.Tracker!.Scene!.Time}
                                   Location: {x.Metadata.Tracker.Scene.Location}
                                   Weather: {x.Metadata.Tracker.Scene.Weather}
                                   Characters: {string.Join(", ", x.Metadata.Tracker.Scene.CharactersPresent)}
                                   {x.SceneContent}
                                   """))
        {
            chatHistory.AddUserMessage(se);
        }

        var hasSceneContext = context.SceneContext.Length > 0;
        string requestPrompt;
        if (!hasSceneContext)
        {
            requestPrompt = $"""
                             {PromptSections.CurrentScene(context)}

                             This is the first scene of the adventure. Review it for quality issues.
                             """;
        }
        else
        {
            requestPrompt = $"""
                             {PromptSections.NarrativeCatalystGuidance(context)}
                             
                             {PromptSections.PlayerAction(context.PlayerAction)}

                             {PromptSections.CurrentScene(context)}

                             Review the scene above for quality issues.
                             """;
        }

        chatHistory.AddUserMessage(requestPrompt);

        var kernel = kernelBuilder.Create();
        var kernelWithKg = kernel.Build();
        var outputParser = ResponseParser.CreateTextParser("qa_review");
        var promptExecutionSettings = kernelBuilder.GetDefaultPromptExecutionSettings();

        var reviewText = await agentKernel.SendRequestAsync(
            chatHistory,
            outputParser,
            promptExecutionSettings,
            nameof(QualityAssuranceAgent),
            kernelWithKg,
            cancellationToken);

        return new QaReviewOutput { ReviewText = reviewText };
    }

    private const int SceneContextCount = 5;
}