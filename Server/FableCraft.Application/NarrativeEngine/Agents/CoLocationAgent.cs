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

internal sealed class CoLocationAgent(
    IAgentKernel agentKernel,
    ILogger logger,
    KernelBuilderFactory kernelBuilderFactory,
    IDbContextFactory<ApplicationDbContext> dbContextFactory) : BaseAgent(dbContextFactory, kernelBuilderFactory), IProcessor
{
    private const int RecentSceneCount = 3;

    public async Task Invoke(
        GenerationContext context,
        CancellationToken cancellationToken)
    {
        if (context.SkipCoLocation)
        {
            logger.Information("CoLocationAgent: Skipping (not selected for regeneration)");
            return;
        }

        if (context.CoLocationOutput != null)
        {
            logger.Information("CoLocationAgent: Co-location already determined for adventure {AdventureId}, skipping.",
                context.AdventureId);
            return;
        }

        var sceneLocation = context.NewTracker?.Scene?.Location ?? context.LatestTracker()?.Scene?.Location;
        var sceneTime = context.NewTracker?.Scene?.Time ?? context.LatestTracker()?.Scene?.Time;

        if (string.IsNullOrEmpty(sceneLocation))
        {
            logger.Information("CoLocationAgent: No scene location available, skipping co-location determination.");
            return;
        }

        var kernelBuilder = await GetKernelBuilder(context);
        var systemPrompt = await GetPromptAsync(context);

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(systemPrompt);

        var contextPrompt = BuildContextMessage(context, sceneLocation, sceneTime);
        chatHistory.AddUserMessage(contextPrompt);

        var requestPrompt = "Determine which characters from the registry are physically present at the current scene location. Output only the co-location list.";
        chatHistory.AddUserMessage(requestPrompt);

        var outputParser = ResponseParser.CreateJsonParser<CoLocationOutput>("output");
        var kernel = kernelBuilder.Create().Build();

        var output = await agentKernel.SendRequestAsync(
            chatHistory,
            outputParser,
            kernelBuilder.GetDefaultPromptExecutionSettings(),
            nameof(CoLocationAgent),
            kernel,
            cancellationToken);

        context.CoLocationOutput = output;

        logger.Information(
            "CoLocationAgent: Determined {Count} co-located characters for adventure {AdventureId} at location {Location}",
            output.CoLocatedCharacters.Length,
            context.AdventureId,
            sceneLocation);
    }

    protected override AgentName GetAgentName() => AgentName.CoLocationAgent;

    private string BuildContextMessage(GenerationContext context, string sceneLocation, string? sceneTime)
    {
        var charactersPresent = context.NewTracker?.Scene?.CharactersPresent
                                ?? context.LatestTracker()?.Scene?.CharactersPresent
                                ?? [];

        var characterRegistry = BuildCharacterRegistry(context, charactersPresent);
        var recentNarrative = BuildRecentNarrative(context);

        return $"""
                <scene_location>
                {sceneLocation}
                </scene_location>

                <scene_time>
                {sceneTime ?? "Unknown"}
                </scene_time>

                <characters_already_present>
                {string.Join(", ", charactersPresent)}
                </characters_already_present>

                <character_registry>
                {characterRegistry}
                </character_registry>

                <recent_narrative>
                {recentNarrative}
                </recent_narrative>
                """;
    }

    private static string BuildCharacterRegistry(GenerationContext context, string[] charactersPresent)
    {
        var registryEntries = new List<string>();

        foreach (var character in context.Characters.Where(c =>
                     !c.IsDead &&
                     c.CharacterTracker?.Location != null &&
                     (c.Importance == CharacterImportance.Significant || c.Importance == CharacterImportance.ArcImportance)))
        {
            if (charactersPresent.Contains(character.Name, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            var importance = character.Importance == CharacterImportance.ArcImportance ? "arc_important" : "significant";
            registryEntries.Add($"- {character.Name} | Importance: {importance} | Location: {character.CharacterTracker!.Location}");
        }

        foreach (var bgChar in context.BackgroundCharacters.Where(c => !string.IsNullOrEmpty(c.LastLocation)))
        {
            if (charactersPresent.Contains(bgChar.Name, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            registryEntries.Add($"- {bgChar.Name} | Importance: background | Location: {bgChar.LastLocation}");
        }

        return registryEntries.Count > 0
            ? string.Join("\n", registryEntries)
            : "No characters in registry with known locations.";
    }

    private string BuildRecentNarrative(GenerationContext context)
    {
        if (context.SceneContext.Length == 0)
        {
            return "No previous scenes.";
        }

        var scenes = context.SceneContext
            .OrderByDescending(x => x.SequenceNumber)
            .Take(RecentSceneCount)
            .OrderBy(x => x.SequenceNumber);

        var formatted = string.Join("\n\n---\n\n",
            scenes.Select(s => $"""
                                Time: {s.Metadata.Tracker?.Scene?.Time}
                                Location: {s.Metadata.Tracker?.Scene?.Location}

                                {s.SceneContent}
                                """));

        return formatted;
    }
}
