using System.Diagnostics;

using FableCraft.Application.NarrativeEngine.Agents;
using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;

using Microsoft.EntityFrameworkCore;

using Serilog;

namespace FableCraft.Application.NarrativeEngine.Workflow;

/// <summary>
/// Orchestrates off-screen character simulation after a scene ends.
/// Runs SimulationPlanner to determine which characters need simulation,
/// then executes standalone simulations for arc_important characters.
/// </summary>
internal sealed class SimulationOrchestrator(
    SimulationPlannerAgent plannerAgent,
    StandaloneSimulationAgent standaloneAgent,
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
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

        if (plan.Standalone is { Count: > 0 })
        {
            await RunStandaloneSimulations(context, plan, cancellationToken);
        }

        if (plan.Cohorts is { Count: > 0 })
        {
            logger.Information("Cohort simulations requested but not yet implemented. Cohorts: {Cohorts}",
                string.Join(", ", plan.Cohorts.Select(c => string.Join("+", c.Characters))));
        }
    }

    private async Task RunStandaloneSimulations(
        GenerationContext context,
        SimulationPlannerOutput plan,
        CancellationToken cancellationToken)
    {
        var profiledCharacterNames = context.Characters
            .Where(c => c.Importance == CharacterImportance.ArcImportance || c.Importance == CharacterImportance.Significant)
            .Select(c => c.Name)
            .ToArray();
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
                    WorldEvents = previousState?.StoryState.WorldMomentum,
                    ProfiledCharacterNames = profiledCharacterNames
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
                                SceneTracker = x.StoryTracker,
                            })
                            .ToList(),
                        Relationships = characterRelationships,
                        SceneRewrites = result.Scenes.Select(x => new CharacterSceneContext
                            {
                                Content = x.Narrative,
                                SequenceNumber = character.SceneRewrites.Max(s => s.SequenceNumber) + 1,
                                StoryTracker = x.StoryTracker
                            })
                            .ToList(),
                        Importance = character.Importance,
                        SimulationMetadata = new SimulationMetadata
                        {
                            LastSimulated = context.NewTracker!.Scene!.Time,
                            PotentialInteractions = result.PotentialInteractions,
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
        var characterEventsToSave = new List<CharacterEvent>();

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
                characterEventsToSave.AddRange(result.CharacterEvents.Select(ce => new CharacterEvent
                {
                    Id = Guid.NewGuid(),
                    AdventureId = context.AdventureId,
                    TargetCharacterName = ce.Character,
                    SourceCharacterName = result.Name,
                    Time = ce.Time,
                    Event = ce.Event,
                    SourceRead = ce.MyRead,
                    Consumed = false
                }));
            }
        }

        if (characterEventsToSave.Count > 0)
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            await dbContext.CharacterEvents.AddRangeAsync(characterEventsToSave, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            logger.Information("Saved {Count} character events from simulation", characterEventsToSave.Count);
        }
    }
}