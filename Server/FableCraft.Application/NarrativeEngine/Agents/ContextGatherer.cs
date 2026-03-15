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

using Serilog;

namespace FableCraft.Application.NarrativeEngine.Agents;

internal sealed class ContextGatherer(
    IAgentKernel agentKernel,
    ILogger logger,
    IPluginFactory pluginFactory,
    KernelBuilderFactory kernelBuilderFactory,
    IDbContextFactory<ApplicationDbContext> dbContextFactory) : BaseAgent(dbContextFactory, kernelBuilderFactory), IProcessor
{
    private const int SceneContextCount = 20;

    public async Task Invoke(
        GenerationContext context,
        CancellationToken cancellationToken)
    {
        if (context.SkipContextGatherer)
        {
            logger.Information("ContextGatherer: Skipping (not selected for regeneration)");
            return;
        }

        if (context.ContextGathered != null)
        {
            logger.Information("Context already gathered for adventure {AdventureId}, skipping context gathering step.", context.AdventureId);
            return;
        }

        var kernelBuilder = await GetKernelBuilder(context);
        var systemPrompt = await GetPromptAsync(context);

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(systemPrompt);

        var previousContext = GetPreviousContextSummary(context);
        var sceneLocation = context.NewTracker?.Scene?.Location ?? context.LatestTracker()?.Scene?.Location;
        var contextPrompt = $"""
                             {LoreGenerated(context)}

                             {previousContext}

                             {CharacterLocationsForDiscovery(context, sceneLocation)}

                             {PromptSections.McStorySummary(context)}

                             {(context.SceneContext.Length > 0 ? PromptSections.RecentScenes(context.SceneContext, SceneContextCount) : "")}
                             """;
        chatHistory.AddUserMessage(contextPrompt);

        var requestPrompt = $"""
                             {PromptSections.SceneTracker(context, context.NewTracker!.Scene!)}
                             The latest scene that was just generated:
                             <latest_scene>
                             {context.NewScene?.Scene}
                             </latest_scene>
                             """;

        chatHistory.AddUserMessage(requestPrompt);

        var callerContext = new CallerContext(GetType().Name, context.AdventureId, context.NewSceneId);
        var skKernelBuilder = kernelBuilder.Create();

        await pluginFactory.AddPluginAsync<WorldKnowledgePlugin>(skKernelBuilder, context, callerContext);
        await pluginFactory.AddPluginAsync<MainCharacterNarrativePlugin>(skKernelBuilder, context, callerContext);

        var kernel = skKernelBuilder.Build();
        var outputParser = ResponseParser.CreateTextParser("context");

        try
        {
            var contextText = await agentKernel.SendRequestAsync(
                chatHistory,
                outputParser,
                kernelBuilder.GetDefaultPromptExecutionSettings(),
                nameof(ContextGatherer),
                kernel,
                cancellationToken);

            context.ContextGathered = new ContextBase
            {
                Context = contextText,
                AdditionalData = new Dictionary<string, object>(),
                BackgroundRoster = [],
                CoLocatedCharacters = []
            };

            logger.Information(
                "Context gathered for adventure {AdventureId}: {ContextLength} chars",
                context.AdventureId,
                contextText.Length);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error gathering context for adventure {AdventureId}", context.AdventureId);
        }
    }

    protected override AgentName GetAgentName() => AgentName.ContextGatherer;

    private static string CharacterRoster(GenerationContext context)
    {
        if (context.BackgroundCharacters.Count > 0)
        {
            var characters = context.BackgroundCharacters.Select(z =>
                $"Name: {z.Name}, Identity: {z.Identity}, Last Location: {z.LastLocation}, Last SeenTime: {z.LastSeenTime}");
            return $"""
                    ### Background Character Registry
                    {string.Join("\n- ", characters)}
                    """;
        }

        return string.Empty;
    }

    private static string CharacterLocationsForDiscovery(GenerationContext context, string? sceneLocation)
    {
        if (string.IsNullOrEmpty(sceneLocation))
        {
            return string.Empty;
        }

        var currentCharactersPresent = context.NewTracker?.Scene?.CharactersPresent
                                       ?? context.LatestTracker()?.Scene?.CharactersPresent
                                       ?? [];

        var characterLocations = new List<object>();

        foreach (var character in context.Characters.Where(c => !c.IsDead && c.CharacterTracker?.Location != null))
        {
            if (currentCharactersPresent.Contains(character.Name, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            characterLocations.Add(new
            {
                name = character.Name,
                importance = character.Importance.ToString().ToLowerInvariant(),
                location = character.CharacterTracker!.Location
            });
        }

        foreach (var bgChar in context.BackgroundCharacters.Where(c => !string.IsNullOrEmpty(c.LastLocation)))
        {
            if (currentCharactersPresent.Contains(bgChar.Name, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            characterLocations.Add(new
            {
                name = bgChar.Name,
                importance = "background",
                location = bgChar.LastLocation
            });
        }

        if (characterLocations.Count == 0)
        {
            return string.Empty;
        }

        return $"""
                <scene_location>
                {sceneLocation}
                </scene_location>

                <character_locations>
                {characterLocations.ToJsonString()}
                </character_locations>
                """;
    }

    private static string GetPreviousContextSummary(GenerationContext context)
    {
        var lastScene = context.SceneContext.MaxBy(x => x.SequenceNumber);
        var previousContext = lastScene?.Metadata.GatheredContext;
        if (previousContext == null || string.IsNullOrEmpty(previousContext.Context))
        {
            return string.Empty;
        }

        return $"""
                <previous_context_gathered>
                Context from previous scene generation (consider carrying forward relevant items):
                {previousContext.Context}

                Additional data:
                {context.ContextGathered?.AdditionalData}
                </previous_context_gathered>
                """;
    }

    private static string LoreGenerated(GenerationContext context)
    {
        if (context.PreviouslyGeneratedLore.Length > 0)
        {
            return $"""
                    Previously generated lore pieces (will be added to world context automatically):
                    {string.Join("\n", context.PreviouslyGeneratedLore.Select(x => $"- {x.Category}: {x.Title}"))}
                    """;
        }

        return string.Empty;
    }
}