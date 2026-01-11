using FableCraft.Application.NarrativeEngine;
using FableCraft.Infrastructure;
using FableCraft.Infrastructure.Clients;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;
using FableCraft.Infrastructure.Queue;

using Microsoft.EntityFrameworkCore;

namespace FableCraft.Application.AdventureGeneration;

public class AddAdventureToKnowledgeGraphCommand : IMessage
{
    public required Guid AdventureId { get; set; }
}

internal class AddAdventureToKnowledgeGraphCommandHandler(
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    IRagChunkService ragChunkService,
    SceneGenerationOrchestrator sceneGenerationOrchestrator)
    : IMessageHandler<AddAdventureToKnowledgeGraphCommand>
{
    public async Task HandleAsync(AddAdventureToKnowledgeGraphCommand message, CancellationToken cancellationToken)
    {
        ragChunkService.EnsureDirectoryExists(message.AdventureId);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var adventure = await dbContext.Adventures
            .Include(x => x.MainCharacter)
            .Include(x => x.Lorebook)
            .Include(x => x.Scenes)
            .ThenInclude(scene => scene.CharacterActions)
            .SingleAsync(x => x.Id == message.AdventureId, cancellationToken);

        if (adventure.RagProcessingStatus is not (ProcessingStatus.Pending or ProcessingStatus.Failed)
            && adventure.SceneGenerationStatus is not (ProcessingStatus.Pending or ProcessingStatus.Failed))
        {
            return;
        }

        var filesToCommit = new List<FileToWrite>();

        var existingLorebookChunks = await dbContext.Chunks
            .AsNoTracking()
            .Where(x => adventure.Lorebook.Select(y => y.Id).Contains(x.EntityId))
            .ToListAsync(cancellationToken);

        foreach (var lorebookEntry in adventure.Lorebook.OrderBy(x => x.Priority))
        {
            var existingChunk = existingLorebookChunks.SingleOrDefault(y => y.EntityId == lorebookEntry.Id);
            if (existingChunk is null)
            {
                var newLorebookChunk = ragChunkService.CreateChunk(
                    lorebookEntry.Id,
                    message.AdventureId,
                    lorebookEntry.Content,
                    lorebookEntry.ContentType);

                filesToCommit.Add(new FileToWrite(
                    newLorebookChunk,
                    lorebookEntry.Content,
                    [RagClientExtensions.GetWorldDatasetName(message.AdventureId)]));
            }
        }

        var existingCharacterChunks = await dbContext.Chunks
            .AsNoTracking()
            .Where(x => x.EntityId == adventure.MainCharacter.Id)
            .ToListAsync(cancellationToken);

        var characterContent = $"""
                                Name: {adventure.MainCharacter.Name}

                                {adventure.MainCharacter.Description}
                                """;

        var characterHash = ragChunkService.ComputeHash(characterContent);
        var existingCharacterChunk = existingCharacterChunks.FirstOrDefault(x => x.ContentHash == characterHash);

        if (existingCharacterChunk is null)
        {
            var newCharacterChunk = ragChunkService.CreateChunk(
                adventure.MainCharacter.Id,
                message.AdventureId,
                characterContent,
                ContentType.txt);

            filesToCommit.Add(new FileToWrite(
                newCharacterChunk,
                characterContent,
                [RagClientExtensions.GetMainCharacterDatasetName(message.AdventureId), RagClientExtensions.GetWorldDatasetName(message.AdventureId)]));
        }

        if (filesToCommit.Count > 0 && adventure.RagProcessingStatus is not ProcessingStatus.Completed)
        {
            await dbContext.Adventures.Where(x => x.Id == adventure.Id)
                .ExecuteUpdateAsync(x => x.SetProperty(a => a.RagProcessingStatus, ProcessingStatus.InProgress),
                    cancellationToken);

            var strategy = dbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    ragChunkService.EnsureDirectoryExists(message.AdventureId);
                    await ragChunkService.WriteFilesAsync(filesToCommit, cancellationToken);
                    await ragChunkService.CommitChunksToRagAsync(filesToCommit, cancellationToken);

                    var worldDataset = RagClientExtensions.GetWorldDatasetName(message.AdventureId);
                    var mainCharacterDataset = RagClientExtensions.GetMainCharacterDatasetName(message.AdventureId);

                    await ragChunkService.CognifyDatasetsAsync([worldDataset, mainCharacterDataset], cancellationToken: cancellationToken);

                    foreach (var file in filesToCommit)
                    {
                        dbContext.Chunks.Add(file.Chunk);
                    }

                    adventure.RagProcessingStatus = ProcessingStatus.Completed;
                    await dbContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    adventure.RagProcessingStatus = ProcessingStatus.Failed;
                    dbContext.Adventures.Update(adventure);
                    await dbContext.SaveChangesAsync(cancellationToken);
                    throw;
                }
            });
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