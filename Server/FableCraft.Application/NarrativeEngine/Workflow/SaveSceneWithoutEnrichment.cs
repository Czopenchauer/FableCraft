using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;

using Microsoft.EntityFrameworkCore;

namespace FableCraft.Application.NarrativeEngine.Workflow;

internal sealed class SaveSceneWithoutEnrichment(IDbContextFactory<ApplicationDbContext> dbContextFactory) : IProcessor
{
    public async Task Invoke(GenerationContext context, CancellationToken cancellationToken)
    {
        var newScene = new Scene
        {
            Id = context.NewSceneId!.Value,
            SequenceNumber = (context.SceneContext.OrderByDescending(x => x.SequenceNumber)
                                  .FirstOrDefault()?.SequenceNumber
                              ?? 0)
                             + 1,
            AdventureId = context.AdventureId,
            NarrativeText = context.NewScene!.Scene,
            Metadata = new Metadata
            {
                ResolutionOutput = context.NewResolution,
                WriterObservation = context.NewScene.AdditionalData
            },
            AdventureSummary = null,
            CharacterActions = context.NewScene.Choices.Select(x => new MainCharacterAction
            {
                ActionDescription = x,
                Selected = false
            }).ToList(),
            Lorebooks = new List<LorebookEntry>(),
            CreatedAt = DateTimeOffset.UtcNow,
            EnrichmentStatus = EnrichmentStatus.NotEnriched,
            CommitStatus = CommitStatus.Uncommited
        };
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var adventure = await dbContext.Adventures.SingleAsync(x => x.Id == newScene.AdventureId, cancellationToken: cancellationToken);
        var lastScene = await dbContext.Scenes
            .Where(x => x.AdventureId == context.AdventureId)
            .Include(x => x.CharacterActions)
            .OrderByDescending(x => x.SequenceNumber)
            .FirstOrDefaultAsync(cancellationToken);
        var selectedAction = lastScene?.CharacterActions.FirstOrDefault(x => x.Selected);
        if (lastScene != null)
        {
            if (selectedAction != null)
            {
                selectedAction.Selected = true;
            }
            else
            {
                selectedAction = new MainCharacterAction
                {
                    ActionDescription = context.PlayerAction,
                    Selected = true
                };
                lastScene.CharacterActions.Add(selectedAction);
            }

            dbContext.Scenes.Update(lastScene);
        }

        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                adventure.Scenes.Add(newScene);
                adventure.LastPlayedAt = DateTimeOffset.UtcNow;
                dbContext.Update(adventure);
                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }
}