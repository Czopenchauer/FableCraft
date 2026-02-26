using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;

using Microsoft.EntityFrameworkCore;

using Serilog;

namespace FableCraft.Application.NarrativeEngine.Agents;

/// <summary>
///     Coordinates standalone simulation for characters marked for inference.
///     Delegates actual simulation to StandaloneSimulationAgent for full simulation output.
/// </summary>
internal sealed class OffscreenInferenceAgent(
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    StandaloneSimulationAgent standaloneAgent,
    CharacterTrackerAgent characterTrackerAgent,
    CharacterContextGatherer characterContextGatherer,
    WorldInfoExtractorAgent worldInfoExtractorAgent,
    LoreCrafter loreCrafter,
    LocationCrafter locationCrafter,
    ItemCrafter itemCrafter,
    ILogger logger)
{
    public async Task Invoke(
        GenerationContext context,
        SimulationPlannerOutput plan,
        CancellationToken cancellationToken)
    {
        logger.Information("OffscreenInferenceProcessor: Starting...");

        var significantForInference = plan.Inference;
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

        logger.Information("Running standalone simulation for {Count} inference characters",
            significantForInference.Count);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

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

        var previousState = context.SceneContext?
            .OrderByDescending(x => x.SequenceNumber)
            .FirstOrDefault()?.Metadata.ChroniclerState;

        var gatheredContext = context.SceneContext?
            .OrderByDescending(x => x.SequenceNumber)
            .FirstOrDefault()?.Metadata.GatheredContext;

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

                if (currentSceneTracker.CharactersPresent.Contains(character.Name, StringComparer.OrdinalIgnoreCase))
                {
                    logger.Warning("Skipping offscreen simulation for {CharacterName} as they are already present on scene", character.Name);
                    return;
                }

                if (context.CharacterUpdates.Any(z => z.CharacterId == character.CharacterId))
                {
                    logger.Information("Skipping already simulated character: {CharacterName}", character.Name);
                    return;
                }

                var events = eventsByCharacter.GetValueOrDefault(character.Name, []);
                CharacterContext updatedCharacter;
                StandaloneSimulationOutput result;

                if (context.PendingReflectionCache.TryGetValue(character.CharacterId, out var cached)
                    && cached.Source == ReflectionSource.OffscreenInference)
                {
                    logger.Information("Using cached offscreen simulation for {CharacterName}", character.Name);
                    updatedCharacter = cached.Result;
                    result = new StandaloneSimulationOutput
                    {
                        Scenes = [],
                        RelationshipUpdates = [],
                        ProfileUpdates = updatedCharacter.CharacterState,
                        PendingMcInteraction = updatedCharacter.SimulationMetadata?.PendingMcInteraction,
                        WorldEventsEmitted = null,
                        CharacterEvents = null
                    };
                }
                else
                {
                    var input = new StandaloneSimulationInput
                    {
                        Character = character,
                        TimePeriod = context.NewTracker!.Scene,
                        WorldEvents = previousState?.WorldMomentum,
                        GatheredWorldContext = gatheredContext
                    };

                    try
                    {
                        logger.Information("Running standalone simulation for inference character {CharacterName}",
                            character.Name);

                        result = await standaloneAgent.Invoke(context, input, cancellationToken);

                        logger.Information(
                            "Standalone simulation complete for {CharacterName}: {SceneCount} scenes, {RelUpdates} relationship updates",
                            character.Name,
                            result.Scenes.Count,
                            result.RelationshipUpdates?.Count ?? 0);

                        var characterRelationships = new List<CharacterRelationshipContext>();
                        foreach (var relationshipUpdate in result.RelationshipUpdates ?? [])
                        {
                            if (relationshipUpdate.ExtensionData?.Count == 0)
                            {
                                logger.Warning("Character {CharacterName} has no relationships update!", character.Name);
                            }

                            var relationship = character.Relationships.SingleOrDefault(x => x.TargetCharacterName == relationshipUpdate.Name);
                            if (relationship == null)
                            {
                                characterRelationships.Add(new CharacterRelationshipContext
                                {
                                    TargetCharacterName = relationshipUpdate.Name,
                                    Data = relationshipUpdate.ExtensionData!,
                                    UpdateTime = input.TimePeriod!.Time,
                                    SequenceNumber = 0,
                                    Dynamic = relationshipUpdate.Dynamic
                                });
                            }
                            else if (relationshipUpdate.ExtensionData?.Count > 0)
                            {
                                var newRelationship = new CharacterRelationshipContext
                                {
                                    TargetCharacterName = relationship.TargetCharacterName,
                                    Data = relationshipUpdate.ExtensionData!,
                                    UpdateTime = input.TimePeriod!.Time,
                                    SequenceNumber = relationship.SequenceNumber + 1,
                                    Dynamic = relationshipUpdate.Dynamic
                                };
                                characterRelationships.Add(newRelationship);
                            }
                        }

                        updatedCharacter = new CharacterContext
                        {
                            CharacterId = character.CharacterId,
                            CharacterState = result.ProfileUpdates ?? character.CharacterState,
                            CharacterTracker = character.CharacterTracker,
                            Name = character.Name,
                            Description = character.Description,
                            CharacterMemories = result.Scenes.Select(x => new MemoryContext
                                {
                                    Salience = x.Memory.Salience,
                                    Data = x.Memory.ExtensionData!,
                                    MemoryContent = x.Memory.Summary,
                                    SceneTracker = x.SceneTracker
                                })
                                .ToList(),
                            Relationships = characterRelationships,
                            SceneRewrites = result.Scenes.Select((x, idx) => new CharacterSceneContext
                                {
                                    Content = x.Narrative,
                                    SequenceNumber = (character.SceneRewrites.Count > 0
                                        ? character.SceneRewrites.Max(s => s.SequenceNumber)
                                        : 0) + idx + 1,
                                    SceneTracker = x.SceneTracker
                                })
                                .ToList(),
                            Importance = character.Importance,
                            SimulationMetadata = new SimulationMetadata
                            {
                                LastSimulated = input.TimePeriod!.Time,
                                PendingMcInteraction = result.PendingMcInteraction
                            },
                            IsDead = false
                        };

                        lock (context)
                        {
                            context.PendingReflectionCache[character.CharacterId] = new CachedReflectionResult
                            {
                                CharacterId = character.CharacterId,
                                CharacterName = character.Name,
                                Source = ReflectionSource.OffscreenInference,
                                Result = updatedCharacter
                            };
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, "Standalone simulation failed for inference character {CharacterName}", character.Name);
                        throw;
                    }
                }

                try
                {
                    var characterSceneContext = BuildCharacterSceneContext(context, updatedCharacter);
                    var trackerTask = characterTrackerAgent.InvokeAfterSimulation(context, character, updatedCharacter, context.NewTracker!.Scene!, cancellationToken);
                    var creationTask = ProcessCreationRequests(context, result.CreationRequests, characterSceneContext, cancellationToken);
                    var worldInfoTask = ExtractWorldInfoFromSimulation(context, result.Scenes, character.Name, cancellationToken);

                    await Task.WhenAll(trackerTask, creationTask, GatherAndStoreCharacterContext(context, updatedCharacter, cancellationToken), worldInfoTask);

                    var tracker = await trackerTask;
                    updatedCharacter.CharacterTracker = tracker.Tracker;
                    updatedCharacter.IsDead = tracker.IsDead;

                    lock (context)
                    {
                        context.PendingReflectionCache.Remove(character.CharacterId);
                        context.CharacterUpdates.Add(updatedCharacter);

                        foreach (var evt in events)
                        {
                            context.CharacterEventsToConsume.Add(evt.Id);
                        }

                        if (result.WorldEventsEmitted is { Count: > 0 })
                        {
                            foreach (var worldEvent in result.WorldEventsEmitted)
                            {
                                context.NewWorldEvents.Add(new WorldEvent
                                {
                                    When = worldEvent.When,
                                    Where = worldEvent.Where,
                                    Event = worldEvent.Event
                                });
                            }
                        }

                        if (result.CharacterEvents is { Count: > 0 })
                        {
                            foreach (var ce in result.CharacterEvents)
                            {
                                context.NewCharacterEvents.Add(new CharacterEventToSave
                                {
                                    AdventureId = context.AdventureId,
                                    TargetCharacterName = ce.Character,
                                    SourceCharacterName = character.Name,
                                    Time = ce.Time,
                                    Event = ce.Event,
                                    SourceRead = ce.MyRead
                                });
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Post-simulation processing failed for {CharacterName}", character.Name);
                    throw;
                }
            })
            .ToArray();

        await Task.WhenAll(tasks);
    }

    private async Task ProcessCreationRequests(
        GenerationContext context,
        CreationRequests? requests,
        SceneContext[] sceneContext,
        CancellationToken cancellationToken)
    {
        if (requests == null)
        {
            return;
        }

        var tasks = new List<Task>();

        foreach (var loc in requests.Locations.Where(l => !l.Processed))
        {
            tasks.Add(Task.Run(async () =>
                {
                    var result = await locationCrafter.Invoke(context, loc, sceneContext, cancellationToken);
                    lock (context)
                    {
                        loc.Processed = true;
                        context.NewLocations = [.. (context.NewLocations ?? []), result];
                    }
                },
                cancellationToken));
        }

        foreach (var item in requests.Items.Where(i => !i.Processed))
        {
            tasks.Add(Task.Run(async () =>
                {
                    var result = await itemCrafter.Invoke(context, item, sceneContext, cancellationToken);
                    lock (context)
                    {
                        item.Processed = true;
                        context.NewItems = [.. (context.NewItems ?? []), result];
                    }
                },
                cancellationToken));
        }

        foreach (var lore in requests.Lore.Where(l => !l.Processed))
        {
            tasks.Add(Task.Run(async () =>
                {
                    var result = await loreCrafter.Invoke(context, lore, sceneContext, cancellationToken);
                    lock (context)
                    {
                        lore.Processed = true;
                        context.NewLore.Add(result);
                    }
                },
                cancellationToken));
        }

        if (tasks.Count > 0)
        {
            await Task.WhenAll(tasks);
            logger.Information("Processed {Count} creation requests from inference simulation", tasks.Count);
        }
    }

    private static SceneContext[] BuildCharacterSceneContext(GenerationContext context, CharacterContext characterContext)
    {
        var globalGatheredContext = context.SceneContext?
            .OrderByDescending(x => x.SequenceNumber)
            .FirstOrDefault()?.Metadata.GatheredContext;

        return characterContext.SceneRewrites
            .Select(scene => new SceneContext
            {
                SequenceNumber = scene.SequenceNumber,
                SceneContent = scene.Content,
                PlayerChoice = string.Empty,
                Metadata = new Metadata
                {
                    GatheredContext = globalGatheredContext,
                    Tracker = scene.SceneTracker != null
                        ? new Tracker { Scene = scene.SceneTracker }
                        : null
                }
            })
            .ToArray();
    }

    private async Task GatherAndStoreCharacterContext(
        GenerationContext context,
        CharacterContext characterContext,
        CancellationToken cancellationToken)
    {
        try
        {
            var gatheredContext = await characterContextGatherer.Invoke(context, characterContext, cancellationToken);

            var lastRewrite = characterContext.SceneRewrites
                .OrderByDescending(x => x.SequenceNumber)
                .FirstOrDefault();

            if (lastRewrite != null)
            {
                lastRewrite.GatheredContext = gatheredContext;
                logger.Information(
                    "Stored gathered context for {CharacterName} in scene rewrite #{SequenceNumber}",
                    characterContext.Name,
                    lastRewrite.SequenceNumber);
            }
        }
        catch (Exception ex)
        {
            logger.Warning(ex, "Failed to gather context for character {CharacterName}, continuing without it", characterContext.Name);
        }
    }

    private async Task ExtractWorldInfoFromSimulation(
        GenerationContext context,
        List<SimulationScene> scenes,
        string characterName,
        CancellationToken cancellationToken)
    {
        if (scenes.Count == 0)
            return;

        var tasks = scenes
            .Select((scene, index) => (scene, index, sourceKey: $"inference-simulation:{characterName}:{index}"))
            .Where(x =>
            {
                lock (context)
                {
                    if (context.ProcessedWorldInfoSources.Contains(x.sourceKey))
                    {
                        logger.Information("Skipping world info extraction from {Character} inference simulation scene {Index} (already processed)",
                            characterName, x.index);
                        return false;
                    }
                }

                return !string.IsNullOrEmpty(x.scene.Narrative) && x.scene.SceneTracker != null;
            })
            .Select(x => ExtractWorldInfoFromSingleScene(context, x.scene, x.sourceKey, characterName, cancellationToken))
            .ToArray();

        await Task.WhenAll(tasks);
    }

    private async Task ExtractWorldInfoFromSingleScene(
        GenerationContext context,
        SimulationScene scene,
        string sourceKey,
        string characterName,
        CancellationToken cancellationToken)
    {
        try
        {
            var alreadyHandled = BuildAlreadyHandledContent(context);
            var result = await worldInfoExtractorAgent.Invoke(context, scene.Narrative, scene.SceneTracker!, alreadyHandled, cancellationToken);
            logger.Information("Extracted {ActivityCount} activities from {Character} inference simulation",
                result.Activity.Count, characterName);

            lock (context)
            {
                context.WorldInfoExtractions ??= new WorldInfoExtractionOutput();
                context.WorldInfoExtractions.Activity.AddRange(result.Activity);
                context.ProcessedWorldInfoSources.Add(sourceKey);
            }
        }
        catch (Exception ex)
        {
            logger.Warning(ex, "Failed to extract world info from {Character} inference simulation scene, continuing without it", characterName);
        }
    }

    private static AlreadyHandledContent BuildAlreadyHandledContent(GenerationContext context)
    {
        var creationRequests = context.NewScene?.CreationRequests;
        return new AlreadyHandledContent
        {
            Characters = creationRequests?.Characters,
            Locations = creationRequests?.Locations,
            Items = creationRequests?.Items,
            Lore = creationRequests?.Lore,
            WorldEvents = context.NewWorldEvents,
            BackgroundCharacters = context.NewBackgroundCharacters
        };
    }
}
