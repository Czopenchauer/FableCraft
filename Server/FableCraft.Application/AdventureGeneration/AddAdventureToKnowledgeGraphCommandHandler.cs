using FableCraft.Application.NarrativeEngine;
using FableCraft.Infrastructure;
using FableCraft.Infrastructure.Clients;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;
using FableCraft.Infrastructure.Queue;

using Microsoft.EntityFrameworkCore;

using Serilog;

namespace FableCraft.Application.AdventureGeneration;

public class AddAdventureToKnowledgeGraphCommand : IMessage
{
    public required Guid AdventureId { get; set; }
}

internal class AddAdventureToKnowledgeGraphCommandHandler(
    ApplicationDbContext dbContext,
    IRagChunkService ragChunkService,
    SceneGenerationOrchestrator sceneGenerationOrchestrator,
    ILogger logger)
    : IMessageHandler<AddAdventureToKnowledgeGraphCommand>
{
    public async Task HandleAsync(AddAdventureToKnowledgeGraphCommand message, CancellationToken cancellationToken)
    {
        var adventure = await dbContext.Adventures
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == message.AdventureId, cancellationToken);

        if (adventure is null)
        {
            logger.Information("Adventure not found");
            return;
        }

        if ((adventure.RagProcessingStatus is not (ProcessingStatus.Pending or ProcessingStatus.Failed)
             && adventure.SceneGenerationStatus is not (ProcessingStatus.Pending or ProcessingStatus.Failed)))
        {
            logger.Information("Skipping adding adventure due to it being in {ragState} RagState and scene state {sceneState}",
                adventure.RagProcessingStatus,
                adventure.SceneGenerationStatus);
            return;
        }

        try
        {
            await dbContext.Adventures.Where(x => x.Id == message.AdventureId)
                .ExecuteUpdateAsync(x => x.SetProperty(z => z.RagProcessingStatus, ProcessingStatus.InProgress), cancellationToken: cancellationToken);

            adventure = await dbContext.Adventures
                .Include(x => x.MainCharacter)
                .Include(x => x.Lorebook)
                .Include(x => x.Scenes)
                .ThenInclude(scene => scene.CharacterActions)
                .SingleAsync(x => x.Id == message.AdventureId, cancellationToken);

            var creationRequests = new List<ChunkCreationRequest>();
            foreach (var lorebookEntry in adventure.Lorebook)
            {
                creationRequests.Add(new ChunkCreationRequest(lorebookEntry.Id,
                    lorebookEntry.Content,
                    lorebookEntry.ContentType,
                    [RagClientExtensions.GetWorldDatasetName(message.AdventureId)]));
            }

            string[] allDatasets = [RagClientExtensions.GetMainCharacterDatasetName(message.AdventureId), RagClientExtensions.GetWorldDatasetName(message.AdventureId)];
            var mainCharacter = adventure.MainCharacter;
            creationRequests.Add(new ChunkCreationRequest(mainCharacter.Id,
                RagClientExtensions.GetMainCharacterDescription(mainCharacter),
                ContentType.txt,
                allDatasets));

            var strategy = dbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

                var chunks = await ragChunkService.CreateChunk(creationRequests, message.AdventureId, cancellationToken);
                await ragChunkService.CommitChunksToRagAsync(chunks, cancellationToken);

                await ragChunkService.CognifyDatasetsAsync(allDatasets, cancellationToken: cancellationToken);

                adventure.RagProcessingStatus = ProcessingStatus.Completed;

                dbContext.AddRange(chunks);
                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            });
        }
        catch (Exception)
        {
            await dbContext.Adventures.Where(x => x.Id == message.AdventureId)
                .ExecuteUpdateAsync(x => x.SetProperty(z => z.RagProcessingStatus, ProcessingStatus.Failed), cancellationToken: cancellationToken);
            throw;
        }

        await dbContext.Adventures.Where(x => x.Id == adventure.Id)
            .ExecuteUpdateAsync(x => x.SetProperty(a => a.SceneGenerationStatus, ProcessingStatus.InProgress),
                cancellationToken);

        var executionStrategy = dbContext.Database.CreateExecutionStrategy();
        await executionStrategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                await sceneGenerationOrchestrator.GenerateSceneAsync(message.AdventureId, string.Empty, cancellationToken);
                adventure.SceneGenerationStatus = ProcessingStatus.Completed;
                dbContext.Adventures.Update(adventure);
                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch (Exception)
            {
                await transaction.RollbackAsync(cancellationToken);
                adventure.SceneGenerationStatus = ProcessingStatus.Failed;
                dbContext.Adventures.Update(adventure);
                await dbContext.SaveChangesAsync(cancellationToken);
                throw;
            }
        });
    }
}