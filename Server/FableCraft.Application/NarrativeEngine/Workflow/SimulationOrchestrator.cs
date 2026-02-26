using System.Diagnostics;

using FableCraft.Application.NarrativeEngine.Agents;
using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;

using Serilog;

namespace FableCraft.Application.NarrativeEngine.Workflow;

/// <summary>
///     Orchestrates off-screen character simulation after a scene ends.
///     Runs SimulationPlanner to determine which characters need simulation,
///     then executes standalone simulations for arc_important characters
///     and cohort simulations for characters who interact together.
/// </summary>
internal sealed class SimulationOrchestrator(
    SimulationPlannerAgent plannerAgent,
    StandaloneSimulationAgent standaloneAgent,
    SimulationModeratorAgent cohortModeratorAgent,
    OffscreenInferenceAgent offscreenInferenceAgent,
    CharacterTrackerAgent characterTrackerAgent,
    CharacterContextGatherer characterContextGatherer,
    WorldInfoExtractorAgent worldInfoExtractorAgent,
    LoreCrafter loreCrafter,
    LocationCrafter locationCrafter,
    ItemCrafter itemCrafter,
    ILogger logger) : IProcessor
{
    public async Task Invoke(GenerationContext context, CancellationToken cancellationToken)
    {
        if (context.SkipSimulation)
        {
            logger.Information("SimulationOrchestrator: Skipping simulation (not selected for regeneration)");
            return;
        }

        var sceneTracker = context.NewTracker?.Scene;
        if (sceneTracker == null)
        {
            logger.Warning("SimulationOrchestrator: No scene tracker available, skipping simulation");
            throw new UnreachableException("Scene tracker should be available at this point in the workflow");
        }

        if (!context.Characters.Any())
        {
            return;
        }

        logger.Information("Running SimulationPlanner...");
        var plan = await plannerAgent.Invoke(context, sceneTracker, cancellationToken);
        context.SimulationPlan ??= plan;

        var charactersInScene = context.NewTracker?.Scene?.CharactersPresent ?? [];
        foreach (var standaloneSimulation in plan.Standalone ?? [])
        {
            if (charactersInScene.Contains(standaloneSimulation.Character))
            {
                logger.Error("SimulationPlanner: Simulating character {CharacterName} BUT they were present in the previous scene. Skipping", standaloneSimulation.Character);
            }
        }

        plan.Standalone = plan.Standalone?.Where(x => !charactersInScene.Contains(x.Character)).ToList() ?? [];

        logger.Information(
            "SimulationPlanner: Simulation needed. Cohorts={CohortCount}, Standalone={StandaloneCount}, Skip={SkipCount}",
            string.Join(", ", plan.Cohorts?.Select(c => string.Join("+", c.Characters)) ?? []),
            string.Join(", ", string.Join("+", plan.Standalone?.Select(c => c.Character) ?? [])),
            string.Join(", ", string.Join("+", plan.Skip?.Select(c => c.Character) ?? [])));

        await Task.WhenAll(RunStandaloneSimulations(context, plan, cancellationToken),
            RunCohortSimulations(context, plan, cancellationToken),
            offscreenInferenceAgent.Invoke(context, plan, cancellationToken));
    }

    private async Task RunStandaloneSimulations(
        GenerationContext context,
        SimulationPlannerOutput plan,
        CancellationToken cancellationToken)
    {
        if ((plan.Standalone?.Count ?? 0) == 0)
        {
            return;
        }

        var previousState = context.SceneContext?
            .OrderByDescending(x => x.SequenceNumber)
            .FirstOrDefault()?.Metadata.ChroniclerState;

        var gatheredContext = GetPreviousGatheredContext(context);

        var simulationTasks = plan.Standalone!
            .Select(async standalone =>
            {
                var character = context.Characters.FirstOrDefault(c => c.Name == standalone.Character);
                if (character == null)
                {
                    logger.Warning("Standalone simulation requested for unknown character: {CharacterName}",
                        standalone.Character);
                    throw new UnreachableException($"Requested simulation for unknown character: {standalone.Character}");
                }

                if (context.CharacterUpdates.Any(c => c.Name == character.Name))
                {
                    logger.Information("Skipping already simulated standalone character: {CharacterName}", character.Name);
                    return default;
                }

                CharacterContext characterContext;
                StandaloneSimulationOutput result;

                if (context.PendingReflectionCache.TryGetValue(character.CharacterId, out var cached)
                    && cached.Source == ReflectionSource.StandaloneSimulation)
                {
                    logger.Information("Using cached standalone simulation for {CharacterName}", character.Name);
                    characterContext = cached.Result;
                    result = new StandaloneSimulationOutput
                    {
                        Scenes = [],
                        RelationshipUpdates = [],
                        ProfileUpdates = characterContext.CharacterState,
                        PendingMcInteraction = characterContext.SimulationMetadata?.PendingMcInteraction,
                        WorldEventsEmitted = null,
                        CharacterEvents = null
                    };
                }
                else
                {
                    logger.Information("Running standalone simulation for {CharacterName}...", character.Name);

                    var input = new StandaloneSimulationInput
                    {
                        Character = character,
                        TimePeriod = context.NewTracker!.Scene,
                        WorldEvents = previousState?.WorldMomentum,
                        GatheredWorldContext = gatheredContext
                    };

                    try
                    {
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

                        characterContext = new CharacterContext
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
                                Source = ReflectionSource.StandaloneSimulation,
                                Result = characterContext
                            };
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, "Standalone simulation failed for {CharacterName}", character.Name);
                        throw;
                    }
                }

                try
                {
                    var characterSceneContext = BuildCharacterSceneContext(context, characterContext);
                    var trackerTask = characterTrackerAgent.InvokeAfterSimulation(context, character, characterContext, context.NewTracker!.Scene!, cancellationToken);
                    var creationTask = ProcessCreationRequests(context, result.CreationRequests, characterSceneContext, cancellationToken);
                    var worldInfoTask = ExtractWorldInfoFromSimulation(context, result.Scenes, character.Name, cancellationToken);

                    await Task.WhenAll(trackerTask, creationTask, GatherAndStoreCharacterContext(context, characterContext, cancellationToken), worldInfoTask);

                    var tracker = await trackerTask;
                    characterContext.CharacterTracker = tracker.Tracker;
                    characterContext.IsDead = tracker.IsDead;

                    lock (context)
                    {
                        context.PendingReflectionCache.Remove(character.CharacterId);
                    }

                    return (characterContext, result.WorldEventsEmitted, result.CharacterEvents, character.Name);
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Standalone simulation tracker failed for {CharacterName}", character.Name);
                    throw;
                }
            })
            .ToArray();

        var results = await Task.WhenAll(simulationTasks);

        foreach (var result in results)
        {
            if (result.characterContext == null)
            {
                continue;
            }

            var worldEvents = result.WorldEventsEmitted?.Select(x => new WorldEvent
                                  {
                                      When = x.When,
                                      Where = x.Where,
                                      Event = x.Event
                                  })
                                  .ToList()
                              ?? [];

            lock (context)
            {
                context.CharacterUpdates.Add(result.characterContext);
                foreach (var @event in worldEvents)
                {
                    context.NewWorldEvents.Add(@event);
                }

                if (result.CharacterEvents is { Count: > 0 })
                {
                    foreach (var ce in result.CharacterEvents)
                    {
                        context.NewCharacterEvents.Add(new CharacterEventToSave
                        {
                            AdventureId = context.AdventureId,
                            TargetCharacterName = ce.Character,
                            SourceCharacterName = result.Name,
                            Time = ce.Time,
                            Event = ce.Event,
                            SourceRead = ce.MyRead
                        });
                    }
                }
            }
        }
    }

    private async Task RunCohortSimulations(
        GenerationContext context,
        SimulationPlannerOutput plan,
        CancellationToken cancellationToken)
    {
        if ((plan.Cohorts?.Count ?? 0) == 0)
        {
            return;
        }

        var previousState = context.SceneContext?
            .OrderByDescending(x => x.SequenceNumber)
            .FirstOrDefault()?.Metadata.ChroniclerState;

        var charactersInScene = context.NewTracker?.Scene?.CharactersPresent ?? [];

        var gatheredContext = GetPreviousGatheredContext(context);

        var cohortTasks = plan.Cohorts!.Select(async cohort =>
        {
            // Filter out characters who were present in the scene
            var validCharacters = cohort.Characters
                .Where(c => !charactersInScene.Contains(c))
                .ToList();

            if (validCharacters.Count < 2)
            {
                logger.Warning(
                    "Cohort simulation requires at least 2 characters, but only {Count} valid after filtering. Skipping cohort: {Characters}",
                    validCharacters.Count,
                    string.Join(", ", cohort.Characters));
                return;
            }

            logger.Information("Running cohort simulation for: {Characters}", string.Join(", ", validCharacters));

            var significantCharacters = context.Characters
                .Where(c => c.Importance == CharacterImportance.Significant && !validCharacters.Contains(c.Name))
                .Select(c => $"{c.Name} - {c.Description}")
                .ToArray();

            var input = new CohortSimulationInput
            {
                CohortMembers = context.Characters.Where(x => validCharacters.Contains(x.Name)).ToArray(),
                SimulationPeriod = context.NewTracker!.Scene!.Time!,
                KnownInteractions = cohort.ExtensionData,
                WorldEvents = previousState?.WorldMomentum,
                SignificantCharacters = significantCharacters.Length > 0 ? significantCharacters : null,
            };

            try
            {
                var result = await cohortModeratorAgent.Invoke(context, input, cancellationToken);
                await Task.WhenAll(result.CharacterReflections.Select(x =>
                {
                    var characterName = x.Key;
                    var reflection = x.Value;
                    var character = context.Characters.FirstOrDefault(c => c.Name == characterName);
                    if (character == null)
                    {
                        logger.Warning("Character {CharacterName} not found for reflection processing", characterName);
                        return null;
                    }

                    if (context.CharacterUpdates.Any(c => c.Name == character.Name))
                    {
                        logger.Information("Skipping already simulated cohort character: {CharacterName}", character.Name);
                        return null;
                    }

                    return ProcessSimulationOutput(
                        result,
                        character,
                        reflection,
                        context,
                        cancellationToken);
                }).Where(x => x is not null).ToArray()!);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Cohort simulation failed for characters: {Characters}", string.Join(", ", validCharacters));
                throw;
            }
        }).ToArray();

        await Task.WhenAll(cohortTasks);
    }

    /// <summary>
    ///     Process a StandaloneSimulationOutput into updates for persistence.
    ///     Used by both standalone and cohort simulations.
    /// </summary>
    private async Task ProcessSimulationOutput(
        CohortSimulationResult cohortSimulationResult,
        CharacterContext character,
        StandaloneSimulationOutput result,
        GenerationContext context,
        CancellationToken cancellationToken)
    {
        CharacterContext characterContext;

        if (context.PendingReflectionCache.TryGetValue(character.CharacterId, out var cached)
            && cached.Source == ReflectionSource.CohortSimulation)
        {
            logger.Information("Using cached cohort simulation for {CharacterName}", character.Name);
            characterContext = cached.Result;
        }
        else
        {
            var characterRelationships = new List<CharacterRelationshipContext>();
            foreach (var relationshipUpdate in result.RelationshipUpdates ?? [])
            {
                if (relationshipUpdate.ExtensionData?.Count == 0)
                {
                    logger.Warning("Character {CharacterName} has no relationship update data!", character.Name);
                    continue;
                }

                var relationship = character.Relationships.SingleOrDefault(x => x.TargetCharacterName == relationshipUpdate.Name);
                if (relationship == null)
                {
                    characterRelationships.Add(new CharacterRelationshipContext
                    {
                        TargetCharacterName = relationshipUpdate.Name,
                        Data = relationshipUpdate.ExtensionData!,
                        UpdateTime = context.NewTracker!.Scene!.Time,
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
                        UpdateTime = context.NewTracker!.Scene!.Time,
                        SequenceNumber = relationship.SequenceNumber + 1,
                        Dynamic = relationshipUpdate.Dynamic
                    };
                    characterRelationships.Add(newRelationship);
                }
            }

            characterContext = new CharacterContext
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
                    LastSimulated = cohortSimulationResult.Result.SimulationPeriod.To,
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
                    Source = ReflectionSource.CohortSimulation,
                    Result = characterContext
                };
            }
        }

        var characterSceneContext = BuildCharacterSceneContext(context, characterContext);
        var trackerTask = characterTrackerAgent.InvokeAfterSimulation(context, character, characterContext, context.NewTracker!.Scene!, cancellationToken);
        var creationTask = ProcessCreationRequests(context, result.CreationRequests, characterSceneContext, cancellationToken);
        var worldInfoTask = ExtractWorldInfoFromSimulation(context, result.Scenes, character.Name, cancellationToken);

        await Task.WhenAll(trackerTask, creationTask, GatherAndStoreCharacterContext(context, characterContext, cancellationToken), worldInfoTask);

        var tracker = await trackerTask;
        characterContext.CharacterTracker = tracker.Tracker;
        characterContext.IsDead = tracker.IsDead;

        lock (context)
        {
            context.PendingReflectionCache.Remove(character.CharacterId);
        }

        var worldEvents = result.WorldEventsEmitted?
                              .Select(x => new WorldEvent
                              {
                                  When = x.When,
                                  Where = x.Where,
                                  Event = x.Event
                              })
                              .ToList()
                          ?? [];

        var characterEvents = new List<CharacterEventToSave>();
        if (result.CharacterEvents is { Count: > 0 })
        {
            foreach (var ce in result.CharacterEvents)
            {
                characterEvents.Add(new CharacterEventToSave
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

        lock (context)
        {
            context.CharacterUpdates.Add(characterContext);

            context.NewWorldEvents.AddRange(worldEvents);

            context.NewCharacterEvents.AddRange(characterEvents);
        }
    }

    private async Task ProcessCreationRequests(GenerationContext context, CreationRequests? requests, SceneContext[] sceneContext, CancellationToken cancellationToken)
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
            logger.Information("Processed {Count} creation requests from simulation", tasks.Count);
        }
    }

    /// <summary>
    ///     Gets the gathered context from the previous scene.
    ///     This provides world knowledge for simulation agents.
    /// </summary>
    private static GatheredContext? GetPreviousGatheredContext(GenerationContext context)
    {
        return context.SceneContext?
            .OrderByDescending(x => x.SequenceNumber)
            .FirstOrDefault()?.Metadata.GatheredContext;
    }

    /// <summary>
    ///     Builds SceneContext array from a character's SceneRewrites.
    ///     Uses the character's scene content but borrows GatheredContext from global context
    ///     to preserve world knowledge for crafters.
    /// </summary>
    private static SceneContext[] BuildCharacterSceneContext(GenerationContext context, CharacterContext characterContext)
    {
        // Get the GatheredContext from global context to preserve world knowledge
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

    /// <summary>
    ///     Runs CharacterContextGatherer for a character and stores the result
    ///     in their most recent scene rewrite for use in the next simulation.
    /// </summary>
    private async Task GatherAndStoreCharacterContext(
        GenerationContext context,
        CharacterContext characterContext,
        CancellationToken cancellationToken)
    {
        try
        {
            var gatheredContext = await characterContextGatherer.Invoke(context, characterContext, cancellationToken);

            // Store in the character's last scene rewrite
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
            .Select((scene, index) => (scene, index, sourceKey: $"simulation:{characterName}:{index}"))
            .Where(x =>
            {
                lock (context)
                {
                    if (context.ProcessedWorldInfoSources.Contains(x.sourceKey))
                    {
                        logger.Information("Skipping world info extraction from {Character} simulation scene {Index} (already processed)",
                            characterName, x.index);
                        return false;
                    }
                }

                return !string.IsNullOrEmpty(x.scene.Narrative) && x.scene.SceneTracker != null;
            })
            .Select(x => ExtractWorldInfoFromSingleSimulationScene(context, x.scene, x.sourceKey, characterName, cancellationToken))
            .ToArray();

        await Task.WhenAll(tasks);
    }

    private async Task ExtractWorldInfoFromSingleSimulationScene(
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
            logger.Information("Extracted {ActivityCount} activities from {Character} simulation",
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
            logger.Warning(ex, "Failed to extract world info from {Character} simulation scene, continuing without it", characterName);
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