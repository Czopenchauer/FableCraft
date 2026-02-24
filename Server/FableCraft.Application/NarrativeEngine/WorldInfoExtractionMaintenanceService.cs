using FableCraft.Application.NarrativeEngine.Agents;
using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Infrastructure;
using FableCraft.Infrastructure.Clients;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Serilog;

namespace FableCraft.Application.NarrativeEngine;

/// <summary>
///     Service for extracting world info (activities and world facts) from all scenes.
///     Used for maintenance tasks to backfill world knowledge.
/// </summary>
public sealed class WorldInfoExtractionMaintenanceService(
    ILogger logger,
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    IServiceProvider serviceProvider,
    IRagChunkService ragChunkService,
    IRagClientFactory ragClientFactory)
{
    /// <summary>
    ///     Extracts world info for all scenes in an adventure that don't have it yet.
    ///     Links results to each scene.
    /// </summary>
    public async Task<WorldInfoExtractionResult?> ExtractAllWorldInfoForAdventureAsync(
        Guid adventureId,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var adventureExists = await dbContext.Adventures
            .AnyAsync(a => a.Id == adventureId, cancellationToken);

        if (!adventureExists)
        {
            logger.Warning("Adventure {AdventureId} not found", adventureId);
            return null;
        }

        // Get scenes that don't already have world info extracted (idempotent)
        var scenesToProcess = await dbContext.Scenes
            .Include(s => s.CharacterSceneRewrites)
            .Where(s => s.AdventureId == adventureId)
            .Where(s => s.EnrichmentStatus == EnrichmentStatus.Enriched)
            .Where(s => !s.Lorebooks.Any(lb => lb.Category == nameof(LorebookCategory.Activity)))
            .OrderBy(s => s.SequenceNumber)
            .ToListAsync(cancellationToken);

        // Filter client-side: Metadata.Tracker can't be translated to SQL
        scenesToProcess = scenesToProcess.Where(s => s.Metadata.Tracker != null).ToList();

        if (scenesToProcess.Count == 0)
        {
            logger.Information("No scenes to process for adventure {AdventureId}", adventureId);
            return new WorldInfoExtractionResult
            {
                AdventureId = adventureId,
                TotalScenesProcessed = 0,
                SuccessCount = 0,
                SceneResults = []
            };
        }

        logger.Information(
            "Processing world info extraction for {Count} scenes in adventure {AdventureId}",
            scenesToProcess.Count,
            adventureId);

        const int batchSize = 50;
        var results = new List<SceneWorldInfoResult>();

        foreach (var batch in scenesToProcess.Chunk(batchSize))
        {
            var tasks = batch.Select(scene =>
                ExtractWorldInfoFromSceneAsync(adventureId, scene, cancellationToken));
            var batchResults = await Task.WhenAll(tasks);
            results.AddRange(batchResults);

            logger.Information("Completed batch of {Count} scenes", batch.Length);
        }

        var successCount = results.Count(r => r.Success);
        var lorebookCount = results.Sum(r => r.ActivityCount);
        logger.Information(
            "World info extraction completed for adventure {AdventureId}: {SuccessCount}/{TotalCount} scenes, {LorebookCount} entries committed",
            adventureId,
            successCount,
            results.Count,
            lorebookCount);

        return new WorldInfoExtractionResult
        {
            AdventureId = adventureId,
            TotalScenesProcessed = results.Count,
            SuccessCount = successCount,
            SceneResults = results
        };
    }

    private async Task<SceneWorldInfoResult> ExtractWorldInfoFromSceneAsync(
        Guid adventureId,
        Scene scene,
        CancellationToken cancellationToken)
    {
        try
        {
            var sceneTracker = scene.Metadata.Tracker?.Scene;
            if (sceneTracker is null)
            {
                logger.Warning("Scene {SceneId} has no tracker metadata", scene.Id);
                return new SceneWorldInfoResult
                {
                    SceneId = scene.Id,
                    SequenceNumber = scene.SequenceNumber,
                    Success = false,
                    ErrorMessage = "Scene has no tracker metadata"
                };
            }

            logger.Information(
                "Extracting world info for scene {SequenceNumber} (ID: {SceneId})",
                scene.SequenceNumber,
                scene.Id);

            await using var scope = serviceProvider.CreateAsyncScope();
            var generationContextBuilder = scope.ServiceProvider.GetRequiredService<IGenerationContextBuilder>();
            var generationContext = await generationContextBuilder.BuildRegenerationContextAsync(
                adventureId,
                scene,
                cancellationToken);

            var worldInfoExtractor = scope.ServiceProvider.GetRequiredService<WorldInfoExtractorAgent>();

            var mainSceneTask = worldInfoExtractor.Invoke(
                generationContext,
                scene.NarrativeText,
                sceneTracker,
                new AlreadyHandledContent(),
                cancellationToken);

            var rewriteTasks = scene.CharacterSceneRewrites.Select(async rewrite =>
            {
                try
                {
                    var output = await worldInfoExtractor.Invoke(
                        generationContext,
                        rewrite.Content,
                        rewrite.SceneTracker,
                        new AlreadyHandledContent(),
                        cancellationToken);
                    return (Success: true, Output: output);
                }
                catch (Exception ex)
                {
                    logger.Warning(ex,
                        "Failed to extract world info from CharacterSceneRewrite {RewriteId} for scene {SceneId}",
                        rewrite.Id,
                        scene.Id);
                    throw;
                }
            }).ToList();

            var rewriteResults = await Task.WhenAll(rewriteTasks);
            var mainSceneOutput = await mainSceneTask;

            var allExtractions = new List<WorldInfoExtractionOutput> { mainSceneOutput };
            var rewritesProcessed = rewriteResults.Count(r => r.Success);
            allExtractions.AddRange(rewriteResults.Where(r => r.Success).Select(r => r.Output!));

            var allActivities = allExtractions.SelectMany(e => e.Activity).ToList();

            var activityEntries = allActivities.Select(x => new LorebookEntry
            {
                AdventureId = adventureId,
                SceneId = scene.Id,
                Title = $"Activity at {x.Location}",
                Description = $"[{x.Time}] {string.Join(", ", x.Who)}",
                Category = nameof(LorebookCategory.Activity),
                Content = x.ToJsonString(),
                ContentType = ContentType.json
            }).ToList();

            if (activityEntries.Count > 0)
            {
                await SaveLorebooksAsync(activityEntries, cancellationToken);
            }

            logger.Information(
                "Extracted world info for scene {SequenceNumber}: {ActivityCount} activities, {RewritesProcessed} rewrites",
                scene.SequenceNumber,
                activityEntries.Count,
                rewritesProcessed);

            return new SceneWorldInfoResult
            {
                SceneId = scene.Id,
                SequenceNumber = scene.SequenceNumber,
                Success = true,
                ActivityCount = activityEntries.Count,
                CharacterRewritesProcessed = rewritesProcessed
            };
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to extract world info for scene {SceneId}", scene.Id);
            throw;
        }
    }

    private async Task SaveLorebooksAsync(List<LorebookEntry> lorebooks, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            dbContext.LorebookEntries.AddRange(lorebooks);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        });
    }

    /// <summary>
    ///     Commits Activity lorebooks to the World Knowledge Graph.
    ///     Activities are extracted after scene commit, so they need to be committed separately.
    /// </summary>
    public async Task<ActivityCommitResult?> CommitActivitiesToKnowledgeGraphAsync(
        Guid adventureId,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var adventureExists = await dbContext.Adventures
            .AnyAsync(a => a.Id == adventureId, cancellationToken);

        if (!adventureExists)
        {
            logger.Warning("Adventure {AdventureId} not found", adventureId);
            return null;
        }

        try
        {
            // Find all Activity lorebooks for this adventure
            var activityLorebooks = await dbContext.LorebookEntries
                .Where(lb => lb.AdventureId == adventureId
                             && lb.Category == nameof(LorebookCategory.Activity))
                .ToListAsync(cancellationToken);

            if (activityLorebooks.Count == 0)
            {
                logger.Information("No Activity lorebooks found for adventure {AdventureId}", adventureId);
                return new ActivityCommitResult
                {
                    AdventureId = adventureId,
                    TotalActivitiesFound = 0,
                    AlreadyCommitted = 0,
                    NewlyCommitted = 0,
                    Success = true
                };
            }

            // Check which activities are already committed by checking the Chunks table
            var worldDatasetName = RagClientExtensions.GetWorldDatasetName();
            var existingChunks = await dbContext.Chunks
                .Where(c => c.AdventureId == adventureId && c.DatasetName == worldDatasetName)
                .Select(c => c.EntityId)
                .ToListAsync(cancellationToken);

            var existingEntityIds = existingChunks.ToHashSet();

            var uncommittedActivities = activityLorebooks
                .Where(lb => !existingEntityIds.Contains(lb.Id))
                .ToList();

            var alreadyCommittedCount = activityLorebooks.Count - uncommittedActivities.Count;

            if (uncommittedActivities.Count == 0)
            {
                logger.Information(
                    "All {Count} Activity lorebooks already committed for adventure {AdventureId}",
                    activityLorebooks.Count,
                    adventureId);
                return new ActivityCommitResult
                {
                    AdventureId = adventureId,
                    TotalActivitiesFound = activityLorebooks.Count,
                    AlreadyCommitted = alreadyCommittedCount,
                    NewlyCommitted = 0,
                    Success = true
                };
            }

            logger.Information(
                "Committing {Count} Activity lorebooks to KG for adventure {AdventureId} ({AlreadyCommitted} already committed)",
                uncommittedActivities.Count,
                adventureId,
                alreadyCommittedCount);

            // Create chunk requests following the pattern from SceneGeneratedEvent
            var creationRequests = uncommittedActivities.Select(lorebook =>
                new ChunkCreationRequest(
                    lorebook.Id,
                    $"""
                     {lorebook.Title}
                     {lorebook.Content}
                     {lorebook.Category}
                     """,
                    lorebook.ContentType,
                    [worldDatasetName])).ToList();

            // Execute in transaction
            var strategy = dbContext.Database.CreateExecutionStrategy();
            var ragBuilder = await ragClientFactory.CreateBuildClientForAdventure(adventureId, cancellationToken);

            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

                var chunks = await ragChunkService.CreateChunk(creationRequests, adventureId, cancellationToken);
                await ragChunkService.CommitChunksToRagAsync(ragBuilder, chunks, cancellationToken);
                await ragChunkService.CognifyDatasetsAsync(ragBuilder, [worldDatasetName], cancellationToken: cancellationToken);

                var existingChunksInDb = await dbContext.Chunks
                    .Where(c => c.AdventureId == adventureId)
                    .ToListAsync(cancellationToken);
                var newChunks = chunks.Except(existingChunksInDb, new ChunkComparer()).ToList();
                dbContext.AddRange(newChunks);
                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(CancellationToken.None);
            });

            logger.Information(
                "Successfully committed {Count} Activity lorebooks to KG for adventure {AdventureId}",
                uncommittedActivities.Count,
                adventureId);

            return new ActivityCommitResult
            {
                AdventureId = adventureId,
                TotalActivitiesFound = activityLorebooks.Count,
                AlreadyCommitted = alreadyCommittedCount,
                NewlyCommitted = uncommittedActivities.Count,
                Success = true
            };
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to commit Activity lorebooks to KG for adventure {AdventureId}", adventureId);
            return new ActivityCommitResult
            {
                AdventureId = adventureId,
                TotalActivitiesFound = 0,
                AlreadyCommitted = 0,
                NewlyCommitted = 0,
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    private class ChunkComparer : IEqualityComparer<Chunk>
    {
        public bool Equals(Chunk? x, Chunk? y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (x is null)
            {
                return false;
            }

            if (y is null)
            {
                return false;
            }

            if (x.GetType() != y.GetType())
            {
                return false;
            }

            return Nullable.Equals(x.AdventureId, y.AdventureId)
                   && x.Name == y.Name
                   && x.ContentHash == y.ContentHash
                   && x.DatasetName == y.DatasetName
                   && x.KnowledgeGraphNodeId == y.KnowledgeGraphNodeId;
        }

        public int GetHashCode(Chunk obj)
        {
            return HashCode.Combine(obj.AdventureId, obj.Name, obj.ContentHash, obj.DatasetName, obj.KnowledgeGraphNodeId);
        }
    }
}