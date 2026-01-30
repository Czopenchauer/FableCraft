using FableCraft.Application.NarrativeEngine;
using FableCraft.Infrastructure;
using FableCraft.Infrastructure.Clients;
using FableCraft.Infrastructure.Docker;
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
    SceneGenerationOrchestrator sceneGenerationOrchestrator,
    IAdventureRagManager ragManager,
    IRagChunkService ragChunkService,
    IRagClientFactory ragClientFactory,
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

        if (adventure.RagProcessingStatus is not (ProcessingStatus.Pending or ProcessingStatus.Failed)
            && adventure.SceneGenerationStatus is not (ProcessingStatus.Pending or ProcessingStatus.Failed))
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

            logger.Information(
                "Processing adventure {AdventureId} with volume isolation from worldbook {WorldbookId}",
                adventure.Id,
                adventure.WorldbookId);

            string[] mainCharacterDatasets = [RagClientExtensions.GetMainCharacterDatasetName(), RagClientExtensions.GetWorldDatasetName()];
            string[] worldDatasets = [RagClientExtensions.GetWorldDatasetName()];

            List<ChunkCreationRequest> chunkRequests = [new(
                                                           adventure.MainCharacter.Id,
                                                           FormatMainCharacterDescription(adventure.MainCharacter),
                                                           ContentType.txt,
                                                           mainCharacterDatasets)];

            var extraLoreEntries = adventure.Lorebook
                .Select(l => new ChunkCreationRequest(l.Id, l.Content, l.ContentType, worldDatasets))
                .ToList();
            chunkRequests.AddRange(extraLoreEntries);

            logger.Information(
                "Initializing adventure {AdventureId} from worldbook {WorldbookId} with {ExtraLoreCount} extra lore entries",
                adventure.Id,
                adventure.WorldbookId,
                extraLoreEntries?.Count ?? 0);

            try
            {
                await ragManager.InitializeFromWorldbook(adventure, cancellationToken);
                var chunks = await ragChunkService.CreateChunk(chunkRequests, adventure.Id, cancellationToken);
                var ragBuilder = await ragClientFactory.CreateBuildClientForAdventure(adventure.Id, cancellationToken);
                await ragChunkService.CommitChunksToRagAsync(ragBuilder, chunks, cancellationToken);
                await ragChunkService.CognifyDatasetsAsync(ragBuilder, mainCharacterDatasets, cancellationToken: cancellationToken);

                logger.Information(
                    "Successfully initialized adventure {AdventureId} from worldbook {WorldbookId}",
                    adventure.Id,
                    adventure.WorldbookId);
            }
            catch (Exception ex)
            {
                logger.Error(ex,
                    "Failed to initialize adventure {AdventureId} from worldbook {WorldbookId}",
                    adventure.Id,
                    adventure.WorldbookId);

                throw;
            }

            await dbContext.Adventures.Where(x => x.Id == message.AdventureId)
                .ExecuteUpdateAsync(x => x.SetProperty(z => z.RagProcessingStatus, ProcessingStatus.Completed), cancellationToken: cancellationToken);
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
                await sceneGenerationOrchestrator.GenerateSceneAsync(adventure.Id, string.Empty, cancellationToken);
                await dbContext.Adventures.Where(x => x.Id == adventure.Id)
                    .ExecuteUpdateAsync(x => x.SetProperty(a => a.SceneGenerationStatus, ProcessingStatus.Completed), cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch (Exception)
            {
                await transaction.RollbackAsync(cancellationToken);
                await dbContext.Adventures.Where(x => x.Id == adventure.Id)
                    .ExecuteUpdateAsync(x => x.SetProperty(a => a.SceneGenerationStatus, ProcessingStatus.Failed), cancellationToken);
                throw;
            }
        });
    }

    private static string FormatMainCharacterDescription(MainCharacter mc) =>
        $"Name: {mc.Name}\n\n{mc.Description}";
}