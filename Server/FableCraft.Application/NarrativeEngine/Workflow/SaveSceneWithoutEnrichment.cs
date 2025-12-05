using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace FableCraft.Application.NarrativeEngine.Workflow;

internal sealed class SaveSceneWithoutEnrichment(IDbContextFactory<ApplicationDbContext> dbContextFactory) : IProcessor
{
    public async Task Invoke(GenerationContext context, CancellationToken cancellationToken)
    {
        var newScene = new Scene
        {
            SequenceNumber = (context.SceneContext.OrderByDescending(x => x.SequenceNumber)
                                  .FirstOrDefault()?.SequenceNumber
                              ?? 0)
                             + 1,
            AdventureId = context.AdventureId,
            NarrativeText = context.NewScene!.Scene,
            Metadata = new Metadata
            {
                NarrativeMetadata = context.NewNarrativeDirection!,
                Tracker = new Tracker
                {
                    Story = new StoryTracker
                    {
                        Location = "",
                        Weather = "",
                        Time = DateTime.UtcNow
                    },
                    CharactersPresent = Array.Empty<string>()
                }
            },
            AdventureSummary = null,
            CharacterActions = context.NewScene.Choices.Select(x => new MainCharacterAction
            {
                ActionDescription = x,
                Selected = false
            }).ToList(),
            CharacterStates = new List<Character>(),
            Lorebooks = new List<LorebookEntry>(),
            CreatedAt = DateTimeOffset.UtcNow,
            EnrichmentStatus = EnrichmentStatus.NotEnriched,
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

        context.NewSceneId = newScene.Id;
        context.GenerationProcessStep = GenerationProcessStep.SceneSavedAwaitingEnrichment;
    }
}