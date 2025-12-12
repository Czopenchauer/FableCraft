using FableCraft.Application.NarrativeEngine.Agents;
using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;

namespace FableCraft.Application.NarrativeEngine.Workflow;

internal sealed class TrackerProcessor(
    StoryTrackerAgent storyTracker,
    MainCharacterTrackerAgent mainCharacterTrackerAgent,
    MainCharacterDevelopmentAgent mainCharacterDevelopmentAgent,
    CharacterStateAgent characterStateAgent,
    CharacterTrackerAgent characterTrackerAgent,
    CharacterDevelopmentAgent characterDevelopmentAgent) : IProcessor
{
    public async Task Invoke(GenerationContext context, CancellationToken cancellationToken)
    {
        Tracker storyTrackerResult = await storyTracker.Invoke(context, cancellationToken);
        var trackerDto = new TrackerDto
        {
            Story = storyTrackerResult.Story,
            MainCharacter = null!,
            CharactersOnScene = null!,
            Characters = null!,
        };
        var mainCharTrackerTask = mainCharacterTrackerAgent.Invoke(context, storyTrackerResult, cancellationToken);
        var trackerDevelopment = mainCharacterDevelopmentAgent.Invoke(context, storyTrackerResult, cancellationToken);
        IEnumerable<Task<CharacterContext>>? characterUpdateTask = null;
        if (context.Characters.Count != 0)
        {
            characterUpdateTask = context.Characters
                .Where(x => storyTrackerResult.CharactersPresent.Contains(x.Name))
                .Select(async character =>
                {
                    var stateTask = characterStateAgent.Invoke(context, character, storyTrackerResult, cancellationToken);
                    var trackerTask = characterTrackerAgent.Invoke(context, character, storyTrackerResult, cancellationToken);
                    var developmentTracker = characterDevelopmentAgent.Invoke(context, character, storyTrackerResult, cancellationToken);

                    await Task.WhenAll(stateTask, trackerTask, developmentTracker);
                    (CharacterTracker charTracker, var description) = await trackerTask;
                    return new CharacterContext
                    {
                        CharacterId = character.CharacterId,
                        CharacterState = await stateTask,
                        CharacterTracker = charTracker,
                        Name = character.Name,
                        Description = description,
                        SequenceNumber = character.SequenceNumber + 1,
                        DevelopmentTracker = await developmentTracker
                    };
                })
                .ToArray();
        }

        var characterUpdates = characterUpdateTask != null ? await Task.WhenAll(characterUpdateTask) : null;
        (CharacterTracker mainCharTracker, var mainCharDescription) = await mainCharTrackerTask;
        storyTrackerResult.MainCharacter = mainCharTracker;
        context.NewMainCharacterDescription = mainCharDescription;
        storyTrackerResult.MainCharacterDevelopment = await trackerDevelopment;
        storyTrackerResult.Characters = characterUpdates?.Select(x => x.CharacterTracker!).ToArray();
        storyTrackerResult.CharacterDevelopment = characterUpdates?.Select(x => x.DevelopmentTracker!).ToArray();
        context.NewTracker = storyTrackerResult;
        context.GenerationProcessStep = GenerationProcessStep.TrackerFinished;
    }
}