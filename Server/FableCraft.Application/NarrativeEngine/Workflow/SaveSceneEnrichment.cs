using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;
using FableCraft.Infrastructure.Queue;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

using Serilog;

namespace FableCraft.Application.NarrativeEngine.Workflow;

internal sealed class SaveSceneEnrichment(
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    IMessageDispatcher messageDispatcher,
    ILogger logger) : IProcessor
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

        List<LorebookEntry> loreEntities;
        lock (context)
        {
            loreEntities = context.NewLore?.Select(x => new LorebookEntry
                               {
                                   AdventureId = context.AdventureId,
                                   Description = x.Description,
                                   Title = x.Title,
                                   Category = nameof(LorebookCategory.Lore),
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

            var backgroundCharacterEntities = context.NewBackgroundCharacters?.Select(x => new LorebookEntry
                                              {
                                                  AdventureId = context.AdventureId,
                                                  Title = x.Name,
                                                  Description = x.Description,
                                                  Category = nameof(LorebookCategory.BackgroundCharacter),
                                                  Content = x.ToJsonString(),
                                                  ContentType = ContentType.json
                                              }).ToList()
                                              ?? new List<LorebookEntry>();

            loreEntities.AddRange(worldEventEntities);
            loreEntities.AddRange(backgroundCharacterEntities);
        }

        var chroniclerLore = context.ChroniclerLore?.Select(x => new LorebookEntry
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

        loreEntities.AddRange(chroniclerLore);
        loreEntities.AddRange(locationEntities);
        loreEntities.AddRange(itemsEntities);
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

            await ProcessImportanceFlags(context, dbContext, logger, cancellationToken);

            await MarkCharacterEventsConsumed(context, cancellationToken, dbContext);

            await SaveNewCharacterEvents(context, dbContext);

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
        List<CharacterContext> characterUpdates;
        List<CharacterContext> newCharacters;
        lock (context)
        {
            characterUpdates = context.CharacterUpdates?.ToList() ?? [];
            newCharacters = context.NewCharacters?.ToList() ?? [];
        }

        if (characterUpdates.Count > 0)
        {
            var updateNames = characterUpdates.Select(y => y.Name).ToList();
            var characters = await dbContext
                .Characters
                .Where(x => x.AdventureId == context.AdventureId && updateNames.Contains(x.Name))
                .ToListAsync(cancellationToken: cancellationToken);

            foreach (Character character in characters)
            {
                var update = characterUpdates.Single(x => x.Name == character.Name);
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
                    SceneTracker = x.SceneTracker!
                });
                character.CharacterMemories.AddRange(memories);
                character.CharacterRelationships.AddRange(relationships);
                character.CharacterSceneRewrites.AddRange(sceneRewrites);
                dbContext.Characters.Update(character);
            }
        }

        if (newCharacters.Count > 0)
        {
            var newCharacterNames = newCharacters.Select(y => y.Name).ToList();
            var characters = await dbContext
                .Characters
                .Where(x => x.AdventureId == context.AdventureId && newCharacterNames.Contains(x.Name))
                .ToListAsync(cancellationToken: cancellationToken);

            foreach (CharacterContext contextNewCharacter in newCharacters)
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
                    SceneTracker = x.SceneTracker!
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
                        Importance = contextNewCharacter.Importance,
                    };
                    dbContext.Characters.Add(newChar);
                }
            }
        }
    }

    private async static Task MarkCharacterEventsConsumed(GenerationContext context, CancellationToken cancellationToken, ApplicationDbContext dbContext)
    {
        List<Guid> eventIds;
        lock (context)
        {
            if (context.CharacterEventsToConsume.Count == 0)
            {
                return;
            }
            eventIds = context.CharacterEventsToConsume.ToList();
        }
        if (eventIds.Count == 0)
        {
            return;
        }

        await dbContext.CharacterEvents
            .Where(e => eventIds.Contains(e.Id))
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(e => e.Consumed, true),
                cancellationToken);
    }

    private static async Task SaveNewCharacterEvents(GenerationContext context, ApplicationDbContext dbContext)
    {
        List<CharacterEventToSave> eventsToSave;
        lock (context)
        {
            if (context.NewCharacterEvents.Count == 0)
            {
                return;
            }
            eventsToSave = context.NewCharacterEvents.ToList();
        }
        if (eventsToSave.Count == 0)
        {
            return;
        }

        var entities = eventsToSave.Select(e => new CharacterEvent
        {
            Id = Guid.NewGuid(),
            AdventureId = e.AdventureId,
            TargetCharacterName = e.TargetCharacterName,
            SourceCharacterName = e.SourceCharacterName,
            Time = e.Time,
            Event = e.Event,
            SourceRead = e.SourceRead,
            Consumed = false
        });

        await dbContext.CharacterEvents.AddRangeAsync(entities);
    }

    private async static Task ProcessImportanceFlags(
        GenerationContext context,
        ApplicationDbContext dbContext,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var importanceFlags = context.NewScene?.ImportanceFlags;
        if (importanceFlags == null)
        {
            return;
        }

        var allRequests = importanceFlags.UpgradeRequests
            .Concat(importanceFlags.DowngradeRequests)
            .ToList();

        if (allRequests.Count == 0)
        {
            return;
        }

        var validRequests = allRequests.Where(IsValidTransition).ToList();
        var invalidRequests = allRequests.Except(validRequests).ToList();

        foreach (var invalid in invalidRequests)
        {
            logger.Warning(
                "Invalid importance transition requested for {Character}: {From} -> {To}. " +
                "Only arc_important <-> significant transitions are supported.",
                invalid.Character, invalid.From, invalid.To);
        }

        if (validRequests.Count == 0)
        {
            return;
        }

        var characterNames = validRequests.Select(r => r.Character).ToList();
        var characters = await dbContext.Characters
            .Where(x => x.AdventureId == context.AdventureId && characterNames.Contains(x.Name))
            .ToListAsync(cancellationToken);

        foreach (var request in validRequests)
        {
            var character = characters.SingleOrDefault(c => c.Name == request.Character);
            if (character == null)
            {
                logger.Warning(
                    "Character {Character} not found for importance transition",
                    request.Character);
                continue;
            }

            var newImportance = CharacterImportanceConverter.FromString(request.To);

            logger.Information(
                "Updating importance for {Character}: {From} -> {To}. Reason: {Reason}",
                request.Character, request.From, request.To, request.Reason);

            character.Importance = newImportance;
        }
    }

    private static bool IsValidTransition(ImportanceChangeRequest request)
    {
        return (request.From == "arc_important" && request.To == "significant") ||
               (request.From == "significant" && request.To == "arc_important");
    }
}