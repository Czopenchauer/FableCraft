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

                    var reflectionTask = characterReflectionAgent.Invoke(context, character, storyTrackerResult, cancellationToken);
                    var trackerTask = characterTrackerAgent.Invoke(context, character, storyTrackerResult, cancellationToken);

                    await Task.WhenAll(reflectionTask, trackerTask);

                    var reflectionOutput = await reflectionTask;
                    (CharacterTracker charTracker, var description) = await trackerTask;

                    var characterState = character.CharacterState;
                    if (reflectionOutput.ExtensionData?.Count == 0)
                    {
                        logger.Warning("Character reflection output for character {CharacterName} has no update for character state.", character.Name);
                        characterState = characterState.PatchWith(reflectionOutput.ExtensionData);
                    }

                    var characterRelationships = new List<CharacterRelationship>();
                    foreach (CharacterRelationshipOutput reflectionOutputRelationshipUpdate in reflectionOutput.RelationshipUpdates)
                    {
                        if (reflectionOutputRelationshipUpdate.ExtensionData?.Count == 0)
                        {
                            logger.Warning("Character {CharacterName} has no relationships update!", character.Name);
                        }

                        var relationship = character.Relationships.SingleOrDefault(x => x.TargetCharacterName == reflectionOutputRelationshipUpdate.Name);
                        if (relationship == null)
                        {
                            characterRelationships.Add(new CharacterRelationship
                            {
                                TargetCharacterName = reflectionOutputRelationshipUpdate.Name,
                                Data = reflectionOutputRelationshipUpdate.ExtensionData.ToJsonString(),
                                SequenceNumber = 0,
                                StoryTracker = storyTrackerResult
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
                        CharacterTracker = charTracker,
                        Name = character.Name,
                        Description = description,
                        CharacterMemories = reflectionOutput.Memory!.Select(x => new CharacterMemory
                        {
                            Summary = x.Summary,
                            Salience = x.Salience,
                            Data = x.ExtensionData.ToJsonString(),
                            StoryTracker = storyTrackerResult
                        }).ToList(),
                        Relationships = characterRelationships,
                        SceneRewrites =
                        [
                            new CharacterSceneRewrite
                            {
                                Content = reflectionOutput.SceneRewrite,
                                StoryTracker = storyTrackerResult,
                            }
                        ]
                    };

                    return characterContext;
                })
                .ToArray();
        }

        context.NewTracker!.MainCharacter = await mainCharTrackerTask;
        if (characterUpdateTask != null)
        {
            await UnpackCharacterUpdates(context, characterUpdateTask);
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