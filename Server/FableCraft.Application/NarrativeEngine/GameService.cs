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

    public required EnrichmentStatus EnrichmentStatus { get; init; }
}

public class SubmitActionRequest
{
    public Guid AdventureId { get; init; }

    public string ActionText { get; init; } = null!;
}

public interface IGameService
{
    Task<GameScene> GetCurrentSceneAsync(Guid adventureId, CancellationToken cancellationToken);

    Task<GameScene> GetSceneAsync(Guid adventureId, Guid sceneId, CancellationToken cancellationToken);

    Task<GameScene> RegenerateAsync(Guid adventureId, Guid sceneId, CancellationToken cancellationToken);

    Task DeleteSceneAsync(Guid adventureId, Guid sceneId, CancellationToken cancellationToken);

    Task<GameScene> SubmitActionAsync(Guid adventureId, string actionText, CancellationToken cancellationToken);

    Task<SceneEnrichmentOutput> EnrichSceneAsync(Guid adventureId, Guid sceneId, CancellationToken cancellationToken);
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

        MainCharacter mainCharacter = await _dbContext.MainCharacters.Where(x => x.AdventureId == adventureId).SingleAsync(cancellationToken);
        Scene lastScene = scene.OrderByDescending(x => x.SequenceNumber).First();
        var sceneGenerationOutput = SceneGenerationOutput.CreateFromScene(lastScene, mainCharacter);
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
            GenerationOutput = sceneGenerationOutput
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
        MainCharacter mainCharacter = await _dbContext.MainCharacters.Where(x => x.AdventureId == adventureId).SingleAsync(cancellationToken);
        var sceneGenerationOutput = SceneGenerationOutput.CreateFromScene(scene, mainCharacter);
        return new GameScene
        {
            CanRegenerate = scene.CommitStatus == CommitStatus.Uncommited,
            SceneId = scene.Id,
            EnrichmentStatus = scene.EnrichmentStatus,
            PreviousScene = neighborScenes.FirstOrDefault(x => x.SequenceNumber == scene.SequenceNumber - 1)
                ?.Id,
            NextScene = neighborScenes.FirstOrDefault(x => x.SequenceNumber == scene.SequenceNumber + 1)
                ?.Id,
            GenerationOutput = sceneGenerationOutput
        };
    }

    public async Task<GameScene> RegenerateAsync(Guid adventureId, Guid sceneId, CancellationToken cancellationToken)
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
                    await _dbContext.SaveChangesAsync(cancellationToken);

                    await transaction.CommitAsync(cancellationToken);
                }
                catch
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
            });

            SceneGenerationOutput scene = await _sceneGenerationOrchestrator.GenerateFullSceneAsync(adventureId, string.Empty, cancellationToken);
            return new GameScene
            {
                GenerationOutput = scene,
                CanRegenerate = true,
                SceneId = scene.SceneId,
                EnrichmentStatus = EnrichmentStatus.Enriched,
                PreviousScene = null,
                NextScene = null
            };
        }

        Scene lastScene = scenes.Last();
        if (lastScene.CommitStatus != CommitStatus.Uncommited)
        {
            throw new InvalidOperationException("Can only regenerate the last uncommitted scene");
        }

        IExecutionStrategy executionStrategy = _dbContext.Database.CreateExecutionStrategy();
        return await executionStrategy.ExecuteAsync(async () =>
        {
            MainCharacter mainCharacter = await _dbContext.MainCharacters
                .Where(x => x.AdventureId == adventureId)
                .SingleAsync(cancellationToken);
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
                GenerationOutput = SceneGenerationOutput.CreateFromScene(nextScene, mainCharacter),
                EnrichmentStatus = EnrichmentStatus.Enriched,
                PreviousScene = null,
                NextScene = null
            };
        });
    }

    // TODO: clear knowledge graph entries related to the deleted scene
    public async Task DeleteSceneAsync(Guid adventureId, Guid sceneId, CancellationToken cancellationToken)
    {
        Adventure? adventure = await _dbContext.Adventures
            .Include(a => a.Scenes)
            .ThenInclude(s => s.CharacterActions)
            .FirstOrDefaultAsync(a => a.Id == adventureId, cancellationToken);

        if (adventure == null)
        {
            throw new AdventureNotFoundException(adventureId);
        }

        if (adventure.Scenes.Count == 0)
        {
            _logger.Warning("Adventure {AdventureId} has no scenes to delete", adventureId);
            throw new InvalidOperationException("No scenes to delete");
        }

        Scene lastScene = adventure.Scenes.MaxBy(s => s.SequenceNumber)!;
        if (lastScene.CommitStatus != CommitStatus.Uncommited)
        {
            throw new InvalidOperationException("Can only delete the last uncommitted scene");
        }

        _logger.Information("Deleting scene {SceneId} (sequence {SequenceNumber}) from adventure {AdventureId}",
            lastScene.Id,
            lastScene.SequenceNumber,
            adventureId);

        _dbContext.Scenes.Remove(lastScene);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<GameScene> SubmitActionAsync(Guid adventureId, string actionText, CancellationToken cancellationToken)
    {
        var adventure = await _dbContext.Adventures
            .Include(x => x.MainCharacter)
            .Select(x =>
                new
                {
                    AdventureId = x.Id,
                    x.MainCharacter
                })
            .FirstOrDefaultAsync(a => a.AdventureId == adventureId, cancellationToken);

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

        try
        {
            Scene nextScene =
                await _sceneGenerationOrchestrator.GenerateSceneAsync(adventureId, actionText, cancellationToken);
            var generationOutput = SceneGenerationOutput.CreateFromScene(nextScene, adventure.MainCharacter);
            return new GameScene
            {
                GenerationOutput = generationOutput,
                CanRegenerate = true,
                SceneId = nextScene.Id,
                EnrichmentStatus = EnrichmentStatus.NotEnriched,
                PreviousScene = currentScene.Id,
                NextScene = null
            };
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to generate next scene for adventure {AdventureId}", adventureId);
            throw;
        }
    }

    public async Task<SceneEnrichmentOutput> EnrichSceneAsync(Guid adventureId,
        Guid sceneId,
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
            throw new InvalidOperationException($"Scene {sceneId} not found for adventure {adventureId}");
        }

        if (scene.EnrichmentStatus == EnrichmentStatus.Enriched)
        {
            _logger.Warning("Scene {SceneId} is already enriched", sceneId);
            scene.Metadata.Tracker!.Characters = scene.CharacterStates.Select(x => x.Tracker).ToArray();
            return new SceneEnrichmentOutput
            {
                SceneId = sceneId,
                Tracker = new TrackerDto
                {
                    Story = scene.Metadata.Tracker!.Story,
                    MainCharacter = new MainCharacterTrackerDto
                    {
                        Tracker = scene.Metadata.Tracker!.MainCharacter!,
                        Development = scene.Metadata.Tracker!.MainCharacterDevelopment!,
                        Description = scene.Metadata.MainCharacterDescription!
                    },
                    CharactersOnScene = scene.Metadata.Tracker!.CharactersPresent,
                    Characters = scene.CharacterStates.Select(x => new CharacterStateDto
                        {
                            CharacterId = x.CharacterId,
                            Name = x.CharacterStats.CharacterIdentity.FullName!,
                            Description = x.Description,
                            State = x.CharacterStats,
                            Tracker = x.Tracker,
                            Development = x.DevelopmentTracker!
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
                .EnrichSceneAsync(adventureId, sceneId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to enrich scene {SceneId} for adventure {AdventureId}", sceneId, adventureId);
            throw;
        }
    }
}