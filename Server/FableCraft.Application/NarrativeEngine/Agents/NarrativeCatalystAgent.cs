using System.Text;

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

namespace FableCraft.Application.NarrativeEngine.Agents;

internal sealed class NarrativeCatalystAgent(
    IAgentKernel agentKernel,
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    KernelBuilderFactory kernelBuilderFactory,
    IPluginFactory pluginFactory) : BaseAgent(dbContextFactory, kernelBuilderFactory)
{
    private const int MaxScene = 20;

    protected override AgentName GetAgentName() => AgentName.NarrativeCatalystAgent;

    public async Task<NarrativeCatalystOutput> Invoke(
        GenerationContext context,
        SceneTracker sceneTracker,
        CancellationToken cancellationToken)
    {
        if (context.NarrativeCatalystOutput is not null)
        {
            return context.NarrativeCatalystOutput;
        }

        var kernelBuilder = await GetKernelBuilder(context);

        var systemPrompt = await GetPromptAsync(context);
        var isFirstScene = (context.SceneContext?.Length ?? 0) == 0;

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(systemPrompt);

        var contextPrompt = await BuildContextPrompt(context, sceneTracker, isFirstScene, cancellationToken);
        chatHistory.AddUserMessage(contextPrompt);

        var requestPrompt = await BuildRequestPrompt(context, isFirstScene, cancellationToken);
        chatHistory.AddUserMessage(requestPrompt);

        var kernel = kernelBuilder.Create();
        var callerContext = new CallerContext(GetType().Name, context.AdventureId, context.NewSceneId);
        await pluginFactory.AddPluginAsync<WorldKnowledgePlugin>(kernel, context, callerContext);
        await pluginFactory.AddPluginAsync<MainCharacterNarrativePlugin>(kernel, context, callerContext);
        var kernelWithKg = kernel.Build();

        var promptExecutionSettings = kernelBuilder.GetDefaultFunctionPromptExecutionSettings();

        var response = await agentKernel.SendRequestAsync(
            chatHistory,
            rawResponse => rawResponse,
            promptExecutionSettings,
            nameof(NarrativeCatalystAgent),
            kernelWithKg,
            cancellationToken);

        var output = ParseOutput(response);
        context.NarrativeCatalystOutput = output;
        return output;
    }

    private static NarrativeCatalystOutput ParseOutput(string response)
    {
        var storyAssessment = ResponseParser.ExtractText(response, "story_assessment");
        var catalystGoals = ResponseParser.ExtractText(response, "catalyst");
        var randomEvent = ResponseParser.TryExtractText(response, "random_event");

        return new NarrativeCatalystOutput
        {
            StoryAssessment = storyAssessment,
            CatalystGoals = catalystGoals,
            RandomEvent = string.IsNullOrWhiteSpace(randomEvent) ? null : randomEvent
        };
    }

    private async Task<string> BuildContextPrompt(GenerationContext context, SceneTracker sceneTracker, bool isFirstScene, CancellationToken cancellationToken)
    {
        var previousCatalystGoals = GetPreviousCatalystGoals(context);
        var loreRequested = context.NewScene!.CreationRequests?.Lore != null
            ? $"""
               <lore_requested>
               This lore was already requested. Do not request it again.
               {string.Join("\n", context.NewScene!.CreationRequests?.Lore.ToJsonString() ?? string.Empty)}
               </lore_requested>
               """
            : string.Empty;

        await using var dbContext = await DbContextFactory.CreateDbContextAsync(cancellationToken);

        var instruction = await dbContext.Adventures
            .Select(x => new
            {
                x.Id,
                x.FirstSceneGuidance
            })
            .SingleAsync(x => x.Id == context.AdventureId, cancellationToken);
        var init = context.SceneContext.Length == 1 ? PromptSections.InitialInstruction(instruction.FirstSceneGuidance) : string.Empty;

        var scenes = context.SceneContext
            .OrderByDescending(x => x.SequenceNumber)
            .Take(MaxScene);
        var formatted = string.Join("\n",
            scenes
                .OrderBy(x => x.SequenceNumber)
                .Select(x => $"""
                              Scene number: {x.SequenceNumber}
                              Time: {x.Metadata.Tracker!.Scene!.Time}
                              Location: {x.Metadata.Tracker.Scene.Location}
                              Weather: {x.Metadata.Tracker.Scene.Weather}
                              Characters on scene: {string.Join(",", x.Metadata.Tracker.Scene.CharactersPresent)}
                              {x.SceneContent}
                              """));

        var inject = $"""
                <last_scenes>
                {formatted}
                </last_scenes>
                """;
        return $"""
                {PromptSections.Context(context)}
                
                {PromptSections.MainCharacter(context)}

                {context.LatestTracker()?.MainCharacter?.MainCharacter.ToJsonString() ?? string.Empty}

                {(!isFirstScene ? inject : "")}

                {PromptSections.SceneTracker(context, sceneTracker)}

                {loreRequested}

                {previousCatalystGoals}
                
                {init}
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

                    It's the first scene of the adventure. Assess where the story begins and set initial narrative goals.
                    """;
        }

        return $"""
                {PromptSections.CurrentScene(context)}

                Update your narrative goals based on the current scene.
                Assess where the story is now and what should happen next to make it more interesting.
                """;
    }

    private static string GetPreviousCatalystGoals(GenerationContext context)
    {
        var previousGoals = context.SceneContext?
            .OrderByDescending(x => x.SequenceNumber)
            .FirstOrDefault()?.Metadata;

        if (previousGoals is null)
        {
            return string.Empty;
        }
        
        var goals = new StringBuilder();
        if (string.IsNullOrEmpty(previousGoals.CatalystStoryAssessment))
        {
            goals.AppendLine($"""
                              Check these goals. Are they still going to make it more interesting? Are they still narrative goals?
                              <previous_story_assessment>
                              {previousGoals.CatalystStoryAssessment}
                              </previous_story_assessment>
                              """);
        }
        
        if (string.IsNullOrEmpty(previousGoals.CatalystGoals))
        {
            goals.AppendLine($"""
                             <previous_goals>
                             {previousGoals.CatalystGoals}
                             </previous_goals>
                             """);
        }

        return goals.ToString();
    }
}