using FableCraft.Application.NarrativeEngine.Agents;
using FableCraft.Application.NarrativeEngine.Models;

namespace FableCraft.Application.NarrativeEngine.Workflow;

internal sealed class TrackerProcessor(TrackerAgent trackerAgent, CharacterStateTracker characterStateTracker) : IProcessor
{
    public async Task Invoke(GenerationContext context, CancellationToken cancellationToken)
    {
        var tracker = trackerAgent.Invoke(context, cancellationToken);

        var lastSceneCharacters = context.SceneContext.LastOrDefault()?.Metadata.Tracker?.CharactersPresent ?? [];
        IEnumerable<Task<CharacterContext>>? characterUpdateTask = null;
        if (context.Characters.Count != 0)
        {
            characterUpdateTask = context.Characters
                .Where(x => lastSceneCharacters.Contains(x.Name))
                .Select(character => characterStateTracker.Invoke(context, character, cancellationToken))
                .ToArray();
        }

        context.NewTracker = await tracker;
        var characterUpdates = characterUpdateTask != null ? await Task.WhenAll(characterUpdateTask) : null;
        context.CharacterUpdates = characterUpdates;
        context.GenerationProcessStep = GenerationProcessStep.TrackerFinished;
    }
}