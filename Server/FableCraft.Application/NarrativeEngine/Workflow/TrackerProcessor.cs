using FableCraft.Application.NarrativeEngine.Agents;
using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;

using Serilog;

namespace FableCraft.Application.NarrativeEngine.Workflow;

internal sealed class TrackerProcessor(
    StoryTrackerAgent storyTracker,
    MainCharacterTrackerAgent mainCharacterTrackerAgent,
    CharacterReflectionAgent characterReflectionAgent,
    CharacterTrackerAgent characterTrackerAgent,
    InitMainCharacterTrackerAgent initMainCharacterTrackerAgent,
    ILogger logger) : IProcessor
{
    public async Task Invoke(GenerationContext context, CancellationToken cancellationToken)
    {
        var storyTrackerResult = context.NewTracker?.Story ?? await storyTracker.Invoke(context, cancellationToken);

        if (string.IsNullOrEmpty(storyTrackerResult.Location) || string.IsNullOrEmpty(storyTrackerResult.Time) || string.IsNullOrEmpty(storyTrackerResult.Weather))
        {
            throw new InvalidOperationException();
        }

        context.NewTracker ??= new Tracker();
        context.NewTracker.Story = storyTrackerResult;
        var mainCharTrackerTask = context.NewTracker?.MainCharacter?.MainCharacter != null
            ? Task.FromResult(context.NewTracker.MainCharacter)
            : ProcessMainChar(context, storyTrackerResult, cancellationToken);

        var alreadyProcessedCharacters = context.CharacterUpdates?
                                             .Select(x => x.Name)
                                         ?? [];

        Task<CharacterContext>[]? characterUpdateTask = null;
        if (context.Characters.Count != 0)
        {
            characterUpdateTask = context.Characters
                .Where(x => storyTrackerResult.CharactersPresent.Contains(x.Name))
                .Select(async character =>
                {
                    // Skip if this character was already processed in a previous attempt
                    if (alreadyProcessedCharacters.Contains(character.Name))
                    {
                        return context.CharacterUpdates!.First(x => x.Name == character.Name);
                    }

                    var reflectionOutput = await characterReflectionAgent.Invoke(context, character, storyTrackerResult, cancellationToken);

                    var characterState = character.CharacterState;
                    if (reflectionOutput.ExtensionData?.Count == 0)
                    {
                        logger.Warning("Character reflection output for character {CharacterName} has no update for character state.", character.Name);
                        characterState = characterState.PatchWith(reflectionOutput.ExtensionData);
                    }

                    var characterRelationships = new List<CharacterRelationshipContext>();
                    foreach (CharacterRelationshipOutput reflectionOutputRelationshipUpdate in reflectionOutput.RelationshipUpdates)
                    {
                        if (reflectionOutputRelationshipUpdate.ExtensionData?.Count == 0)
                        {
                            logger.Warning("Character {CharacterName} has no relationships update!", character.Name);
                        }

                        var relationship = character.Relationships.SingleOrDefault(x => x.TargetCharacterName == reflectionOutputRelationshipUpdate.Name);
                        if (relationship == null)
                        {
                            characterRelationships.Add(new CharacterRelationshipContext
                            {
                                TargetCharacterName = reflectionOutputRelationshipUpdate.Name,
                                Data = reflectionOutputRelationshipUpdate.ExtensionData.ToJsonString(),
                                StoryTracker = storyTrackerResult,
                                SequenceNumber = 0,
                            });
                        }
                        else if (reflectionOutputRelationshipUpdate.ExtensionData?.Count > 0)
                        {
                            var updatedRelationship = relationship.PatchWith(reflectionOutputRelationshipUpdate.ExtensionData);
                            updatedRelationship.SequenceNumber = relationship.SequenceNumber + 1;
                            characterRelationships.Add(updatedRelationship);
                        }
                    }

                    var characterContext = new CharacterContext
                    {
                        CharacterId = character.CharacterId,
                        CharacterState = characterState,
                        CharacterTracker = character.CharacterTracker,
                        Name = character.Name,
                        Description = character.Description,
                        CharacterMemories = reflectionOutput.Memory!.Select(x => new MemoryContext
                        {
                            Salience = x.Salience,
                            Data = x.ExtensionData.ToJsonString(),
                            MemoryContent = x.Summary,
                            StoryTracker = storyTrackerResult,
                        }).ToList(),
                        Relationships = characterRelationships,
                        SceneRewrites =
                        [
                            new CharacterSceneContext
                            {
                                Content = reflectionOutput.SceneRewrite,
                                StoryTracker = storyTrackerResult,
                                SequenceNumber = character.SceneRewrites.MaxBy(x => x.SequenceNumber)?.SequenceNumber + 1 ?? 0,
                            }
                        ]
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

        Task<CharacterContext>[]? sceneRewriteForNewChar = null;
        if (context.NewCharacters?.Length > 0)
        {
            sceneRewriteForNewChar = context.NewCharacters
                .Select(async character =>
                {
                    if (character.SceneRewrites.Count > 0)
                    {
                        return character;
                    }

                    var reflection = await characterReflectionAgent.Invoke(context, character, storyTrackerResult, cancellationToken);
                    character.SceneRewrites =
                    [
                        new CharacterSceneContext
                        {
                            Content = reflection.SceneRewrite,
                            StoryTracker = storyTrackerResult,
                            SequenceNumber = 0
                        }
                    ];
                    character.CharacterMemories = reflection.Memory!.Select(x => new MemoryContext()
                    {
                        Salience = x.Salience,
                        Data = x.ExtensionData.ToJsonString(),
                        StoryTracker = storyTrackerResult,
                        MemoryContent = x.Summary
                    }).ToList();

                    if (reflection.ExtensionData != null)
                    {
                        character.CharacterState = character.CharacterState.PatchWith(reflection.ExtensionData);
                    }

                    return character;
                })
                .ToArray();
        }

        context.NewTracker!.MainCharacter = await mainCharTrackerTask;
        if (characterUpdateTask != null)
        {
            await UnpackCharacterUpdates(context, characterUpdateTask);
        }

        if (sceneRewriteForNewChar != null)
        {
            await Task.WhenAll(sceneRewriteForNewChar);
        }
    }

    private async Task UnpackCharacterUpdates(GenerationContext context, Task<CharacterContext>[] tasks)
    {
        context.CharacterUpdates ??= new List<CharacterContext>();
        foreach (var task in tasks)
        {
            context.CharacterUpdates.Add(await task);
        }
    }

    private async Task<MainCharacterTracker> ProcessMainChar(GenerationContext context, StoryTracker storyTrackerResult, CancellationToken cancellationToken)
    {
        if (context.SceneContext.Length == 0)
        {
            return await initMainCharacterTrackerAgent.Invoke(context, storyTrackerResult, cancellationToken);
        }

        var mainCharacterDeltaTrackerOutput = await mainCharacterTrackerAgent.Invoke(context, storyTrackerResult, cancellationToken);

        var newTracker = new MainCharacterTracker
        {
            MainCharacter = context.LatestTracker()!.MainCharacter!.MainCharacter.PatchWith(mainCharacterDeltaTrackerOutput.TrackerChanges!.ExtensionData!),
            MainCharacterDescription = mainCharacterDeltaTrackerOutput.Description
        };
        return newTracker;
    }
}