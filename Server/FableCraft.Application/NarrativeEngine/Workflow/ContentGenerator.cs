using FableCraft.Application.NarrativeEngine.Agents;
using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;

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
        if (context.NewCharacters != null && context.NewLore != null && context.NewLocations != null && context.NewItems != null)
        {
            logger.Information("Content already generated, skipping");
            return;
        }

        var creationRequests = context.NewScene?.CreationRequests ?? new CreationRequests();

        // Start all content generation tasks in parallel
        Task<CharacterContext[]>? characterTask = null;
        Task<GeneratedLore[]>? loreTask = null;
        Task<LocationGenerationResult[]>? locationTask = null;
        Task<GeneratedItem[]>? itemTask = null;

        if (context.NewCharacters != null)
        {
            logger.Information("Characters already created, skipping ({Count})", context.NewCharacters.Length);
        }
        else
        {
            characterTask = Task.WhenAll(creationRequests.Characters
                .Select(x => characterCrafter.Invoke(context, x, cancellationToken)).ToArray());
        }

        if (context.NewLore != null)
        {
            logger.Information("Lore already created, skipping ({Count})", context.NewLore.Count);
        }
        else
        {
            loreTask = Task.WhenAll(creationRequests.Lore
                .Select(x => loreCrafter.Invoke(context, x, cancellationToken)).ToArray());
        }

        if (context.NewLocations != null)
        {
            logger.Information("Locations already created, skipping ({Count})", context.NewLocations.Length);
        }
        else
        {
            locationTask = Task.WhenAll(creationRequests.Locations
                .Select(location => locationCrafter.Invoke(context, location, cancellationToken)).ToArray());
        }

        if (context.NewItems != null)
        {
            logger.Information("Items already created, skipping ({Count})", context.NewItems.Length);
        }
        else
        {
            itemTask = Task.WhenAll(creationRequests.Items
                .Select(item => itemCrafter.Invoke(context, item, cancellationToken)).ToArray());
        }

        // Await all tasks in parallel
        if (characterTask != null)
        {
            context.NewCharacters = await characterTask;
            logger.Information("Created {Count} new characters", context.NewCharacters.Length);
        }

        if (loreTask != null)
        {
            var lore = await loreTask;
            foreach (GeneratedLore generatedLore in lore)
            {
                context.NewLore!.Enqueue(generatedLore);
            }

            logger.Information("Created {Count} new lore", context.NewLore!.Count);
        }

        if (locationTask != null)
        {
            context.NewLocations = await locationTask;
            logger.Information("Created {Count} new locations", context.NewLocations.Length);
        }

        if (itemTask != null)
        {
            context.NewItems = await itemTask;
            logger.Information("Created {Count} new items", context.NewItems.Length);
        }
    }
}