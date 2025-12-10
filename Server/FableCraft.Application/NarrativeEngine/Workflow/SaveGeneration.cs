using System.Text.Json;

using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;
using FableCraft.Infrastructure.Queue;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace FableCraft.Application.NarrativeEngine.Workflow;

internal sealed class SaveGeneration(IDbContextFactory<ApplicationDbContext> dbContextFactory, IMessageDispatcher messageDispatcher) : IProcessor
{
    public async Task Invoke(GenerationContext context, CancellationToken cancellationToken)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true
        };

        var newLoreEntities = context.NewLore?.Select(x => new LorebookEntry
                              {
                                  AdventureId = context.AdventureId,
                                  Description = x.Summary,
                                  Category = x.Title,
                                  Content = JsonSerializer.Serialize(x, options),
                                  ContentType = ContentType.json
                              }).ToList()
                              ?? new List<LorebookEntry>();
        var newLocationsEntities = context.NewLocations?.Select(x => new LorebookEntry
                                   {
                                       AdventureId = context.AdventureId,
                                       Description = x.NarrativeData.ShortDescription,
                                       Content = JsonSerializer.Serialize(x, options),
                                       Category = x.EntityData.Name,
                                       ContentType = ContentType.json
                                   }).ToList()
                                   ?? new List<LorebookEntry>();
        newLoreEntities.AddRange(newLocationsEntities);

        var newCharactersEntities = context.NewCharacters?.Select(x => new Character
                                    {
                                        AdventureId = context.AdventureId,
                                        CharacterId = x.CharacterId,
                                        Description = x.Description,
                                        CharacterStats = x.CharacterState,
                                        Tracker = x.CharacterTracker!,
                                        SequenceNumber = x.SequenceNumber,
                                        DevelopmentTracker = x.DevelopmentTracker!
                                    }).ToList()
                                    ?? new List<Character>();

        var updatesToExistingCharacters = context.CharacterUpdates?.Select(x => new Character
                                          {
                                              AdventureId = context.AdventureId,
                                              CharacterId = x.CharacterId,
                                              Description = x.Description,
                                              CharacterStats = x.CharacterState,
                                              Tracker = x.CharacterTracker!,
                                              SequenceNumber = x.SequenceNumber,
                                              DevelopmentTracker = x.DevelopmentTracker!
                                          })
                                          ?? new List<Character>();
        newCharactersEntities.AddRange(updatesToExistingCharacters);
        var newScene = new Scene
        {
            SequenceNumber = (context.SceneContext.OrderByDescending(x => x.SequenceNumber).FirstOrDefault()?.SequenceNumber ?? 0) + 1,
            AdventureId = context.AdventureId,
            NarrativeText = context.NewScene!.Scene,
            Metadata = new Metadata
            {
                Tracker = context.NewTracker!,
                NarrativeMetadata = context.NewNarrativeDirection!
            },
            AdventureSummary = null,
            CharacterActions = context.NewScene.Choices.Select(x => new MainCharacterAction
            {
                ActionDescription = x,
                Selected = false
            }).ToList(),
            CharacterStates = newCharactersEntities.ToList(),
            Lorebooks = newLoreEntities,
            CreatedAt = DateTimeOffset.UtcNow,
            EnrichmentStatus = EnrichmentStatus.Enriched,
            CommitStatus = CommitStatus.Uncommited
        };

        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        IExecutionStrategy strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                await dbContext.Scenes.AddAsync(newScene, cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });

        await messageDispatcher.PublishAsync(new SceneGeneratedEvent
            {
                AdventureId = context.AdventureId,
                SceneId = newScene.Id
            },
            cancellationToken);
        context.GenerationProcessStep = GenerationProcessStep.Completed;
    }
}