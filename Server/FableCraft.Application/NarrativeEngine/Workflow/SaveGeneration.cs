using System.Text.Json;

using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;
using FableCraft.Infrastructure.Queue;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace FableCraft.Application.NarrativeEngine.Workflow;

internal sealed class SaveGeneration(ApplicationDbContext dbContext, IMessageDispatcher messageDispatcher) : IProcessor
{
    public async Task Invoke(GenerationContext context, CancellationToken cancellationToken)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true
        };

        var newLoreEntities = context.NewLore!.Select(x => new LorebookEntry
        {
            AdventureId = context.AdventureId,
            Description = x.Summary,
            Category = x.Title,
            Content = JsonSerializer.Serialize(x, options),
            ContentType = ContentType.json,
        }).ToList();
        var newLocationsEntities = context.NewLocations!.Select(x => new LorebookEntry
        {
            AdventureId = context.AdventureId,
            Description = x.NarrativeData.ShortDescription,
            Content = JsonSerializer.Serialize(x, options),
            Category = x.EntityData.Name,
            ContentType = ContentType.json
        });
        newLoreEntities.AddRange(newLocationsEntities);

        var newCharactersEntities = context.NewCharacters!.Select(x => new Character()
        {
            AdventureId = context.AdventureId,
            CharacterId = x.CharacterId,
            Description = x.Description,
            CharacterStats = x.CharacterState,
            Tracker = x.CharacterTracker!,
            SequenceNumber = 0,
        }).ToList();

        var updatesToExistingCharacters = context.Characters.Select(x => new Character
        {
            AdventureId = context.AdventureId,
            CharacterId = x.CharacterId,
            Description = x.Description,
            CharacterStats = x.CharacterState,
            Tracker = x.CharacterTracker!,
            SequenceNumber = 0,
        });
        newCharactersEntities.AddRange(updatesToExistingCharacters);
        var newScene = new Scene
        {
            SequenceNumber = context.SceneContext.Max(x => x.SequenceNumber) + 1,
            AdventureId = context.AdventureId,
            NarrativeText = context.NewScene!.Scene,
            Metadata = new Metadata
            {
                Tracker = context.NewTracker!,
                NarrativeMetadata = context.NewNarrativeDirection!
            },
            AdventureSummary = null,
            CharacterActions = context.NewScene.Choices.Select(x => new MainCharacterAction()
            {
                ActionDescription = x,
                Selected = false
            }).ToList(),
            CharacterStates = newCharactersEntities.ToList(),
            Lorebooks = newLoreEntities,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        IExecutionStrategy strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                await dbContext.Scenes.AddAsync(newScene, cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);

                await messageDispatcher.PublishAsync(new SceneGeneratedEvent
                    {
                        AdventureId = context.AdventureId,
                        SceneId = newScene.Id
                    },
                    cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });

        context.GenerationProcessStep = GenerationProcessStep.Completed;
    }
}