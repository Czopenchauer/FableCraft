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
        // Skip if already fully processed
        if (context.NewCharacters != null &&
            context.NewLore != null &&
            context.NewLocations != null &&
            context.NewItems != null)
        {
            logger.Information("Content already generated, skipping");
            return;
        }

        var creationRequests = context.NewNarrativeDirection!.CreationRequests;

        if (context.NewCharacters != null)
        {
            logger.Information("Characters already created, skipping ({Count})", context.NewCharacters.Length);
        }
        else
        {
            var characterCreationTasks = creationRequests.Characters
                .Select(x => characterCrafter.Invoke(context, x, cancellationToken))
                .ToList();
            var characterCreations = await Task.WhenAll(characterCreationTasks);
            logger.Information("Created {Count} new characters", characterCreations.Length);
            context.NewCharacters = characterCreations;
        }

        if (context.NewLore != null)
        {
            logger.Information("Lore already created, skipping ({Count})", context.NewLore.Length);
        }
        else
        {
            var newLoreTask = creationRequests.Lore
                .Select(x => loreCrafter.Invoke(context, x, cancellationToken))
                .ToList();
            var newLore = await Task.WhenAll(newLoreTask);
            logger.Information("Created {Count} new lore", newLore.Length);
            context.NewLore = newLore;
        }

        if (context.NewLocations != null)
        {
            logger.Information("Locations already created, skipping ({Count})", context.NewLocations.Length);
        }
        else
        {
            var newLocationTask = creationRequests.Locations
                .Select(location => locationCrafter.Invoke(context, location, cancellationToken))
                .ToList();
            var newLocations = await Task.WhenAll(newLocationTask);
            logger.Information("Created {Count} new locations", newLocations.Length);
            context.NewLocations = newLocations;
        }

        if (context.NewItems != null)
        {
            logger.Information("Items already created, skipping ({Count})", context.NewItems.Length);
        }
        else
        {
            var newItemTask = creationRequests.Items
                .Select(item => itemCrafter.Invoke(context, item, cancellationToken))
                .ToList();
            var newItems = await Task.WhenAll(newItemTask);
            logger.Information("Created {Count} new items", newItems.Length);
            context.NewItems = newItems;
        }
    }
}