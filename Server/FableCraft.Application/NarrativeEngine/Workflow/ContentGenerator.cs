using FableCraft.Application.NarrativeEngine.Agents;
using FableCraft.Application.NarrativeEngine.Models;

namespace FableCraft.Application.NarrativeEngine.Workflow;

internal class ContentGenerator(
    LoreCrafter loreCrafter,
    LocationCrafter locationCrafter,
    CharacterCrafter characterCrafter
    ) : IProcessor
{
    public async Task Invoke(GenerationContext context, CancellationToken cancellationToken)
    {
        if (context.GenerationProcessStep < GenerationProcessStep.CharacterCreationFinished)
        {
            var characterCreationTasks = context.NewNarrativeDirection!.CreationRequests.Characters
                .Select(x => characterCrafter.Invoke(context, x, cancellationToken))
                .ToList();
            var characterCreations = await Task.WhenAll(characterCreationTasks);
            context.NewCharacters = characterCreations;
            context.GenerationProcessStep = GenerationProcessStep.CharacterCreationFinished;
        }

        if (context.GenerationProcessStep < GenerationProcessStep.LoreGenerationFinished)
        {
            var newLoreTask = context.NewNarrativeDirection!.CreationRequests.Lore
                .Select(x => loreCrafter.Invoke(context, x, cancellationToken))
                .ToList();
            var newLore = await Task.WhenAll(newLoreTask);
            context.NewLore = newLore;
            context.GenerationProcessStep = GenerationProcessStep.LoreGenerationFinished;
        }

        if (context.GenerationProcessStep < GenerationProcessStep.LocationGenerationFinished)
        {
            var newLocationTask = context.NewNarrativeDirection!.CreationRequests.Locations
                .Select(location => locationCrafter.Invoke(context, location, cancellationToken))
                .ToList();
            var newLocation = await Task.WhenAll(newLocationTask);
            context.NewLocations = newLocation;
            context.GenerationProcessStep = GenerationProcessStep.LocationGenerationFinished;
        }
    }
}