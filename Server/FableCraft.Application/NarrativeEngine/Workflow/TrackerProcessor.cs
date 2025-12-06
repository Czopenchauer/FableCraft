using FableCraft.Application.NarrativeEngine.Agents;
using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;

namespace FableCraft.Application.NarrativeEngine.Workflow;

internal sealed class TrackerProcessor(
    TrackerAgent trackerAgent,
    CharacterStateAgent characterStateAgent,
    CharacterTrackerAgent characterTrackerAgent) : IProcessor
{
    public async Task Invoke(GenerationContext context, CancellationToken cancellationToken)
    {
        var trackerTask = trackerAgent.Invoke(context, cancellationToken);

        var lastSceneCharacters = context.SceneContext
            .OrderByDescending(x => x.SequenceNumber)
            .FirstOrDefault()?.Metadata.Tracker?.CharactersPresent ?? [];
        IEnumerable<Task<CharacterContext>>? characterUpdateTask = null;
        if (context.Characters.Count != 0)
        {
            characterUpdateTask = context.Characters
                .Where(x => lastSceneCharacters.Contains(x.Name))
                .Select(async character =>
                {
                    var stateTask = characterStateAgent.Invoke(context, character, cancellationToken);
                    var trackerTask = characterTrackerAgent.Invoke(context, character, cancellationToken);

                    await Task.WhenAll(stateTask, trackerTask);

                    return new CharacterContext
                    {
                        CharacterId = character.CharacterId,
                        CharacterState = await stateTask,
                        CharacterTracker = await trackerTask,
                        Name = character.Name,
                        Description = character.Description,
                        SequenceNumber = character.SequenceNumber + 1
                    };
                })
                .ToArray();
        }

        await Task.WhenAll(trackerTask);

        var tracker = await trackerTask;

        // Create new tracker with MainCharacterDevelopment
        context.NewTracker = new Tracker
        {
            Story = tracker.Story,
            CharactersPresent = tracker.CharactersPresent,
            MainCharacter = tracker.MainCharacter,
            Characters = tracker.Characters,
        };
        var characterUpdates = characterUpdateTask != null ? await Task.WhenAll(characterUpdateTask) : null;
        context.CharacterUpdates = characterUpdates;
        context.GenerationProcessStep = GenerationProcessStep.TrackerFinished;
    }
}