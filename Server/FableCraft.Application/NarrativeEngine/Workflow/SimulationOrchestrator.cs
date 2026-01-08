using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

using FableCraft.Application.NarrativeEngine.Agents;
using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;

using Serilog;

namespace FableCraft.Application.NarrativeEngine.Workflow;

/// <summary>
/// Orchestrates off-screen character simulation after a scene ends.
/// Runs SimulationPlanner to determine which characters need simulation,
/// then executes standalone simulations for arc_important characters
/// and cohort simulations for characters who interact together.
/// </summary>
[Experimental("SKEXP0110")]
internal sealed class SimulationOrchestrator(
    SimulationPlannerAgent plannerAgent,
    StandaloneSimulationAgent standaloneAgent,
    SimulationModeratorAgent cohortModeratorAgent,
    ILogger logger) : IProcessor
{
    public async Task Invoke(GenerationContext context, CancellationToken cancellationToken)
    {
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

        if (plan.SimulationNeeded != true)
        {
            logger.Information("SimulationPlanner: No simulation needed - {Reason}", plan.Reason ?? "no reason provided");
            return;
        }

        var charactersInScene = context.NewTracker?.Scene?.CharactersPresent ?? [];
        foreach (StandaloneSimulation standaloneSimulation in plan.Standalone ?? [])
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

        await Task.WhenAll(RunStandaloneSimulations(context, plan, cancellationToken), RunCohortSimulations(context, plan, cancellationToken));
    }

    private async Task RunStandaloneSimulations(
        GenerationContext context,
        SimulationPlannerOutput plan,
        CancellationToken cancellationToken)
    {
        if((plan.Standalone?.Count ?? 0) == 0)
        {
            return;
        }

        var previousState = context.SceneContext?
            .OrderByDescending(x => x.SequenceNumber)
            .FirstOrDefault()?.Metadata.ChroniclerState;

        var simulationTasks = plan.Standalone!
            .Select(async standalone =>
            {
                var character = context.Characters.FirstOrDefault(c => c.Name == standalone.Character);
                if (character == null)
                {
                    logger.Warning("Standalone simulation requested for unknown character: {CharacterName}",
                        standalone.Character);
                    throw new UnreachableException("Requested simulation for unknown character");
                }

                logger.Information("Running standalone simulation for {CharacterName}...", character.Name);

                var input = new StandaloneSimulationInput
                {
                    Character = character,
                    TimePeriod = plan.SimulationPeriod.ToJsonString(),
                    WorldEvents = previousState?.StoryState.WorldMomentum
                };

                try
                {
                    var result = await standaloneAgent.Invoke(context, input, cancellationToken);
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
                                UpdateTime = plan.SimulationPeriod!.To,
                                SequenceNumber = 0,
                                Dynamic = relationshipUpdate.Dynamic,
                            });
                        }
                        else if (relationshipUpdate.ExtensionData?.Count > 0)
                        {
                            var updatedRelationship = relationship.Data.PatchWith(relationshipUpdate.ExtensionData);
                            var newRelationship = new CharacterRelationshipContext
                            {
                                TargetCharacterName = relationship.TargetCharacterName,
                                Data = updatedRelationship,
                                UpdateTime = plan.SimulationPeriod!.To,
                                SequenceNumber = relationship.SequenceNumber + 1,
                                Dynamic = relationshipUpdate.Dynamic,
                            };
                            characterRelationships.Add(newRelationship);
                        }
                    }

                    var characterContext = new CharacterContext
                    {
                        CharacterId = character.CharacterId,
                        CharacterState = character.CharacterState.PatchWith(result.ProfileUpdates ?? []),
                        CharacterTracker = character.CharacterTracker.PatchWith(result.TrackerUpdates ?? []),
                        Name = character.Name,
                        Description = character.Description,
                        CharacterMemories = result.Scenes.Select(x => new MemoryContext
                            {
                                Salience = x.Memory.Salience,
                                Data = x.Memory.ExtensionData!,
                                MemoryContent = x.Memory.Summary,
                                SceneTracker = x.SceneTracker,
                            })
                            .ToList(),
                        Relationships = characterRelationships,
                        SceneRewrites = result.Scenes.Select(x => new CharacterSceneContext
                            {
                                Content = x.Narrative,
                                SequenceNumber = character.SceneRewrites.Count > 0
                                    ? character.SceneRewrites.Max(s => s.SequenceNumber) + 1
                                    : 1,
                                SceneTracker = x.SceneTracker
                            })
                            .ToList(),
                        Importance = character.Importance,
                        SimulationMetadata = new SimulationMetadata
                        {
                            LastSimulated = plan.SimulationPeriod!.To,
                            PendingMcInteraction = result.PendingMcInteraction
                        }
                    };
                    return (characterContext, result.WorldEventsEmitted, result.CharacterEvents, character.Name);
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Standalone simulation failed for {CharacterName}", character.Name);
                    throw;
                }
            })
            .ToArray();

        var results = await Task.WhenAll(simulationTasks);

        foreach (var result in results)
        {
            context.CharacterUpdates.Enqueue(result.characterContext);
            var worldEvents = result.WorldEventsEmitted?.Select(x => new WorldEvent
                              {
                                  When = x.When,
                                  Where = x.Where,
                                  Event = x.Event
                              })
                              ?? [];
            foreach (var @event in worldEvents)
            {
                context.NewWorldEvents.Enqueue(@event);
            }

            if (result.CharacterEvents is { Count: > 0 })
            {
                foreach (var ce in result.CharacterEvents)
                {
                    context.NewCharacterEvents.Enqueue(new CharacterEventToSave
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
                .Select(c => $"{c.Name} - {c.Description ?? "No description available"}")
                .ToArray();

            var input = new CohortSimulationInput
            {
                CohortMembers = context.Characters.Where(x => validCharacters.Contains(x.Name)).ToArray(),
                SimulationPeriod = plan.SimulationPeriod!,
                KnownInteractions = cohort.ExtensionData,
                WorldEvents = previousState?.StoryState.WorldMomentum,
                SignificantCharacters = significantCharacters.Length > 0 ? significantCharacters : null
            };

            try
            {
                var result = await cohortModeratorAgent.Invoke(context, input, cancellationToken);

                foreach (var (characterName, reflection) in result.CharacterReflections)
                {
                    var character = context.Characters.FirstOrDefault(c => c.Name == characterName);
                    if (character == null)
                    {
                        logger.Warning("Character {CharacterName} not found for reflection processing", characterName);
                        continue;
                    }

                    var processedResult = ProcessSimulationOutput(
                        result,
                        character,
                        reflection,
                        plan,
                        context);

                    context.CharacterUpdates.Enqueue(processedResult.CharacterContext);

                    foreach (var worldEvent in processedResult.WorldEvents)
                    {
                        context.NewWorldEvents.Enqueue(worldEvent);
                    }

                    foreach (var charEvent in processedResult.CharacterEvents)
                    {
                        context.NewCharacterEvents.Enqueue(charEvent);
                    }
                }
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
    /// Process a StandaloneSimulationOutput into updates for persistence.
    /// Used by both standalone and cohort simulations.
    /// </summary>
    private (CharacterContext CharacterContext, List<WorldEvent> WorldEvents, List<CharacterEventToSave> CharacterEvents)
        ProcessSimulationOutput(
            CohortSimulationResult cohortSimulationResult, 
            CharacterContext character,
            StandaloneSimulationOutput result,
            SimulationPlannerOutput plan,
            GenerationContext context)
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
                    UpdateTime = plan.SimulationPeriod!.To,
                    SequenceNumber = 0,
                    Dynamic = relationshipUpdate.Dynamic,
                });
            }
            else if (relationshipUpdate.ExtensionData?.Count > 0)
            {
                var updatedRelationship = relationship.Data.PatchWith(relationshipUpdate.ExtensionData);
                var newRelationship = new CharacterRelationshipContext
                {
                    TargetCharacterName = relationship.TargetCharacterName,
                    Data = updatedRelationship,
                    UpdateTime = plan.SimulationPeriod!.To,
                    SequenceNumber = relationship.SequenceNumber + 1,
                    Dynamic = relationshipUpdate.Dynamic,
                };
                characterRelationships.Add(newRelationship);
            }
        }

        var characterContext = new CharacterContext
        {
            CharacterId = character.CharacterId,
            CharacterState = character.CharacterState.PatchWith(result.ProfileUpdates ?? []),
            CharacterTracker = character.CharacterTracker.PatchWith(result.TrackerUpdates ?? []),
            Name = character.Name,
            Description = character.Description,
            CharacterMemories = result.Scenes.Select(x => new MemoryContext
                {
                    Salience = x.Memory.Salience,
                    Data = x.Memory.ExtensionData!,
                    MemoryContent = x.Memory.Summary,
                    SceneTracker = x.SceneTracker,
                })
                .ToList(),
            Relationships = characterRelationships,
            SceneRewrites = result.Scenes.Select(x => new CharacterSceneContext
                {
                    Content = x.Narrative,
                    SequenceNumber = character.SceneRewrites.Count > 0
                        ? character.SceneRewrites.Max(s => s.SequenceNumber) + 1
                        : 1,
                    SceneTracker = x.SceneTracker
                })
                .ToList(),
            Importance = character.Importance,
            SimulationMetadata = new SimulationMetadata
            {
                LastSimulated = cohortSimulationResult.Result.SimulationPeriod.To,
                PendingMcInteraction = result.PendingMcInteraction
            }
        };

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

        return (characterContext, worldEvents, characterEvents);
    }
}