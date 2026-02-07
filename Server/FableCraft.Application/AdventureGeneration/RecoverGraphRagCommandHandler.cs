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

public class RecoverGraphRagCommand : IMessage
{
    public required Guid AdventureId { get; set; }

    /// <summary>
    /// Optional: reassign worldbook before recreating the GraphRAG volume.
    /// </summary>
    public Guid? NewWorldbookId { get; init; }
}

internal class RecoverGraphRagCommandHandler(
    ApplicationDbContext dbContext,
    IAdventureRagManager ragManager,
    IRagChunkService ragChunkService,
    IRagClientFactory ragClientFactory,
    ILogger logger)
    : IMessageHandler<RecoverGraphRagCommand>
{
    public async Task HandleAsync(RecoverGraphRagCommand message, CancellationToken cancellationToken)
    {
        var adventureId = message.AdventureId;

        logger.Information("Starting GraphRAG recovery for adventure {AdventureId}", adventureId);

        try
        {
            if (message.NewWorldbookId.HasValue)
            {
                var worldbookExists = await dbContext.Worldbooks
                    .AnyAsync(x => x.Id == message.NewWorldbookId.Value, cancellationToken);

                if (!worldbookExists)
                {
                    logger.Error("Worldbook {WorldbookId} not found for adventure recovery", message.NewWorldbookId.Value);
                    throw new InvalidOperationException($"Worldbook {message.NewWorldbookId.Value} not found");
                }

                await dbContext.Adventures
                    .Where(x => x.Id == adventureId)
                    .ExecuteUpdateAsync(x => x.SetProperty(a => a.WorldbookId, message.NewWorldbookId.Value), cancellationToken);

                logger.Information("Updated adventure {AdventureId} to use worldbook {WorldbookId}",
                    adventureId, message.NewWorldbookId.Value);
            }

            await dbContext.Adventures
                .Where(x => x.Id == adventureId)
                .ExecuteUpdateAsync(x => x.SetProperty(a => a.RagProcessingStatus, ProcessingStatus.InProgress), cancellationToken);

            var adventure = await dbContext.Adventures
                .AsSplitQuery()
                .Include(x => x.MainCharacter)
                .Include(x => x.Lorebook)
                .Include(x => x.Characters)
                .Include(x => x.Scenes)
                    .ThenInclude(s => s.Lorebooks)
                .Include(x => x.Scenes)
                    .ThenInclude(s => s.CharacterSceneRewrites)
                .SingleOrDefaultAsync(x => x.Id == adventureId, cancellationToken);

            if (adventure is null)
            {
                logger.Error("Adventure {AdventureId} not found", adventureId);
                throw new InvalidOperationException($"Adventure {adventureId} not found");
            }

            logger.Information("Recreating GraphRAG volume for adventure {AdventureId} from worldbook {WorldbookId}",
                adventureId, adventure.WorldbookId);

            await ragManager.RecreateFromWorldbook(adventure, cancellationToken);

            var deletedChunks = await dbContext.Chunks
                .Where(x => x.AdventureId == adventureId)
                .ExecuteDeleteAsync(cancellationToken);

            logger.Information("Deleted {ChunkCount} existing chunks for adventure {AdventureId}",
                deletedChunks, adventureId);

            await dbContext.Scenes
                .Where(x => x.AdventureId == adventureId)
                .ExecuteUpdateAsync(x => x.SetProperty(s => s.CommitStatus, CommitStatus.Uncommited), cancellationToken);

            string[] mainCharacterDatasets = [RagClientExtensions.GetMainCharacterDatasetName(), RagClientExtensions.GetWorldDatasetName()];
            string[] worldDatasets = [RagClientExtensions.GetWorldDatasetName()];

            var initialChunkRequests = new List<ChunkCreationRequest>
            {
                new(adventure.MainCharacter.Id,
                    RagClientExtensions.GetMainCharacterDescription(adventure.MainCharacter),
                    ContentType.txt,
                    mainCharacterDatasets)
            };

            var lorebookChunks = adventure.Lorebook
                .Select(l => new ChunkCreationRequest(l.Id, l.Content, l.ContentType, worldDatasets))
                .ToList();
            initialChunkRequests.AddRange(lorebookChunks);

            logger.Information("Creating initial chunks for adventure {AdventureId}: 1 main character + {LorebookCount} lorebook entries",
                adventureId, lorebookChunks.Count);

            var ragBuilder = await ragClientFactory.CreateBuildClientForAdventure(adventureId, cancellationToken);
            var initialChunks = await ragChunkService.CreateChunk(initialChunkRequests, adventureId, cancellationToken);
            await ragChunkService.CommitChunksToRagAsync(ragBuilder, initialChunks, cancellationToken);
            await ragChunkService.CognifyDatasetsAsync(ragBuilder, mainCharacterDatasets, cancellationToken: cancellationToken);

            dbContext.Chunks.AddRange(initialChunks);
            await dbContext.SaveChangesAsync(cancellationToken);

            var scenesToCommit = adventure.Scenes
                .OrderBy(s => s.SequenceNumber)
                .ToList();

            if (scenesToCommit.Count == 0)
            {
                logger.Information("No scenes to commit for adventure {AdventureId}", adventureId);
                await dbContext.Adventures
                    .Where(x => x.Id == adventureId)
                    .ExecuteUpdateAsync(x => x.SetProperty(a => a.RagProcessingStatus, ProcessingStatus.Completed), cancellationToken);

                logger.Information("Successfully recovered GraphRAG for adventure {AdventureId} (no scenes)", adventureId);
                return;
            }

            var sceneChunkRequests = new List<ChunkCreationRequest>();
            var characters = adventure.Characters;

            foreach (var scene in scenesToCommit)
            {
                var sceneLorebookRequests = scene.Lorebooks.Select(x =>
                    new ChunkCreationRequest(x.Id,
                        $"""
                         {x.Title}
                         {x.Content}
                         {x.Category}
                         """,
                        x.ContentType,
                        [RagClientExtensions.GetWorldDatasetName()]));
                sceneChunkRequests.AddRange(sceneLorebookRequests);

                if (scene.Metadata?.Tracker?.MainCharacter?.MainCharacter != null &&
                    scene.Metadata?.Tracker?.Scene != null)
                {
                    var sceneContent = $"""
                                        Character is: {scene.Metadata.Tracker.MainCharacter.MainCharacter.Name}
                                        Time: {scene.Metadata.Tracker.Scene.Time}
                                        Location: {scene.Metadata.Tracker.Scene.Location}
                                        Weather: {scene.Metadata.Tracker.Scene.Weather}
                                        Characters on scene: {string.Join(", ", scene.Metadata.Tracker.Scene.CharactersPresent)}

                                        {scene.NarrativeText}
                                        """;
                    sceneChunkRequests.Add(new ChunkCreationRequest(scene.Id, sceneContent, ContentType.txt,
                        [RagClientExtensions.GetMainCharacterDatasetName()]));
                }
                else
                {
                    sceneChunkRequests.Add(new ChunkCreationRequest(scene.Id, scene.NarrativeText, ContentType.txt,
                        [RagClientExtensions.GetMainCharacterDatasetName()]));
                }

                foreach (var rewrite in scene.CharacterSceneRewrites)
                {
                    var character = characters.SingleOrDefault(c => c.Id == rewrite.CharacterId);
                    if (character == null)
                    {
                        continue;
                    }

                    var content = $"""
                                   Character: {character.Name}
                                   Time: {rewrite.SceneTracker.Time}
                                   Location: {rewrite.SceneTracker.Location}
                                   Weather: {rewrite.SceneTracker.Weather}
                                   Characters on scene: {string.Join(", ", rewrite.SceneTracker.CharactersPresent)}

                                   {rewrite.Content}
                                   """;
                    sceneChunkRequests.Add(new ChunkCreationRequest(rewrite.Id,
                        content,
                        ContentType.txt,
                        [RagClientExtensions.GetCharacterDatasetName(rewrite.CharacterId)]));
                }
            }

            var allDatasets = characters
                .Select(x => RagClientExtensions.GetCharacterDatasetName(x.Id))
                .Append(RagClientExtensions.GetMainCharacterDatasetName())
                .Append(RagClientExtensions.GetWorldDatasetName())
                .ToArray();

            foreach (var character in characters)
            {
                sceneChunkRequests.Add(new ChunkCreationRequest(character.Id,
                    RagClientExtensions.GetCharacterDescription(character),
                    ContentType.txt,
                    allDatasets));
            }

            sceneChunkRequests.Add(new ChunkCreationRequest(adventure.MainCharacter.Id,
                RagClientExtensions.GetMainCharacterDescription(adventure.MainCharacter),
                ContentType.txt,
                allDatasets));

            logger.Information("Committing {ChunkCount} chunks for {SceneCount} scenes in adventure {AdventureId}",
                sceneChunkRequests.Count, scenesToCommit.Count, adventureId);

            var strategy = dbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    var sceneChunks = await ragChunkService.CreateChunk(sceneChunkRequests, adventureId, cancellationToken);
                    await ragChunkService.CommitChunksToRagAsync(ragBuilder, sceneChunks, cancellationToken);
                    await ragChunkService.CognifyDatasetsAsync(ragBuilder, allDatasets, cancellationToken: cancellationToken);

                    await dbContext.Scenes
                        .Where(x => x.AdventureId == adventureId)
                        .ExecuteUpdateAsync(x => x.SetProperty(s => s.CommitStatus, CommitStatus.Commited), cancellationToken);

                    var existingChunks = await dbContext.Chunks
                        .Where(x => x.AdventureId == adventureId)
                        .ToListAsync(cancellationToken);

                    var newChunks = sceneChunks
                        .Where(sc => !existingChunks.Any(ec =>
                            ec.EntityId == sc.EntityId &&
                            ec.ContentHash == sc.ContentHash &&
                            ec.DatasetName == sc.DatasetName))
                        .ToList();

                    dbContext.Chunks.AddRange(newChunks);
                    await dbContext.SaveChangesAsync(cancellationToken);

                    await transaction.CommitAsync(cancellationToken);
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
            });

            await dbContext.Adventures
                .Where(x => x.Id == adventureId)
                .ExecuteUpdateAsync(x => x.SetProperty(a => a.RagProcessingStatus, ProcessingStatus.Completed), cancellationToken);

            logger.Information("Successfully recovered GraphRAG for adventure {AdventureId}", adventureId);
        }
        catch (Exception ex)
        {
            await dbContext.Adventures
                .Where(x => x.Id == adventureId)
                .ExecuteUpdateAsync(x => x.SetProperty(a => a.RagProcessingStatus, ProcessingStatus.Failed), cancellationToken);

            await dbContext.Scenes
                .Where(x => x.AdventureId == adventureId)
                .ExecuteUpdateAsync(x => x.SetProperty(s => s.CommitStatus, CommitStatus.Uncommited), cancellationToken);

            logger.Error(ex, "Failed to recover GraphRAG for adventure {AdventureId}", adventureId);
            throw;
        }
    }
}
