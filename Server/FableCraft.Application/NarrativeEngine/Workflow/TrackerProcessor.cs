using FableCraft.Application.NarrativeEngine.Agents;
using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;

namespace FableCraft.Application.NarrativeEngine.Workflow;

internal sealed class TrackerProcessor(
    StoryTrackerAgent storyTracker,
    MainCharacterTrackerAgent mainCharacterTrackerAgent,
    CharacterStateAgent characterStateAgent,
    CharacterTrackerAgent characterTrackerAgent) : IProcessor
{
    public async Task Invoke(GenerationContext context, CancellationToken cancellationToken)
    {
        var storyTrackerResult = context.NewTracker?.Story != null
            ? context.NewTracker.Story
            : await storyTracker.Invoke(context, cancellationToken);

        if (string.IsNullOrEmpty(storyTrackerResult.Location) || string.IsNullOrEmpty(storyTrackerResult.Time) || string.IsNullOrEmpty(storyTrackerResult.Weather))
        {
            throw new InvalidOperationException();
        }

        context.NewTracker ??= new Tracker();
        context.NewTracker.Story = storyTrackerResult;
        var mainCharTrackerTask = context.NewTracker?.MainCharacter?.MainCharacter != null
            ? Task.FromResult(
                (context.NewTracker!.MainCharacter.MainCharacter!, context.NewTracker.MainCharacter.MainCharacterDescription ?? context.MainCharacter.Description))
            : mainCharacterTrackerAgent.Invoke(context, storyTrackerResult, cancellationToken);

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

                    var stateTask = characterStateAgent.Invoke(context, character, storyTrackerResult, cancellationToken);
                    var trackerTask = characterTrackerAgent.Invoke(context, character, storyTrackerResult, cancellationToken);

                    await Task.WhenAll(stateTask, trackerTask);
                    (CharacterTracker charTracker, var description) = await trackerTask;
                    return new CharacterContext
                    {
                        CharacterId = character.CharacterId,
                        CharacterState = await stateTask,
                        CharacterTracker = charTracker,
                        Name = character.Name,
                        Description = description,
                        SequenceNumber = character.SequenceNumber + 1
                    };
                })
                .ToArray();
        }

        (CharacterTracker mainCharTracker, var mainCharDescription) = await mainCharTrackerTask;
        context.NewTracker!.MainCharacter = new MainCharacterTracker
        {
            MainCharacter = mainCharTracker,
            MainCharacterDescription = mainCharDescription
        };

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
}
