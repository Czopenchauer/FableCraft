using FableCraft.Application.Exceptions;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

using Serilog;

namespace FableCraft.Application.NarrativeEngine;

public class GameScene
{
    public required Guid? PreviousScene { get; init; }

    public required Guid? NextScene { get; init; }

    public required Guid SceneId { get; init; }

    public required SceneGenerationOutput? GenerationOutput { get; init; }

    public required bool CanRegenerate { get; init; }

    public required bool CanDelete { get; init; }

    public required EnrichmentStatus EnrichmentStatus { get; init; }
}

public class SubmitActionRequest
{
    public Guid AdventureId { get; init; }

    public string ActionText { get; init; } = null!;
}

public class RegenerateEnrichmentRequest
{
    public List<string> AgentsToRegenerate { get; init; } = new();
}

public interface IGameService
{
    Task<GameScene> GetCurrentSceneAsync(Guid adventureId, CancellationToken cancellationToken);

    Task<GameScene> GetSceneAsync(Guid adventureId, Guid sceneId, CancellationToken cancellationToken);

    Task<GameScene> RegenerateAsync(Guid adventureId, CancellationToken cancellationToken);

    Task DeleteSceneAsync(Guid adventureId, CancellationToken cancellationToken);

    Task<GameScene> SubmitActionAsync(Guid adventureId, string actionText, CancellationToken cancellationToken);

    Task<SceneEnrichmentOutput> EnrichSceneAsync(Guid adventureId, CancellationToken cancellationToken);

    Task<SceneEnrichmentOutput> RegenerateEnrichmentAsync(
        Guid adventureId,
        Guid sceneId,
        List<string> agentsToRegenerate,
        CancellationToken cancellationToken);
}

