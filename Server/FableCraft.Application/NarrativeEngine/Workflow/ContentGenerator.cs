using FableCraft.Application.NarrativeEngine.Agents;
using FableCraft.Application.NarrativeEngine.Models;

using Serilog;

namespace FableCraft.Application.NarrativeEngine.Workflow;

internal class ContentGenerator(
    LoreCrafter loreCrafter,
    LocationCrafter locationCrafter,
    CharacterCrafter characterCrafter,
    ItemCrafter itemCrafter,
    ILogger logger
) : IProcessor
{
    public async Task Invoke(GenerationContext context, CancellationToken cancellationToken)
    {
        var characterCreationTasks = context.NewNarrativeDirection!.CreationRequests.Characters
            .Select(x => characterCrafter.Invoke(context, x, cancellationToken))
            .ToList();

        var newLoreTask = context.NewNarrativeDirection!.CreationRequests.Lore
            .Select(x => loreCrafter.Invoke(context, x, cancellationToken))
            .ToList();

        var newLocationTask = context.NewNarrativeDirection!.CreationRequests.Locations
            .Select(location => locationCrafter.Invoke(context, location, cancellationToken))
            .ToList();

        var newItemTask = context.NewNarrativeDirection!.CreationRequests.Items
            .Select(item => itemCrafter.Invoke(context, item, cancellationToken))
            .ToList();

        var characterCreations = await Task.WhenAll(characterCreationTasks);
        logger.Information("Created {Count} new characters", characterCreations.Length);
        context.NewCharacters = characterCreations;

        var newLore = await Task.WhenAll(newLoreTask);
        logger.Information("Created {Count} new lore", newLore.Length);
        context.NewLore = newLore;

        var newLocation = await Task.WhenAll(newLocationTask);
        logger.Information("Created {Count} new locations", newLocation.Length);
        context.NewLocations = newLocation;

        var newItems = await Task.WhenAll(newItemTask);
        logger.Information("Created {Count} new items", newItems.Length);
        context.NewItems = newItems;

        context.GenerationProcessStep = GenerationProcessStep.ContentCreationFinished;
    }
}