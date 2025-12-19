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

            await UpsertCharacters(context, cancellationToken, dbContext);

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

    private async static Task UpsertCharacters(GenerationContext context, CancellationToken cancellationToken, ApplicationDbContext dbContext)
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
                    Description =  update.Description,
                    CharacterStats = update.CharacterState,
                    Tracker = update.CharacterTracker!,
                    SequenceNumber = character.Version + 1,
                    SceneId = context.NewSceneId!.Value
                });
                character.Version += 1;
                character.CharacterMemories.AddRange(update.CharacterMemories);
                character.CharacterRelationships.AddRange(update.Relationships);
                character.CharacterSceneRewrites.AddRange(update.SceneRewrites);
                dbContext.Characters.Update(character);
            }
        }

        if (context.NewCharacters?.Length > 0)
        {
            var newCharacterEntities = context.NewCharacters?.Select(x => new Character
                                       {
                                           AdventureId = context.AdventureId,
                                           Name = x.Name,
                                           CharacterStates =
                                           [
                                               new()
                                               {
                                                   Description = x.Description,
                                                   CharacterStats = x.CharacterState,
                                                   Tracker = x.CharacterTracker!,
                                                   SequenceNumber = 0,
                                                   SceneId = context.NewSceneId!.Value,
                                               }
                                           ],
                                           Version = 0,
                                           CharacterMemories = x.CharacterMemories,
                                           CharacterRelationships = x.Relationships,
                                           CharacterSceneRewrites = x.SceneRewrites,
                                       }).ToList()
                                       ?? new List<Character>();

            dbContext.Characters.AddRange(newCharacterEntities);
        }
    }
}