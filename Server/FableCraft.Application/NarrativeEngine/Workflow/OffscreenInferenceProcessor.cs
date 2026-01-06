using FableCraft.Application.NarrativeEngine.Agents;
using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;

using Microsoft.EntityFrameworkCore;

using Serilog;

namespace FableCraft.Application.NarrativeEngine.Workflow;

/// <summary>
/// Runs OffscreenInference for significant characters that are likely to appear in the next scene.
/// Uses significant_for_inference from SimulationPlannerOutput to determine which characters need inference.
/// Consumes CharacterEvents logged by arc_important character simulations.
/// </summary>
internal sealed class OffscreenInferenceProcessor(
    OffscreenInferenceAgent inferenceAgent,
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    ILogger logger) : IProcessor
{
    public async Task Invoke(GenerationContext context, CancellationToken cancellationToken)
    {
        var significantForInference = context.SimulationPlan?.SignificantForInference;
        if (significantForInference is not { Count: > 0 })
        {
            logger.Debug("OffscreenInferenceProcessor: No significant characters need inference");
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

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        // Get character names that need inference
        var characterNames = significantForInference.Select(s => s.Character).ToList();

        // Query all unconsumed events for these characters
        var eventsByCharacter = await dbContext.CharacterEvents
            .Where(e => e.AdventureId == context.AdventureId
                        && characterNames.Contains(e.TargetCharacterName)
                        && !e.Consumed)
            .GroupBy(e => e.TargetCharacterName)
            .ToDictionaryAsync(
                g => g.Key,
                g => g.ToList(),
                cancellationToken);

        foreach (var significantEntry in significantForInference)
        {
            var character = context.Characters.FirstOrDefault(c => c.Name == significantEntry.Character);
            if (character == null)
            {
                logger.Warning("OffscreenInference requested for unknown character: {CharacterName}",
                    significantEntry.Character);
                continue;
            }

            // Get events for this character
            var events = eventsByCharacter.GetValueOrDefault(character.Name, []);
            var eventDtos = events.Select(e => new CharacterEventDto
            {
                Time = e.Time,
                Event = e.Event,
                SourceCharacter = e.SourceCharacterName,
                SourceRead = e.SourceRead
            }).ToList();

            // Calculate time elapsed (from last simulation or last known state update)
            var lastSimulated = character.SimulationMetadata?.LastSimulated;
            var timeElapsed = CalculateTimeElapsed(lastSimulated, currentSceneTracker.Time);

            var input = new OffscreenInferenceInput
            {
                Character = character,
                EventsLog = eventDtos,
                TimeElapsed = timeElapsed,
                CurrentDateTime = currentSceneTracker.Time ?? "Unknown",
                WorldEvents = context.SceneContext?
                    .OrderByDescending(x => x.SequenceNumber)
                    .FirstOrDefault()?.Metadata.ChroniclerState?.StoryState.WorldMomentum
            };

            try
            {
                logger.Information("Running OffscreenInference for {CharacterName} (events: {EventCount})",
                    character.Name, eventDtos.Count);

                var result = await inferenceAgent.Invoke(context, input, cancellationToken);

                logger.Information("OffscreenInference complete for {CharacterName}: {Location}, {Activity}",
                    character.Name,
                    result.CurrentSituation.Location,
                    result.CurrentSituation.Activity);

                // Include location in tracker updates if not already present
                var trackerUpdates = result.TrackerUpdates ?? new Dictionary<string, object>();
                if (!trackerUpdates.ContainsKey("Location"))
                {
                    trackerUpdates["Location"] = result.CurrentSituation.Location;
                }

                // Convert scenes to memories and scene rewrites
                var memories = new List<MemoryContext>();
                var sceneRewrites = new List<CharacterSceneContext>();

                if (result.Scenes is { Count: > 0 })
                {
                    foreach (var scene in result.Scenes)
                    {
                        var sceneTracker = new SceneTracker
                        {
                            Time = scene.StoryTracker.DateTime,
                            Location = scene.StoryTracker.Location,
                            Weather = scene.StoryTracker.Weather ?? "Unknown",
                            CharactersPresent = scene.StoryTracker.CharactersPresent?.ToArray() ?? []
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
                            StoryTracker = sceneTracker
                        });
                    }

                    logger.Debug("Created {MemoryCount} memories and {SceneCount} scene rewrites for {CharacterName}",
                        memories.Count, sceneRewrites.Count, character.Name);
                }

                // Create updated character context with patched state
                var updatedCharacter = new CharacterContext
                {
                    CharacterId = character.CharacterId,
                    Name = character.Name,
                    Description = character.Description,
                    Importance = character.Importance,
                    CharacterState = character.CharacterState.PatchWith(result.ProfileUpdates ?? new Dictionary<string, object>()),
                    CharacterTracker = character.CharacterTracker.PatchWith(trackerUpdates),
                    CharacterMemories = memories,
                    Relationships = [], // No relationship changes from inference
                    SceneRewrites = sceneRewrites,
                    SimulationMetadata = character.SimulationMetadata // Keep existing metadata
                };

                context.CharacterUpdates.Enqueue(updatedCharacter);

                // Queue events for consumption in SaveEnrichmentStep
                foreach (var evt in events)
                {
                    context.CharacterEventsToConsume.Enqueue(evt.Id);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "OffscreenInference failed for {CharacterName}", character.Name);
                // Continue with other characters even if one fails
            }
        }
    }

    private static string CalculateTimeElapsed(string? lastSimulated, string? currentTime)
    {
        // Simple fallback - in a production system you'd parse the in-world dates
        if (string.IsNullOrEmpty(lastSimulated))
        {
            return "Unknown (first inference)";
        }

        if (string.IsNullOrEmpty(currentTime))
        {
            return "Unknown";
        }

        // For now, just return a descriptive string
        return $"Since {lastSimulated}";
    }
}
