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
        var scene = dbContext
            .Scenes
            .Include(x => x.CharacterStates)
            .Include(x => x.Lorebooks)
            .Single(x => x.Id == context.NewSceneId && x.AdventureId == context.AdventureId);

        scene.Metadata.Tracker = context.NewTracker!;
        scene.EnrichmentStatus = EnrichmentStatus.Enriched;

        var newCharacterEntities = context.NewCharacters?.Select(x => new Character
                                   {
                                       AdventureId = context.AdventureId,
                                       CharacterId = x.CharacterId,
                                       Description = x.Description,
                                       CharacterStats = x.CharacterState,
                                       Tracker = x.CharacterTracker!,
                                       DevelopmentTracker = x.DevelopmentTracker!,
                                       SequenceNumber = scene.SequenceNumber,
                                       SceneId = scene.Id
                                   }).ToList()
                                   ?? new List<Character>();

        var updatedCharacterEntities = context.CharacterUpdates?.Select(x => new Character
                                       {
                                           AdventureId = context.AdventureId,
                                           CharacterId = x.CharacterId,
                                           Description = x.Description,
                                           CharacterStats = x.CharacterState,
                                           Tracker = x.CharacterTracker!,
                                           DevelopmentTracker = x.DevelopmentTracker!,
                                           SequenceNumber = scene.SequenceNumber,
                                           SceneId = scene.Id
                                       }).ToList()
                                       ?? new List<Character>();

        newCharacterEntities.AddRange(updatedCharacterEntities);
        scene.CharacterStates = newCharacterEntities;

        var loreEntities = context.NewLore?.Select(x => new LorebookEntry
                           {
                               AdventureId = context.AdventureId,
                               Description = x.Description,
                               Category = x.Title,
                               Content = x.ToJsonString(),
                               ContentType = ContentType.json
                           }).ToList()
                           ?? new List<LorebookEntry>();

        var locationEntities = context.NewLocations?.Select(x => new LorebookEntry
                               {
                                   AdventureId = context.AdventureId,
                                   Description = x.Description,
                                   Content = x.ToJsonString(),
                                   Category = x.Title,
                                   ContentType = ContentType.json
                               }).ToList()
                               ?? new List<LorebookEntry>();

        var itemsEntities = context.NewItems?.Select(x => new LorebookEntry
                            {
                                AdventureId = context.AdventureId,
                                Description = x.Description,
                                Content = x.ToJsonString(),
                                Category = x.Name,
                                ContentType = ContentType.json
                            }).ToList()
                            ?? new List<LorebookEntry>();

        loreEntities.AddRange(locationEntities);
        loreEntities.AddRange(itemsEntities);
        foreach (LorebookEntry loreEntry in loreEntities)
        {
            scene.Lorebooks.Add(loreEntry);
        }
        
        IExecutionStrategy strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            var generationProcess = await dbContext.GenerationProcesses.SingleAsync(x => x.AdventureId == context.AdventureId, cancellationToken: cancellationToken);
            dbContext.Scenes.Update(scene);
            dbContext.GenerationProcesses.Remove(generationProcess);
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
}