using FableCraft.Application.NarrativeEngine.Agents;
using FableCraft.Application.NarrativeEngine.Models;

namespace FableCraft.Application.NarrativeEngine.Workflow;

internal sealed class TrackerProcessor(TrackerAgent trackerAgent, CharacterStateTracker characterStateTracker) : IProcessor
{
    public async Task Invoke(GenerationContext context, CancellationToken cancellationToken)
    {
        var tracker = trackerAgent.Invoke(context, cancellationToken);

        var characterUpdatesTask = context.NewTracker!.CharactersPresent
            .ExceptBy(context.NewCharacters!.Select(x => x.Name), x => x)
            .ToList();

        IEnumerable<Task<CharacterContext>>? characterUpdateTask = null;
        if (characterUpdatesTask.Count != 0)
        {
            characterUpdateTask = context.Characters
                .Where(x => characterUpdatesTask.Contains(x.Name))
                .Select(character => characterStateTracker.Invoke(context, character, cancellationToken));
        }

        context.NewTracker = await tracker;
        context.GenerationProcessStep = GenerationProcessStep.TrackerUpdatingFinished;
        var characterUpdates = characterUpdateTask != null ? await Task.WhenAll(characterUpdateTask) : null;
        context.CharacterUpdates = characterUpdates;
        context.GenerationProcessStep = GenerationProcessStep.CharacterStateTrackingFinished;
    }
}