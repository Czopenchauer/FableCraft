using System.Text.Json;

using FableCraft.Application.NarrativeEngine.Agents;
using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;

using Serilog;

namespace FableCraft.Application.NarrativeEngine.Workflow;

/// <summary>
/// Processor responsible for character tracking: main character tracker, character reflection,
/// character tracker updates, and chronicler. Depends on SceneTrackerProcessor having run first.
/// </summary>
internal sealed class CharacterTrackersProcessor(
    MainCharacterTrackerAgent mainCharacterTrackerAgent,
    CharacterReflectionAgent characterReflectionAgent,
    CharacterTrackerAgent characterTrackerAgent,
    InitMainCharacterTrackerAgent initMainCharacterTrackerAgent,
    ChroniclerAgent chroniclerAgent,
    LoreCrafter loreCrafter,
    ILogger logger) : IProcessor
{
    public async Task Invoke(GenerationContext context, CancellationToken cancellationToken)
    {
        var storyTrackerResult = context.NewTracker?.Scene
                                 ?? throw new InvalidOperationException(
                                     "CharacterTrackersProcessor requires context.NewTracker.Scene to be populated. "
                                     + "Ensure SceneTrackerProcessor runs before this processor.");

        var chroniclerTask = ProcessChronicler(context, storyTrackerResult, cancellationToken);

        var mainCharTrackerTask = context.NewTracker?.MainCharacter?.MainCharacter != null
            ? Task.FromResult(context.NewTracker.MainCharacter)
            : ProcessMainChar(context, storyTrackerResult, cancellationToken);

        HashSet<string> alreadyProcessedCharacters;
        lock (context)
        {
            alreadyProcessedCharacters = (context.CharacterUpdates?.Select(x => x.Name) ?? [])
                .Concat(context.NewCharacters?.Select(x => x.Name) ?? [])
                .ToHashSet();
        }

        Task<CharacterContext?>[] characterUpdateTask = [];
        if (context.Characters.Count != 0)
        {
            characterUpdateTask = context.Characters
                .Where(x => storyTrackerResult.CharactersPresent.Contains(x.Name))
                .Select(async character =>
                {
                    // Skip if this character was already processed in a previous attempt
                    if (alreadyProcessedCharacters.Contains(character.Name))
                    {
                        return null;
                    }

                    var reflectionOutput = await characterReflectionAgent.Invoke(context, character, storyTrackerResult, cancellationToken);

                    var characterState = character.CharacterState;

                    if (reflectionOutput.ProfileUpdates.Count > 0)
                    {
                        characterState = characterState.PatchWith(reflectionOutput.ProfileUpdates);
                    }
                    else
                    {
                        logger.Warning("Character reflection output for character {CharacterName} has no update for character state.", character.Name);
                    }

                    var characterRelationships = new List<CharacterRelationshipContext>();
                    foreach (CharacterRelationshipOutput reflectionOutputRelationshipUpdate in reflectionOutput.RelationshipUpdates)
                    {
                        if (reflectionOutputRelationshipUpdate.ExtensionData?.Count == 0)
                        {
                            logger.Warning("Character {CharacterName} has no relationships update!", character.Name);
                        }

                        var relationship = character.Relationships.SingleOrDefault(x => x.TargetCharacterName == reflectionOutputRelationshipUpdate.Toward);
                        if (relationship == null)
                        {
                            characterRelationships.Add(new CharacterRelationshipContext
                            {
                                TargetCharacterName = reflectionOutputRelationshipUpdate.Toward,
                                Data = reflectionOutputRelationshipUpdate.ExtensionData!,
                                UpdateTime = storyTrackerResult.Time,
                                SequenceNumber = 0,
                                Dynamic = reflectionOutputRelationshipUpdate.Dynamic!,
                            });
                        }
                        else if (reflectionOutputRelationshipUpdate.ExtensionData?.Count > 0)
                        {
                            var updatedRelationship = relationship.Data.PatchWith(reflectionOutputRelationshipUpdate.ExtensionData);
                            var newRelationship = new CharacterRelationshipContext
                            {
                                TargetCharacterName = relationship.TargetCharacterName,
                                Data = updatedRelationship,
                                UpdateTime = storyTrackerResult.Time,
                                SequenceNumber = relationship.SequenceNumber + 1,
                                Dynamic = reflectionOutputRelationshipUpdate.Dynamic ?? relationship.Dynamic,
                            };
                            characterRelationships.Add(newRelationship);
                        }
                    }

                    var memory = new List<MemoryContext>();
                    if (reflectionOutput.Memory is not null)
                    {
                        memory.Add(new MemoryContext
                        {
                            Salience = reflectionOutput.Memory!.Salience,
                            Data = reflectionOutput.Memory.ExtensionData!,
                            MemoryContent = reflectionOutput.Memory.Summary,
                            SceneTracker = storyTrackerResult,
                        });
                    }

                    var characterContext = new CharacterContext
                    {
                        CharacterId = character.CharacterId,
                        CharacterState = characterState,
                        CharacterTracker = character.CharacterTracker,
                        Name = character.Name,
                        Description = character.Description,
                        CharacterMemories = memory,
                        Relationships = characterRelationships,
                        SceneRewrites =
                        [
                            new CharacterSceneContext
                            {
                                Content = reflectionOutput.SceneRewrite,
                                SceneTracker = storyTrackerResult,
                                SequenceNumber = character.SceneRewrites.MaxBy(x => x.SequenceNumber)
                                                     ?.SequenceNumber
                                                 + 1
                                                 ?? 0,
                            }
                        ],
                        Importance = character.Importance,
                        SimulationMetadata = null
                    };

                    var trackerDelta = await characterTrackerAgent.Invoke(context, character, storyTrackerResult, cancellationToken);
                    if (trackerDelta.TrackerChanges?.ExtensionData != null)
                    {
                        characterContext.CharacterTracker = character.CharacterTracker.PatchWith(trackerDelta.TrackerChanges.ExtensionData);
                    }

                    return characterContext;
                })
                .ToArray();
        }

        context.NewTracker!.MainCharacter = await mainCharTrackerTask;
        await UnpackCharacterUpdates(context, characterUpdateTask);
        await chroniclerTask;
    }

    private static LoreRequest ConvertToLoreRequest(ChroniclerLoreRequest req)
    {
        return JsonSerializer.Deserialize<LoreRequest>(req.ToJsonString())!;
    }

    private async Task UnpackCharacterUpdates(GenerationContext context, Task<CharacterContext?>[] tasks)
    {
        foreach (var task in tasks)
        {
            var res = await task;
            if (res is not null)
            {
                lock (context)
                {
                    context.CharacterUpdates.Add(res);
                }
            }
        }
    }

    private async Task<MainCharacterState> ProcessMainChar(GenerationContext context, SceneTracker sceneTrackerResult, CancellationToken cancellationToken)
    {
        if (context.SceneContext.Length == 0)
        {
            return await initMainCharacterTrackerAgent.Invoke(context, sceneTrackerResult, cancellationToken);
        }

        var mainCharacterDeltaTrackerOutput = await mainCharacterTrackerAgent.Invoke(context, sceneTrackerResult, cancellationToken);

        var newTracker = new MainCharacterState
        {
            MainCharacter = context.LatestTracker()!.MainCharacter!.MainCharacter.PatchWith(mainCharacterDeltaTrackerOutput.TrackerChanges!.ExtensionData!),
            MainCharacterDescription = mainCharacterDeltaTrackerOutput.Description
        };
        return newTracker;
    }

    private async Task ProcessChronicler(GenerationContext context, SceneTracker storyTrackerResult, CancellationToken cancellationToken)
    {
        if (context.ChroniclerOutput is null)
        {
            var chroniclerOutput = await chroniclerAgent.Invoke(context, storyTrackerResult, cancellationToken);
            context.ChroniclerOutput = chroniclerOutput;

            logger.Information("Chronicler produced {Count} world events", chroniclerOutput.WorldEvents);
            lock (context)
            {
                foreach (WorldEvent chroniclerOutputWorldEvent in chroniclerOutput.WorldEvents)
                {
                    context.NewWorldEvents.Add(chroniclerOutputWorldEvent);
                }
            }
        }

        if (context.ChroniclerLore.Length == 0 && context.ChroniclerOutput.LoreRequests.Length != 0)
        {
            logger.Information("Chronicler requested {Count} lore entries", context.ChroniclerOutput.LoreRequests.Length);
            var loreResults = await Task.WhenAll(
                context.ChroniclerOutput.LoreRequests.Select(req =>
                    loreCrafter.Invoke(context, ConvertToLoreRequest(req), cancellationToken)));

            context.ChroniclerLore = loreResults;
            logger.Information("Created {Count} lore entries from chronicler requests", loreResults.Length);
        }
    }
}