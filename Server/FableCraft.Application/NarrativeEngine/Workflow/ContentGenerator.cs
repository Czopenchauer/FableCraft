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
        if (context.NewLore != null && context.NewLocations != null && context.NewItems != null)
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

        var fullCharacterTask = Task.FromResult<CharacterContext[]>([]);
        Task<GeneratedPartialProfile[]>? backgroundCharacterTask = null;
        Task<GeneratedLore[]>? loreTask = null;
        Task<LocationGenerationResult[]>? locationTask = null;
        Task<GeneratedItem[]>? itemTask = null;

        if (fullProfileCharacterRequests.Count > 0)
        {
            fullCharacterTask = Task.WhenAll(fullProfileCharacterRequests
                .Select(async x =>
                {
                    CharacterContext character;
                    if (x.CharacterId != null)
                    {
                        character = context.NewCharacters!.Single(y => y.CharacterId == x.CharacterId);
                    }
                    else
                    {
                        character = await characterCrafter.Invoke(context, x, cancellationToken);
                        lock (context)
                        {
                            x.CharacterId = character.CharacterId;
                            context.NewCharacters!.Add(character);
                        }
                    }

                    if (character.CharacterMemories.Count > 0 && character.SceneRewrites.Count > 0)
                    {
                        logger.Information("Character {CharacterName} already has memories and scene rewrites, skipping reflection", character.Name);
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

                    var memory = new List<MemoryContext>();
                    if (reflection.Memory is not null)
                    {
                        memory.Add(new MemoryContext
                        {
                            Salience = reflection.Memory!.Salience,
                            Data = reflection.Memory.ExtensionData!,
                            MemoryContent = reflection.Memory.Summary,
                            SceneTracker = context.NewTracker!.Scene!
                        });
                    }

                    character.CharacterMemories = memory;

                    return character;
                }).ToArray());
        }

        bool hasBackgroundCharacters;
        lock (context)
        {
            hasBackgroundCharacters = context.NewBackgroundCharacters.Count > 0;
        }

        if (backgroundCharacterRequests.Count > 0 && hasBackgroundCharacters)
        {
            backgroundCharacterTask = Task.WhenAll(backgroundCharacterRequests
                .Select(x => partialProfileCrafter.Invoke(context, x, cancellationToken)).ToArray());
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

        await Task.WhenAll(fullCharacterTask);

        if (backgroundCharacterTask != null)
        {
            var backgrounds = await backgroundCharacterTask;
            lock (context)
            {
                foreach (var bg in backgrounds)
                {
                    context.NewBackgroundCharacters.Add(bg);
                }
            }

            logger.Information("Created {Count} new background character profiles", backgrounds.Length);
        }

        if (loreTask != null)
        {
            var lore = await loreTask;
            int loreCount;
            lock (context)
            {
                foreach (var generatedLore in lore)
                {
                    context.NewLore!.Add(generatedLore);
                }

                loreCount = context.NewLore!.Count;
            }

            logger.Information("Created {Count} new lore", loreCount);
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