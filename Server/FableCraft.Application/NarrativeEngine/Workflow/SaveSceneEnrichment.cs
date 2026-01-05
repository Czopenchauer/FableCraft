using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;
using FableCraft.Infrastructure.Queue;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace FableCraft.Application.NarrativeEngine.Workflow;

internal sealed class SaveSceneEnrichment(IDbContextFactory<ApplicationDbContext> dbContextFactory, IMessageDispatcher messageDispatcher) : IProcessor
{
    public async Task Invoke(GenerationContext context, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var scene = await dbContext
            .Scenes
            .Include(x => x.CharacterStates)
            .Include(x => x.Lorebooks)
            .SingleAsync(x => x.Id == context.NewSceneId && x.AdventureId == context.AdventureId, cancellationToken: cancellationToken);

        scene.Metadata.Tracker = context.NewTracker!;
        scene.EnrichmentStatus = EnrichmentStatus.Enriched;

        if (context.NewChroniclerState != null)
        {
            scene.Metadata.ChroniclerState = context.NewChroniclerState;
        }

        if (context.WriterGuidance != null)
        {
            scene.Metadata.WriterGuidance = context.WriterGuidance.ToJsonString();
        }

        // Save gathered context for use in next scene generation
        if (context.ContextGathered != null)
        {
            scene.Metadata.GatheredContext = new GatheredContext
            {
                WorldContext = context.ContextGathered.WorldContext.Select(x => new GatheredContextItem
                {
                    Topic = x.Topic,
                    Content = x.Content
                }).ToArray(),
                NarrativeContext = context.ContextGathered.NarrativeContext.Select(x => new GatheredContextItem
                {
                    Topic = x.Topic,
                    Content = x.Content
                }).ToArray(),
                AdditionalProperties = context.ContextGathered.AdditionalData
            };
        }

        var loreEntities = context.NewLore?.Select(x => new LorebookEntry
                           {
                               AdventureId = context.AdventureId,
                               Description = x.Description,
                               Title = x.Title,
                               Category = nameof(LorebookCategory.Lore),
                               Content = x.ToJsonString(),
                               ContentType = ContentType.json
                           }).ToList()
                           ?? new List<LorebookEntry>();

        var locationEntities = context.NewLocations?.Select(x => new LorebookEntry
                               {
                                   AdventureId = context.AdventureId,
                                   Description = x.Description,
                                   Title = x.Title,
                                   Category = nameof(LorebookCategory.Location),
                                   Content = x.ToJsonString(),
                                   ContentType = ContentType.json
                               }).ToList()
                               ?? new List<LorebookEntry>();

        var itemsEntities = context.NewItems?.Select(x => new LorebookEntry
                            {
                                AdventureId = context.AdventureId,
                                Description = x.Description,
                                Title = x.Name,
                                Category = nameof(LorebookCategory.Item),
                                Content = x.ToJsonString(),
                                ContentType = ContentType.json
                            }).ToList()
                            ?? new List<LorebookEntry>();

        var worldEventEntities = context.NewWorldEvents?.Select(x => new LorebookEntry
                                 {
                                     AdventureId = context.AdventureId,
                                     Title = $"Event at {x.Where}",
                                     Description = $"[{x.When}] {x.Where}",
                                     Category = nameof(LorebookCategory.WorldEvent),
                                     Content = $"""
                                                {x.When}: {x.Where}\n\n{x.Event}
                                                """,
                                     ContentType = ContentType.txt
                                 }).ToList()
                                 ?? new List<LorebookEntry>();

        loreEntities.AddRange(locationEntities);
        loreEntities.AddRange(itemsEntities);
        loreEntities.AddRange(worldEventEntities);
        scene.Lorebooks = loreEntities;

        IExecutionStrategy strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            var generationProcess = await dbContext.GenerationProcesses.FirstOrDefaultAsync(x => x.AdventureId == context.AdventureId, cancellationToken: cancellationToken);
            if (generationProcess != null)
            {
                dbContext.GenerationProcesses.Remove(generationProcess);
            }

            dbContext.Scenes.Update(scene);

            await UpsertCharacters(context, scene, cancellationToken, dbContext);

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        });

        await messageDispatcher.PublishAsync(new SceneGeneratedEvent
            {
                AdventureId = scene.AdventureId,
                SceneId = scene.Id
            },
            cancellationToken);
    }

    private async static Task UpsertCharacters(GenerationContext context, Scene scene, CancellationToken cancellationToken, ApplicationDbContext dbContext)
    {
        if (context.CharacterUpdates?.Count > 0)
        {
            var characters = await dbContext
                .Characters
                .Where(x => x.AdventureId == context.AdventureId && context.CharacterUpdates.Select(y => y.Name).Contains(x.Name))
                .ToListAsync(cancellationToken: cancellationToken);

            foreach (Character character in characters)
            {
                var update = context.CharacterUpdates!.Single(x => x.Name == character.Name);
                character.CharacterStates.Add(new CharacterState
                {
                    Description = update.Description,
                    CharacterStats = update.CharacterState,
                    Tracker = update.CharacterTracker!,
                    SequenceNumber = character.Version + 1,
                    Scene = scene
                });
                character.Version += 1;
                var memories = update.CharacterMemories.Select(x => new CharacterMemory
                {
                    SceneTracker = x.SceneTracker,
                    Scene = scene,
                    Summary = x.MemoryContent,
                    Data = x.Data,
                    Salience = x.Salience,
                });

                var relationships = update.Relationships.Select(x => new CharacterRelationship
                {
                    TargetCharacterName = x.TargetCharacterName,
                    Data = x.Data,
                    SequenceNumber = x.SequenceNumber,
                    Scene = scene,
                    UpdateTime = x.UpdateTime,
                });
                var sceneRewrites = update.SceneRewrites.Select(x => new CharacterSceneRewrite
                {
                    Content = x.Content,
                    SequenceNumber = x.SequenceNumber,
                    Scene = scene,
                    SceneTracker = x.StoryTracker!
                });
                character.CharacterMemories.AddRange(memories);
                character.CharacterRelationships.AddRange(relationships);
                character.CharacterSceneRewrites.AddRange(sceneRewrites);
                dbContext.Characters.Update(character);
            }
        }

        if (context.NewCharacters?.Length > 0)
        {
            var characters = await dbContext
                .Characters
                .Where(x => x.AdventureId == context.AdventureId && context.NewCharacters.Select(y => y.Name).Contains(x.Name))
                .ToListAsync(cancellationToken: cancellationToken);

            foreach (CharacterContext contextNewCharacter in context.NewCharacters)
            {
                var memories = contextNewCharacter.CharacterMemories.Select(x => new CharacterMemory
                {
                    SceneTracker = x.SceneTracker,
                    Scene = scene,
                    Summary = x.MemoryContent,
                    Data = x.Data,
                    Salience = x.Salience,
                }).ToList();

                var relationships = contextNewCharacter.Relationships.Select(x => new CharacterRelationship
                {
                    TargetCharacterName = x.TargetCharacterName,
                    Data = x.Data,
                    SequenceNumber = x.SequenceNumber,
                    Scene = scene,
                    UpdateTime = x.UpdateTime
                }).ToList();
                var sceneRewrites = contextNewCharacter.SceneRewrites.Select(x => new CharacterSceneRewrite
                {
                    Content = x.Content,
                    SequenceNumber = x.SequenceNumber,
                    Scene = scene,
                    SceneTracker = x.StoryTracker!
                }).ToList();
                var existingChar = characters.SingleOrDefault(x => x.Name == contextNewCharacter.Name);
                if (existingChar != null)
                {
                    existingChar.CharacterMemories = memories;
                    existingChar.CharacterRelationships = relationships;
                    existingChar.CharacterSceneRewrites = sceneRewrites;
                }
                else
                {
                    var newChar = new Character
                    {
                        AdventureId = context.AdventureId,
                        Name = contextNewCharacter.Name,
                        CharacterStates =
                        [
                            new()
                            {
                                Description = contextNewCharacter.Description,
                                CharacterStats = contextNewCharacter.CharacterState,
                                Tracker = contextNewCharacter.CharacterTracker!,
                                SequenceNumber = 0,
                                Scene = scene
                            }
                        ],
                        Version = 0,
                        CharacterMemories = memories,
                        CharacterRelationships = relationships,
                        CharacterSceneRewrites = sceneRewrites,
                        Importance = default,
                    };
                    dbContext.Characters.Add(newChar);
                }
            }
        }
    }
}