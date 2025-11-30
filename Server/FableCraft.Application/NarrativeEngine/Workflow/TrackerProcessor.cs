using FableCraft.Application.NarrativeEngine.Agents;
using FableCraft.Application.NarrativeEngine.Models;

namespace FableCraft.Application.NarrativeEngine.Workflow;

internal sealed class TrackerProcessor(TrackerAgent trackerAgent, CharacterStateTracker characterStateTracker) : IProcessor
{
    public async Task Invoke(GenerationContext context, CancellationToken cancellationToken)
    {
        if (context.GenerationProcessStep < GenerationProcessStep.TrackerUpdatingFinished)
        {
            var tracker = await trackerAgent.Invoke(context, cancellationToken);
            context.NewTracker = tracker;
            context.GenerationProcessStep = GenerationProcessStep.TrackerUpdatingFinished;
        }

        if (context.GenerationProcessStep < GenerationProcessStep.CharacterStateTrackingFinished)
        {
            var characterUpdatesTask = context.NewTracker!.CharactersPresent
                .ExceptBy(context.NewCharacters!.Select(x => x.Name), x => x)
                .ToList();

            var characterUpdateTask = context.Characters
                .Where(x => characterUpdatesTask.Contains(x.Name))
                .Select(character => characterStateTracker.Invoke(context, character, cancellationToken));
        
            var characterUpdates = await Task.WhenAll(characterUpdateTask);
            context.CharacterUpdates = characterUpdates;
            context.GenerationProcessStep = GenerationProcessStep.CharacterStateTrackingFinished;
        }
    }
}