internal class GameService : IGameService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger _logger;
    private readonly SceneGenerationOrchestrator _sceneGenerationOrchestrator;

    public GameService(
        ApplicationDbContext dbContext,
        ILogger logger,
        SceneGenerationOrchestrator sceneGenerationOrchestrator)
    {
        _dbContext = dbContext;
        _logger = logger;
        _sceneGenerationOrchestrator = sceneGenerationOrchestrator;
    }

    public async Task<GameScene> GetCurrentSceneAsync(Guid adventureId, CancellationToken cancellationToken)
    {
        var scene = await _dbContext.Scenes
            .Where(x => x.AdventureId == adventureId)
            .OrderByDescending(x => x.SequenceNumber)
            .Take(2)
            .Include(scene => scene.CharacterActions)
            .Include(x => x.CharacterStates)
            .Include(x => x.Lorebooks)
            .ToListAsync(cancellationToken);

        if (scene == null)
        {
            throw new AdventureNotFoundException(adventureId);
        }

        Scene lastScene = scene.OrderByDescending(x => x.SequenceNumber).First();
        var sceneGenerationOutput = SceneGenerationOutput.CreateFromScene(lastScene);
        return new GameScene
        {
            CanRegenerate = lastScene.CommitStatus == CommitStatus.Uncommited,
            SceneId = lastScene.Id,
            EnrichmentStatus = lastScene.EnrichmentStatus,
            PreviousScene = scene.Count > 1
                ? scene.OrderByDescending(x => x.SequenceNumber)
                    .Skip(1)
                    .First()
                    .Id
                : null,
            NextScene = null,
            GenerationOutput = sceneGenerationOutput,
            CanDelete = lastScene.CommitStatus == CommitStatus.Uncommited
        };
    }

    public async Task<GameScene> GetSceneAsync(Guid adventureId, Guid sceneId, CancellationToken cancellationToken)
    {
        Scene? scene = await _dbContext.Scenes
            .Where(x => x.Id == sceneId)
            .OrderByDescending(x => x.SequenceNumber)
            .Include(scene => scene.CharacterActions)
            .Include(x => x.CharacterStates)
            .Include(x => x.Lorebooks)
            .FirstOrDefaultAsync(cancellationToken);

        if (scene == null)
        {
            throw new SceneNotFoundException(sceneId);
        }

        var neighborScenes = await _dbContext.Scenes
            .Where(x => x.AdventureId == adventureId && (x.SequenceNumber == scene.SequenceNumber - 1 || x.SequenceNumber == scene.SequenceNumber + 1))
            .OrderByDescending(x => x.SequenceNumber)
            .Select(x => new { x.Id, x.SequenceNumber })
            .ToListAsync(cancellationToken);
        var sceneGenerationOutput = SceneGenerationOutput.CreateFromScene(scene);
        return new GameScene
        {
            CanRegenerate = scene.CommitStatus == CommitStatus.Uncommited,
            SceneId = scene.Id,
            EnrichmentStatus = scene.EnrichmentStatus,
            PreviousScene = neighborScenes.FirstOrDefault(x => x.SequenceNumber == scene.SequenceNumber - 1)
                ?.Id,
            NextScene = neighborScenes.FirstOrDefault(x => x.SequenceNumber == scene.SequenceNumber + 1)
                ?.Id,
            GenerationOutput = sceneGenerationOutput,
            CanDelete = scene.CommitStatus == CommitStatus.Uncommited
        };
    }

    public async Task<GameScene> RegenerateAsync(Guid adventureId, CancellationToken cancellationToken)
    {
        var scenes = await _dbContext.Scenes
            .Include(x => x.CharacterActions)
            .Where(x => x.AdventureId == adventureId)
            .OrderByDescending(x => x.SequenceNumber)
            .Take(2)
            .ToListAsync(cancellationToken);

        if (scenes.Count == 0)
        {
            throw new AdventureNotFoundException(adventureId);
        }

        if (scenes.Count == 1)
        {
            _logger.Information("Regenerating first scene for adventure {AdventureId}", adventureId);
            Adventure adventure = await _dbContext.Adventures
                .Include(a => a.Scenes)
                .SingleAsync(a => a.Id == adventureId, cancellationToken);
            IExecutionStrategy strategy = _dbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using IDbContextTransaction transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    adventure.SceneGenerationStatus = ProcessingStatus.Pending;
                    adventure.Scenes.Clear();
                    var scene = await _sceneGenerationOrchestrator.GenerateSceneAsync(adventureId, string.Empty, cancellationToken);
                    await _dbContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    return new GameScene
                    {
                        GenerationOutput = SceneGenerationOutput.CreateFromScene(scene),
                        CanRegenerate = true,
                        SceneId = scene.Id,
                        EnrichmentStatus = EnrichmentStatus.Enriched,
                        PreviousScene = null,
                        NextScene = null,
                        CanDelete = true
                    };
                }
                catch
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
            });
        }

        Scene lastScene = scenes.Last();
        if (lastScene.CommitStatus != CommitStatus.Uncommited)
        {
            throw new InvalidOperationException("Can only regenerate the last uncommitted scene");
        }

        IExecutionStrategy executionStrategy = _dbContext.Database.CreateExecutionStrategy();
        return await executionStrategy.ExecuteAsync(async () =>
        {
            await using IDbContextTransaction transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            Scene nextScene = await _sceneGenerationOrchestrator.GenerateSceneAsync(adventureId,
                lastScene.CharacterActions.First(x => x.Selected).ActionDescription,
                cancellationToken);

            _dbContext.Scenes.Remove(lastScene);
            await transaction.CommitAsync(cancellationToken);

            return new GameScene
            {
                CanRegenerate = true,
                SceneId = nextScene.Id,
                GenerationOutput = SceneGenerationOutput.CreateFromScene(nextScene),
                EnrichmentStatus = EnrichmentStatus.Enriched,
                PreviousScene = null,
                NextScene = null,
                CanDelete = true
            };
        });
    }

    // TODO: clear knowledge graph entries related to the deleted scene
    public async Task DeleteSceneAsync(Guid adventureId, CancellationToken cancellationToken)
    {
        var scenes = await _dbContext.Scenes
            .Include(s => s.CharacterActions)
            .OrderByDescending(x => x.SequenceNumber)
            .Where(s => s.AdventureId == adventureId)
            .Take(2)
            .ToArrayAsync(cancellationToken: cancellationToken);

        if (scenes.Length == 0)
        {
            _logger.Warning("Adventure {AdventureId} has no scenes to delete", adventureId);
            throw new InvalidOperationException("No scenes to delete");
        }

        Scene lastScene = scenes.MaxBy(s => s.SequenceNumber)!;
        if (lastScene.CommitStatus != CommitStatus.Uncommited)
        {
            throw new InvalidOperationException("Can only delete the last uncommitted scene");
        }

        _logger.Information("Deleting scene {SceneId} (sequence {SequenceNumber}) from adventure {AdventureId}",
            lastScene.Id,
            lastScene.SequenceNumber,
            adventureId);

        IExecutionStrategy executionStrategy = _dbContext.Database.CreateExecutionStrategy();
        await executionStrategy.ExecuteAsync(async () =>
        {
            await using IDbContextTransaction transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            Scene previousScene = scenes.MinBy(s => s.SequenceNumber)!;
            var lastSelectedAction = previousScene.CharacterActions.Single(x => x.Selected);

            _dbContext.CharacterActions.Remove(lastSelectedAction);
            _dbContext.Scenes.Remove(lastScene);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        });
    }

    public async Task<GameScene> SubmitActionAsync(Guid adventureId, string actionText, CancellationToken cancellationToken)
    {
        var adventure = await _dbContext.Adventures
            .Select(ad => new { ad.Id })
            .FirstOrDefaultAsync(a => a.Id == adventureId, cancellationToken);

        if (adventure is null)
        {
            _logger.Debug("Adventure with ID {AdventureId} not found", adventureId);
            throw new AdventureNotFoundException(adventureId);
        }

        Scene? currentScene = await _dbContext.Scenes
            .Where(s => s.AdventureId == adventureId)
            .OrderByDescending(s => s.SequenceNumber)
            .Include(x => x.CharacterStates)
            .Include(x => x.Lorebooks)
            .FirstOrDefaultAsync(cancellationToken);

        if (currentScene == null)
        {
            _logger.Warning("No scenes found for adventure {AdventureId}", adventureId);
            throw new InvalidOperationException("Cannot submit action: adventure has no scenes");
        }

        if (currentScene.EnrichmentStatus != EnrichmentStatus.Enriched)
        {
            throw new InvalidOperationException("Cannot submit action: adventure has no enrichment status");
        }

        try
        {
            Scene nextScene =
                await _sceneGenerationOrchestrator.GenerateSceneAsync(adventureId, actionText, cancellationToken);
            var generationOutput = SceneGenerationOutput.CreateFromScene(nextScene);
            return new GameScene
            {
                GenerationOutput = generationOutput,
                CanRegenerate = true,
                SceneId = nextScene.Id,
                EnrichmentStatus = EnrichmentStatus.NotEnriched,
                PreviousScene = currentScene.Id,
                NextScene = null,
                CanDelete = true
            };
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to generate next scene for adventure {AdventureId}", adventureId);
            throw;
        }
    }

    public async Task<SceneEnrichmentOutput> EnrichSceneAsync(Guid adventureId,
        CancellationToken cancellationToken)
    {
        Scene? scene = await _dbContext.Scenes
            .Where(s => s.AdventureId == adventureId)
            .OrderByDescending(s => s.SequenceNumber)
            .Include(x => x.CharacterStates)
            .Include(x => x.CharacterActions)
            .Include(x => x.Lorebooks)
            .FirstOrDefaultAsync(cancellationToken);

        if (scene == null)
        {
            throw new InvalidOperationException($"Scene not found for adventure {adventureId}");
        }

        if (scene.EnrichmentStatus == EnrichmentStatus.Enriched)
        {
            _logger.Warning("Scene {SceneId} is already enriched", scene.Id);
            return new SceneEnrichmentOutput
            {
                SceneId = scene.Id,
                Tracker = new TrackerDto
                {
                    Scene = scene.Metadata.Tracker!.Scene!,
                    MainCharacter = new MainCharacterTrackerDto
                    {
                        Tracker = scene.Metadata.Tracker!.MainCharacter!.MainCharacter!,
                        Description = scene.Metadata.Tracker!.MainCharacter.MainCharacterDescription!
                    },
                    Characters = scene.CharacterStates.Select(x => new CharacterStateDto
                        {
                            CharacterId = x.CharacterId,
                            Name = x.CharacterStats.CharacterIdentity.Name!,
                            Description = x.Description,
                            State = x.CharacterStats,
                            Tracker = x.Tracker
                        })
                        .ToList()
                },
                NewLore = scene.Lorebooks.Select(x => new LoreDto
                {
                    Title = x.Category,
                    Summary = x.Description
                }).ToList()
            };
        }

        try
        {
            return await _sceneGenerationOrchestrator
                .EnrichSceneAsync(adventureId, scene.Id, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to enrich scene {SceneId} for adventure {AdventureId}", scene.Id, adventureId);
            throw;
        }
    }

    public async Task<SceneEnrichmentOutput> RegenerateEnrichmentAsync(
        Guid adventureId,
        Guid sceneId,
        List<string> agentsToRegenerate,
        CancellationToken cancellationToken)
    {
        Scene? scene = await _dbContext.Scenes
            .Where(s => s.Id == sceneId && s.AdventureId == adventureId)
            .Include(x => x.CharacterStates)
            .Include(x => x.CharacterActions)
            .Include(x => x.Lorebooks)
            .FirstOrDefaultAsync(cancellationToken);

        if (scene == null)
        {
            throw new AdventureNotFoundException(adventureId);
        }

        try
        {
            return await _sceneGenerationOrchestrator
                .RegenerateEnrichmentAsync(adventureId, sceneId, agentsToRegenerate, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to regenerate enrichment for scene {SceneId} in adventure {AdventureId}", sceneId, adventureId);
            throw;
        }
    }
}