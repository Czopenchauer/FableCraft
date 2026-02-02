using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;
using FableCraft.Infrastructure.Queue;

using Microsoft.EntityFrameworkCore;

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
            .SingleAsync(x => x.Id == context.NewSceneId && x.AdventureId == context.AdventureId, cancellationToken);

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
                BackgroundRoster = context.ContextGathered.BackgroundRoster,
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
                                                  Content = x.Description,
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

        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            var generationProcess = await dbContext.GenerationProcesses.FirstOrDefaultAsync(x => x.AdventureId == context.AdventureId, cancellationToken);
            if (generationProcess != null)
            {
                dbContext.GenerationProcesses.Remove(generationProcess);
            }

            dbContext.Scenes.Update(scene);

            await UpsertCharacters(context, scene, cancellationToken, dbContext);

            // Link pre-scene custom characters to the first scene
            if (scene.SequenceNumber == 0)
            {
                await LinkPreSceneCharactersToFirstScene(context.AdventureId, scene, dbContext, cancellationToken);
            }

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

    private static async Task UpsertCharacters(GenerationContext context, Scene scene, CancellationToken cancellationToken, ApplicationDbContext dbContext)
    {
        List<CharacterContext> characterUpdates;
        List<CharacterContext> newCharacters;
        lock (context)
        {
            characterUpdates = context.CharacterUpdates?.ToList() ?? [];
            newCharacters = context.NewCharacters?.ToList() ?? [];
        }

        if (context.NewBackgroundCharacters.Count > 0)
        {
            var backgroundCharacterEntities = context.NewBackgroundCharacters.Select(x => new BackgroundCharacter
            {
                AdventureId = context.AdventureId,
                Description = x.Description,
                SceneId = scene.Id,
                Scene = scene,
                Name = x.Name,
                Identity = x.Identity,
                LastLocation = context.NewTracker!.Scene!.Location,
                LastSeenTime = context.NewTracker!.Scene!.Time,
                Version = 0,
                ConvertedToFull = false
            }).ToList();
            dbContext.BackgroundCharacters.AddRange(backgroundCharacterEntities);
        }

        var backgroundCharacters = await dbContext.BackgroundCharacters
            .Where(x => context.NewTracker!.Scene!.CharactersPresent.Except(context.NewBackgroundCharacters.Select(z => z.Name)).Contains(x.Name))
            .ToListAsync(cancellationToken: cancellationToken);

        foreach (BackgroundCharacter backgroundCharacter in backgroundCharacters)
        {
            var newBackgroundState = new BackgroundCharacter
            {
                AdventureId = context.AdventureId,
                Description = backgroundCharacter.Description,
                SceneId = scene.Id,
                Scene = scene,
                Name = backgroundCharacter.Name,
                Identity = backgroundCharacter.Identity,
                LastLocation = context.NewTracker!.Scene!.Location,
                LastSeenTime = context.NewTracker!.Scene!.Time,
                Version = backgroundCharacter.Version + 1,
                ConvertedToFull = context.NewCharacters?.Any(c => c.Name.Contains(backgroundCharacter.Name)) ?? false
            };
            dbContext.BackgroundCharacters.Add(newBackgroundState);
        }

        if (characterUpdates.Count > 0)
        {
            var updateNames = characterUpdates.Select(y => y.Name).ToList();
            var characters = await dbContext
                .Characters
                .Where(x => x.AdventureId == context.AdventureId && updateNames.Contains(x.Name))
                .ToListAsync(cancellationToken);

            foreach (var character in characters)
            {
                var update = characterUpdates.Single(x => x.Name == character.Name);
                character.CharacterStates.Add(new CharacterState
                {
                    CharacterStats = update.CharacterState,
                    Tracker = update.CharacterTracker!,
                    SequenceNumber = character.Version + 1,
                    Scene = scene,
                    IsDead = update.IsDead,
                });
                character.Version += 1;
                var memories = update.CharacterMemories.Select(x => new CharacterMemory
                {
                    SceneTracker = x.SceneTracker,
                    Scene = scene,
                    Summary = x.MemoryContent,
                    Data = x.Data,
                    Salience = x.Salience
                });

                var relationships = update.Relationships.Select(x => new CharacterRelationship
                {
                    TargetCharacterName = x.TargetCharacterName,
                    Data = x.Data,
                    SequenceNumber = x.SequenceNumber,
                    Scene = scene,
                    UpdateTime = x.UpdateTime,
                    Dynamic = x.Dynamic
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
                .ToListAsync(cancellationToken);

            foreach (var contextNewCharacter in newCharacters)
            {
                var memories = contextNewCharacter.CharacterMemories.Select(x => new CharacterMemory
                {
                    SceneTracker = x.SceneTracker,
                    Scene = scene,
                    Summary = x.MemoryContent,
                    Data = x.Data,
                    Salience = x.Salience
                }).ToList();

                var relationships = contextNewCharacter.Relationships.Select(x => new CharacterRelationship
                {
                    TargetCharacterName = x.TargetCharacterName,
                    Data = x.Data,
                    SequenceNumber = x.SequenceNumber,
                    Scene = scene,
                    UpdateTime = x.UpdateTime,
                    Dynamic = x.Dynamic
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
                            new CharacterState
                            {
                                CharacterStats = contextNewCharacter.CharacterState,
                                Tracker = contextNewCharacter.CharacterTracker!,
                                SequenceNumber = 0,
                                Scene = scene,
                                IsDead = false
                            }
                        ],
                        Version = 0,
                        CharacterMemories = memories,
                        CharacterRelationships = relationships,
                        CharacterSceneRewrites = sceneRewrites,
                        Importance = contextNewCharacter.Importance,
                        IntroductionScene = scene.Id,
                        Scene = scene,
                        Description = contextNewCharacter.Description
                    };
                    dbContext.Characters.Add(newChar);
                }
            }
        }
    }

    private static async Task MarkCharacterEventsConsumed(GenerationContext context, CancellationToken cancellationToken, ApplicationDbContext dbContext)
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

    private static async Task ProcessImportanceFlags(
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
                "Invalid importance transition requested for {Character}: {From} -> {To}. " + "Only arc_important <-> significant transitions are supported.",
                invalid.Character,
                invalid.From,
                invalid.To);
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
                request.Character,
                request.From,
                request.To,
                request.Reason);

            character.Importance = newImportance;
        }
    }

    private static bool IsValidTransition(ImportanceChangeRequest request) =>
        request.From == "arc_important" && request.To == "significant" || request.From == "significant" && request.To == "arc_important";

    /// <summary>
    ///     Links pre-scene custom characters (those with null IntroductionScene) to the first scene.
    ///     This updates the Character, CharacterState, and CharacterRelationship records to point to the first scene.
    /// </summary>
    private async Task LinkPreSceneCharactersToFirstScene(
        Guid adventureId,
        Scene firstScene,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        // Find all characters with null IntroductionScene (pre-scene custom characters)
        var preSceneCharacters = await dbContext.Characters
            .Include(c => c.CharacterStates)
            .Include(c => c.CharacterRelationships)
            .Where(c => c.AdventureId == adventureId && c.IntroductionScene == null)
            .ToListAsync(cancellationToken);

        if (preSceneCharacters.Count == 0)
        {
            return;
        }

        logger.Information("Linking {Count} pre-scene custom characters to first scene {SceneId}",
            preSceneCharacters.Count, firstScene.Id);

        foreach (var character in preSceneCharacters)
        {
            character.IntroductionScene = firstScene.Id;
            character.Scene = firstScene;

            foreach (var state in character.CharacterStates.Where(s => s.SceneId == null))
            {
                state.SceneId = firstScene.Id;
                state.Scene = firstScene;
            }

            foreach (var relationship in character.CharacterRelationships.Where(r => r.SceneId == null))
            {
                relationship.SceneId = firstScene.Id;
                relationship.Scene = firstScene;
            }

            dbContext.Characters.Update(character);
        }
    }
}