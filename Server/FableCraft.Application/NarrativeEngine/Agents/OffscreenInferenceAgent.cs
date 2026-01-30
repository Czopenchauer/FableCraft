using System.Text;

using FableCraft.Application.NarrativeEngine.Agents.Builders;
using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Infrastructure.Llm;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;

using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

using Serilog;

namespace FableCraft.Application.NarrativeEngine.Agents;

/// <summary>
///     Performs lightweight inference about a significant character's current state.
///     This is NOT full simulation - it answers: "Given who this person is and what's happened,
///     where are they now and what state are they in?"
/// </summary>
internal sealed class OffscreenInferenceAgent(
    IAgentKernel agentKernel,
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    KernelBuilderFactory kernelBuilderFactory,
    CharacterTrackerAgent characterTrackerAgent,
    ILogger logger) : BaseAgent(dbContextFactory, kernelBuilderFactory)
{
    protected override AgentName GetAgentName() => AgentName.OffscreenInferenceAgent;

    public async Task Invoke(
        GenerationContext context,
        SimulationPlannerOutput plan,
        CancellationToken cancellationToken)
    {
        logger.Information("OffscreenInferenceProcessor: Starting...");

        var significantForInference = plan.SignificantForInference;
        if (significantForInference is not { Count: > 0 })
        {
            logger.Information("OffscreenInferenceProcessor: No significant characters need inference");
            return;
        }

        var currentSceneTracker = context.NewTracker?.Scene;
        if (currentSceneTracker == null)
        {
            logger.Warning("OffscreenInferenceProcessor: No scene tracker available, skipping inference");
            return;
        }

        logger.Information("Running OffscreenInference for {Count} significant characters",
            significantForInference.Count);

        await using var dbContext = await DbContextFactory.CreateDbContextAsync(cancellationToken);

        var characterNames = significantForInference.Select(s => s.Character).ToList();

        var eventsByCharacter = await dbContext.CharacterEvents
            .Where(e => e.AdventureId == context.AdventureId
                        && characterNames.Contains(e.TargetCharacterName)
                        && !e.Consumed)
            .GroupBy(e => e.TargetCharacterName)
            .ToDictionaryAsync(
                g => g.Key,
                g => g.ToList(),
                cancellationToken);

        var tasks = significantForInference
            .Select(async significantEntry =>
            {
                var character = context.Characters.FirstOrDefault(c => c.Name == significantEntry.Character);
                if (character == null)
                {
                    logger.Warning("OffscreenInference requested for unknown character: {CharacterName}",
                        significantEntry.Character);
                    return;
                }

                if (context.CharacterUpdates.Any(z => z.CharacterId ==  character.CharacterId))
                {
                    logger.Information("Skipping already simulated character: {CharacterName}", character.Name);
                    return;
                }

                var events = eventsByCharacter.GetValueOrDefault(character.Name, []);
                var eventDtos = events.Select(e => new CharacterEventDto
                {
                    Time = e.Time,
                    Event = e.Event,
                    SourceCharacter = e.SourceCharacterName,
                    SourceRead = e.SourceRead
                }).ToList();

                var timeElapsed = $"""
                                   Last action time: {character.SceneRewrites.MaxBy(z => z.SequenceNumber)?.SceneTracker!.Time}
                                   Current time: {context.NewTracker!.Scene!.Time}
                                   """;

                var input = new OffscreenInferenceInput
                {
                    Character = character,
                    EventsLog = eventDtos,
                    TimeElapsed = timeElapsed,
                    CurrentDateTime = currentSceneTracker.Time ?? "Unknown",
                    WorldEvents = context.SceneContext?
                        .OrderByDescending(x => x.SequenceNumber)
                        .FirstOrDefault()?.Metadata.ChroniclerState?.WorldMomentum
                };

                try
                {
                    logger.Information("Running OffscreenInference for {CharacterName} (events: {EventCount})",
                        character.Name,
                        eventDtos.Count);

                    var result = await Simulate(context, input, cancellationToken);

                    var memories = new List<MemoryContext>();
                    var sceneRewrites = new List<CharacterSceneContext>();

                    if (result.Scenes is { Count: > 0 })
                    {
                        foreach (var scene in result.Scenes)
                        {
                            var sceneTracker = new SceneTracker
                            {
                                Time = scene.SceneTracker.DateTime,
                                Location = scene.SceneTracker.Location,
                                Weather = scene.SceneTracker.Weather ?? "Unknown",
                                CharactersPresent = scene.SceneTracker.CharactersPresent?.ToArray() ?? []
                            };

                            memories.Add(new MemoryContext
                            {
                                MemoryContent = scene.Memory.Summary,
                                SceneTracker = sceneTracker,
                                Salience = scene.Memory.Salience,
                                Data = scene.Memory.ExtensionData
                            });

                            sceneRewrites.Add(new CharacterSceneContext
                            {
                                Content = scene.Narrative,
                                SequenceNumber = 0,
                                SceneTracker = sceneTracker
                            });
                        }

                        logger.Information("Created {MemoryCount} memories and {SceneCount} scene rewrites for {CharacterName}",
                            memories.Count,
                            sceneRewrites.Count,
                            character.Name);
                    }

                    var updatedCharacter = new CharacterContext
                    {
                        CharacterId = character.CharacterId,
                        Name = character.Name,
                        Description = character.Description,
                        Importance = character.Importance,
                        CharacterState = result.ProfileUpdates ?? character.CharacterState,
                        CharacterTracker = result.TrackerUpdates ?? character.CharacterTracker,
                        CharacterMemories = memories,
                        Relationships = [],
                        SceneRewrites = sceneRewrites,
                        SimulationMetadata = character.SimulationMetadata,
                        IsDead = false
                    };
                    var tracker = await characterTrackerAgent.InvokeAfterSimulation(context, character, updatedCharacter, context.NewTracker.Scene, cancellationToken);
                    updatedCharacter.CharacterTracker = tracker.Tracker;
                    updatedCharacter.IsDead = tracker.IsDead;
                    lock (context)
                    {
                        context.CharacterUpdates.Add(updatedCharacter);

                        foreach (var evt in events)
                        {
                            context.CharacterEventsToConsume.Add(evt.Id);
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "OffscreenInference failed for {CharacterName}", character.Name);
                    throw;
                }
            })
            .ToArray();

        await Task.WhenAll(tasks);
    }

    private async Task<OffscreenInferenceOutput> Simulate(GenerationContext context, OffscreenInferenceInput input, CancellationToken cancellationToken)
    {
        var kernelBuilder = await GetKernelBuilder(context);

        var systemPrompt = await GetPromptAsync(context);
        systemPrompt = PopulatePlaceholders(systemPrompt, input);

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(systemPrompt);
        chatHistory.AddUserMessage(BuildContextPrompt(input));
        chatHistory.AddUserMessage(BuildRequestPrompt(input));

        var kernel = kernelBuilder.Create().Build();

        var outputParser = ResponseParser.CreateJsonParser<OffscreenInferenceOutput>(
            "offscreen_inference",
            true);

        var promptExecutionSettings = kernelBuilder.GetDefaultFunctionPromptExecutionSettings();

        logger.Information("OffscreenInferenceAgent for {prompt: {prompt}", chatHistory.ToJsonString());
        return await agentKernel.SendRequestAsync(
            chatHistory,
            outputParser,
            promptExecutionSettings,
            nameof(OffscreenInferenceAgent),
            kernel,
            cancellationToken);
    }

    private string BuildContextPrompt(OffscreenInferenceInput input)
    {
        var jsonOptions = PromptSections.GetJsonOptions(true);

        return $"""
                <identity>
                {input.Character.CharacterState.ToJsonString(jsonOptions)}
                </identity>

                <physical_state>
                {input.Character.CharacterTracker?.ToJsonString(jsonOptions) ?? "{}"}
                </physical_state>

                <relationships>
                {FormatRelationships(input.Character)}
                </relationships>

                <world_events>
                {FormatWorldEvents(input.WorldEvents)}
                </world_events>

                <last_scenes>
                {BuildSceneHistoryContent(input.Character)}
                </last_scenes>
                """;
    }

    private static string BuildRequestPrompt(OffscreenInferenceInput input) =>
        $"""
         Live through the period: {input.TimeElapsed}

         What do you do? What happens? How are you affected?
         """;

    private static string FormatRelationships(CharacterContext character)
    {
        if (character.Relationships.Count == 0)
        {
            return "No established relationships.";
        }

        var sb = new StringBuilder();
        foreach (var rel in character.Relationships)
        {
            sb.AppendLine($"### {rel.TargetCharacterName}");
            sb.AppendLine($"**Dynamic:** {rel.Dynamic}");
            if (rel.Data.Count > 0)
            {
                sb.AppendLine($"**Details:** {rel.Data.ToJsonString(PromptSections.GetJsonOptions(true))}");
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static string FormatWorldEvents(object? worldEvents)
    {
        if (worldEvents == null)
        {
            return "No significant world momentum.";
        }

        return worldEvents.ToJsonString();
    }

    private static string BuildSceneHistoryContent(CharacterContext character)
    {
        if (character.SceneRewrites.Count == 0)
        {
            return "*No previous scenes recorded.*";
        }

        var sb = new StringBuilder();
        sb.AppendLine("These are scenes from your perspective (your memories of recent events). This simulation continues from where you left off.");
        sb.AppendLine();

        var recentScenes = character.SceneRewrites
            .OrderByDescending(s => s.SequenceNumber)
            .Take(10)
            .OrderBy(s => s.SequenceNumber)
            .ToList();

        foreach (var scene in recentScenes)
        {
            sb.AppendLine("---");
            sb.AppendLine($"**Scene {scene.SequenceNumber}**");
            if (scene.SceneTracker != null)
            {
                sb.AppendLine($"Time: {scene.SceneTracker.Time}");
                sb.AppendLine($"Location: {scene.SceneTracker.Location}");
            }

            sb.AppendLine();
            sb.AppendLine(scene.Content);
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private string PopulatePlaceholders(string prompt, OffscreenInferenceInput input)
    {
        var jsonOptions = PromptSections.GetJsonOptions(true);

        prompt = prompt.Replace(PlaceholderNames.CharacterName, input.Character.Name);

        prompt = prompt.Replace("{{core_profile}}", input.Character.CharacterState.ToJsonString(jsonOptions));

        prompt = prompt.Replace("{{last_state}}", input.Character.CharacterTracker?.ToJsonString(jsonOptions) ?? "{}");

        prompt = prompt.Replace("{{time_elapsed}}", input.TimeElapsed);

        prompt = prompt.Replace("{{world_events}}", FormatWorldEvents(input.WorldEvents));

        prompt = prompt.Replace("{{events_log}}", FormatEventsLog(input.EventsLog));

        return prompt;
    }

    private static string FormatEventsLog(List<CharacterEventDto> events)
    {
        if (events.Count == 0)
        {
            return "No events recorded affecting this character.";
        }

        var sb = new StringBuilder();
        foreach (var evt in events.OrderBy(e => e.Time))
        {
            sb.AppendLine($"**{evt.Time}** (from {evt.SourceCharacter}):");
            sb.AppendLine($"- Event: {evt.Event}");
            sb.AppendLine($"- Their read: {evt.SourceRead}");
            sb.AppendLine();
        }

        return sb.ToString();
    }
}