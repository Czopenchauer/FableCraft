using FableCraft.Application.Exceptions;
using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Application.NarrativeEngine.Orchestration;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

using Serilog;

using IKernelBuilder = FableCraft.Infrastructure.Llm.IKernelBuilder;

namespace FableCraft.Application.NarrativeEngine;

public class GameScene
{
    public string Text { get; set; } = null!;

    public List<string> Choices { get; set; } = null!;
}

public class SubmitActionRequest
{
    public Guid AdventureId { get; set; }

    public string ActionText { get; set; } = null!;
}

public interface IGameService
{
    Task<GameScene?> GetCurrentSceneAsync(Guid adventureId, CancellationToken cancellationToken);

    /// <summary>
    ///     Regenerates the last scene. Currently only supports regenerating the first scene.
    /// </summary>
    Task<GameScene> RegenerateAsync(Guid adventureId, CancellationToken cancellationToken);

    /// <summary>
    ///     Deletes the last scene from the adventure.
    /// </summary>
    Task DeleteLastSceneAsync(Guid adventureId, CancellationToken cancellationToken);

    /// <summary>
    ///     Generates the first scene for an adventure.
    /// </summary>
    Task<GameScene> GenerateFirstSceneAsync(Guid adventureId, CancellationToken cancellationToken);

    /// <summary>
    ///     Submits a player action and generates the next scene.
    /// </summary>
    Task<GameScene> SubmitActionAsync(Guid adventureId, string actionText, CancellationToken cancellationToken);
}

internal class GameService : IGameService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IKernelBuilder _kernelBuilder;
    private readonly ILogger _logger;
    private readonly SceneGenerationOrchestrator _sceneGenerationOrchestrator;

    public GameService(
        ApplicationDbContext dbContext,
        IKernelBuilder kernelBuilder,
        ILogger logger, SceneGenerationOrchestrator sceneGenerationOrchestrator)
    {
        _dbContext = dbContext;
        _kernelBuilder = kernelBuilder;
        _logger = logger;
        _sceneGenerationOrchestrator = sceneGenerationOrchestrator;
    }

    public async Task<GameScene?> GetCurrentSceneAsync(Guid adventureId, CancellationToken cancellationToken)
    {
        Scene? currentScene = await _dbContext.Scenes
            .OrderByDescending(x => x.SequenceNumber)
            .Include(scene => scene.CharacterActions)
            .FirstOrDefaultAsync(scene => scene.AdventureId == adventureId, cancellationToken);

        if (currentScene == null)
        {
            return null;
        }

        return new GameScene
        {
            Text = currentScene.NarrativeText,
            Choices = currentScene.CharacterActions.Select(x => x.ActionDescription).ToList()
        };
    }

    public async Task<GameScene> RegenerateAsync(Guid adventureId, CancellationToken cancellationToken)
    {
        Adventure? adventure = await _dbContext.Adventures
            .Include(a => a.Scenes)
            .FirstOrDefaultAsync(a => a.Id == adventureId, cancellationToken);

        if (adventure == null)
        {
            throw new AdventureNotFoundException(adventureId);
        }

        var sceneCount = adventure.Scenes.Count;

        if (sceneCount == 0)
        {
            _logger.Warning("Adventure {AdventureId} has no scenes to regenerate", adventureId);
            return new GameScene();
        }

        if (sceneCount == 1)
        {
            _logger.Information("Regenerating first scene for adventure {AdventureId}", adventureId);

            IExecutionStrategy strategy = _dbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using IDbContextTransaction transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    adventure.ProcessingStatus = ProcessingStatus.Pending;
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

            return await GenerateFirstSceneAsync(adventureId, cancellationToken);
        }

        _logger.Warning("Regenerating scenes beyond the first is not yet supported (AdventureId: {AdventureId})", adventureId);
        throw new NotImplementedException("Regenerating scenes beyond the first is not yet supported");
    }

    public async Task DeleteLastSceneAsync(Guid adventureId, CancellationToken cancellationToken)
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

        Scene lastScene = adventure.Scenes
            .OrderByDescending(s => s.SequenceNumber)
            .First();

        _logger.Information("Deleting scene {SceneId} (sequence {SequenceNumber}) from adventure {AdventureId}",
            lastScene.Id,
            lastScene.SequenceNumber,
            adventureId);

        _dbContext.Scenes.Remove(lastScene);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<GameScene> GenerateFirstSceneAsync(Guid adventureId, CancellationToken cancellationToken)
    {
        return await _sceneGenerationOrchestrator.GenerateInitialSceneAsync(adventureId, cancellationToken)
            .ContinueWith(task =>
            {
                GeneratedScene scene = task.Result;
                return new GameScene
                {
                    Text = scene.Scene,
                    Choices = scene.Choices.ToList()
                };
            }, cancellationToken);
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

        MainCharacterAction? selectedAction = currentScene.CharacterActions
            .FirstOrDefault(a => a.ActionDescription == actionText);

        if (selectedAction != null)
        {
            selectedAction.Selected = true;
        }

        try
        {
            GeneratedScene nextScene = await _sceneGenerationOrchestrator.GenerateSceneAsync(adventureId, actionText, cancellationToken);
            return new GameScene
            {
                Text = nextScene.Scene,
                Choices = nextScene.Choices.ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to generate next scene for adventure {AdventureId}", adventureId);
            throw;
        }
    }
}