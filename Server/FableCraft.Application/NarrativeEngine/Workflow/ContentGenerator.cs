using FableCraft.Application.NarrativeEngine.Agents;
using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;

using Serilog;

namespace FableCraft.Application.NarrativeEngine.Workflow;

internal class ContentGenerator(
    LoreCrafter loreCrafter,
    LocationCrafter locationCrafter,
    CharacterCrafter characterCrafter,
    PartialProfileCrafter partialProfileCrafter,
    ItemCrafter itemCrafter,
    ILogger logger,
    CharacterReflectionAgent characterReflectionAgent
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

        // Split character requests by importance tier
        var backgroundCharacterRequests = creationRequests.Characters
            .Where(x => x.Importance == CharacterImportance.Background)
            .ToList();

        var fullProfileCharacterRequests = creationRequests.Characters
            .Where(x => x.Importance == CharacterImportance.ArcImportance || x.Importance == CharacterImportance.Significant)
            .ToList();

        Task<CharacterContext[]>? fullCharacterTask = null;
        Task<GeneratedPartialProfile[]>? backgroundCharacterTask = null;
        Task<GeneratedLore[]>? loreTask = null;
        Task<LocationGenerationResult[]>? locationTask = null;
        Task<GeneratedItem[]>? itemTask = null;

        if (context.NewCharacters != null)
        {
            logger.Information("Characters already created, skipping ({Count})", context.NewCharacters.Length);
        }
        else
        {
            if (fullProfileCharacterRequests.Count > 0)
            {
                fullCharacterTask = Task.WhenAll(fullProfileCharacterRequests
                    .Select(async x =>
                    {
                        var character = await characterCrafter.Invoke(context, x, cancellationToken);
                        if (character.SceneRewrites.Count > 0)
                        {
                            return character;
                        }

                        var reflection = await characterReflectionAgent.Invoke(context, character, context.NewTracker!.Scene!, cancellationToken);
                        character.SceneRewrites =
                        [
                            new CharacterSceneContext
                            {
                                Content = reflection.SceneRewrite,
                                SceneTracker = context.NewTracker!.Scene!,
                                SequenceNumber = 0
                            }
                        ];
                        character.CharacterMemories = reflection.Memory!.Select(y => new MemoryContext()
                        {
                            Salience = y.Salience,
                            Data = y.ExtensionData!,
                            SceneTracker = context.NewTracker!.Scene!,
                            MemoryContent = y.Summary
                        }).ToList();

                        return character;
                    }).ToArray());
            }

            if (backgroundCharacterRequests.Count > 0)
            {
                backgroundCharacterTask = Task.WhenAll(backgroundCharacterRequests
                    .Select(x => partialProfileCrafter.Invoke(context, x, cancellationToken)).ToArray());
            }
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

        if (fullCharacterTask != null)
        {
            context.NewCharacters = await fullCharacterTask;
            logger.Information("Created {Count} new full character profiles", context.NewCharacters.Length);
        }
        else
        {
            context.NewCharacters ??= Array.Empty<CharacterContext>();
        }

        if (backgroundCharacterTask != null)
        {
            var backgrounds = await backgroundCharacterTask;
            foreach (var bg in backgrounds)
            {
                context.NewBackgroundCharacters.Enqueue(bg);
            }

            logger.Information("Created {Count} new background character profiles", backgrounds.Length);
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