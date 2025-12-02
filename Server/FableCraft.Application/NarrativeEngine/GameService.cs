using FableCraft.Application.Exceptions;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

using Serilog;

namespace FableCraft.Application.NarrativeEngine;

public class GameScene
{
    public required Guid SceneId { get; init; }

    public required string Text { get; init; }

    public required List<string> Choices { get; init; }

    public required string? SelectedChoice { get; init; }

    public required Tracker Tracker { get; init; }

    public required NarrativeDirectorOutput NarrativeDirectorOutput { get; init; }

    public required bool CanRegenerate { get; init; }
}

public class SubmitActionRequest
{
    public Guid AdventureId { get; init; }

    public string ActionText { get; init; } = null!;
}

public interface IGameService
{
    Task<GameScene[]> GetScenesAsync(Guid adventureId, int take, int? skip, CancellationToken cancellationToken);

    Task<GameScene> RegenerateAsync(Guid adventureId, Guid sceneId, CancellationToken cancellationToken);

    Task DeleteSceneAsync(Guid adventureId, Guid sceneId, CancellationToken cancellationToken);

    Task<GameScene> SubmitActionAsync(Guid adventureId, string actionText, CancellationToken cancellationToken);
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

    public async Task<GameScene[]> GetScenesAsync(Guid adventureId, int take, int? skip, CancellationToken cancellationToken)
    {
        List<Scene> scenes;
        if (skip.HasValue)
        {
            scenes = await _dbContext.Scenes
                .Where(x => x.AdventureId == adventureId)
                .OrderByDescending(x => x.SequenceNumber)
                .Skip(skip.Value)
                .Take(take)
                .Include(scene => scene.CharacterActions)
                .ToListAsync(cancellationToken);
        }
        else
        {
            scenes = await _dbContext.Scenes
                .Where(x => x.AdventureId == adventureId)
                .OrderByDescending(x => x.SequenceNumber)
                .Include(scene => scene.CharacterActions)
                .ToListAsync(cancellationToken);
        }

        if (scenes.Count == 0)
        {
            return Array.Empty<GameScene>();
        }

        return scenes.Select(x => new GameScene
        {
            Text = x.NarrativeText,
            Choices = x.CharacterActions.Select(y => y.ActionDescription)
                .ToList(),
            Tracker = x.Metadata.Tracker,
            NarrativeDirectorOutput = x.Metadata.NarrativeMetadata,
            CanRegenerate = x.CommitStatus == CommitStatus.Uncommited,
            SceneId = x.Id,
            SelectedChoice = x.CharacterActions.FirstOrDefault(y => y.Selected)?.ActionDescription ?? string.Empty
        }).ToArray();
    }

    public async Task<GameScene> RegenerateAsync(Guid adventureId, Guid sceneId, CancellationToken cancellationToken)
    {
        var scenes = await _dbContext.Scenes
            .Where(x => x.Id == sceneId)
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
            var adventure = await _dbContext.Adventures
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

            var scene = await _sceneGenerationOrchestrator.GenerateInitialSceneAsync(adventureId, cancellationToken);
            return new GameScene
            {
                Text = scene.GeneratedScene.Scene,
                Choices = scene.GeneratedScene.Choices.ToList(),
                Tracker = scene.Tracker,
                NarrativeDirectorOutput = scene.NarrativeDirectorOutput,
                CanRegenerate = true,
                SceneId = scene.SceneId,
                SelectedChoice = null
            };
        }

        var lastScene = scenes[0];
        if (lastScene.CommitStatus != CommitStatus.Uncommited)
        {
            throw new InvalidOperationException("Can only regenerate the last uncommitted scene");
        }

        IExecutionStrategy executionStrategy = _dbContext.Database.CreateExecutionStrategy();
        return await executionStrategy.ExecuteAsync(async () =>
        {
            await using IDbContextTransaction transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            _dbContext.Scenes.Remove(lastScene);
            await _dbContext.SaveChangesAsync(cancellationToken);

            var nextScene = await _sceneGenerationOrchestrator.GenerateSceneAsync(adventureId,
                lastScene.CharacterActions.First(x => x.Selected).ActionDescription,
                cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new GameScene
            {
                Text = nextScene.GeneratedScene.Scene,
                Choices = nextScene.GeneratedScene.Choices.ToList(),
                Tracker = nextScene.Tracker,
                NarrativeDirectorOutput = nextScene.NarrativeDirectorOutput,
                CanRegenerate = true,
                SceneId = nextScene.SceneId,
                SelectedChoice = null
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
        Adventure? adventure = await _dbContext.Adventures
            .Include(a => a.Scenes)
            .ThenInclude(s => s.CharacterActions)
            .FirstOrDefaultAsync(a => a.Id == adventureId, cancellationToken);

        if (adventure is null)
        {
            _logger.Debug("Adventure with ID {AdventureId} not found", adventureId);
            throw new AdventureNotFoundException(adventureId);
        }

        Scene? currentScene = adventure.Scenes
            .OrderByDescending(s => s.SequenceNumber)
            .FirstOrDefault();

        if (currentScene == null)
        {
            _logger.Warning("No scenes found for adventure {AdventureId}", adventureId);
            throw new InvalidOperationException("Cannot submit action: adventure has no scenes");
        }

        try
        {
            var nextScene = await _sceneGenerationOrchestrator.GenerateSceneAsync(adventureId, actionText, cancellationToken);
            return new GameScene
            {
                Text = nextScene.GeneratedScene.Scene,
                Choices = nextScene.GeneratedScene.Choices.ToList(),
                Tracker = nextScene.Tracker,
                NarrativeDirectorOutput = nextScene.NarrativeDirectorOutput,
                CanRegenerate = true,
                SceneId = nextScene.SceneId,
                SelectedChoice = null
            };
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to generate next scene for adventure {AdventureId}", adventureId);
            throw;
        }
    }
}