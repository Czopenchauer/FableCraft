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

public class SceneEnrichmentResult
{
    public required Guid SceneId { get; init; }

    public required Tracker Tracker { get; init; }

    public required List<CharacterInfo> NewCharacters { get; init; }

    public required List<LocationInfo> NewLocations { get; init; }

    public required List<LoreInfo> NewLore { get; init; }
}

public class CharacterInfo
{
    public Guid CharacterId { get; set; }

    public string Name { get; set; } = null!;

    public string Description { get; set; } = null!;
}

public class LocationInfo
{
    public string Name { get; set; } = null!;

    public string Description { get; set; } = null!;
}

public class LoreInfo
{
    public string Title { get; set; } = null!;

    public string Summary { get; set; } = null!;
}

public interface IGameService
{
    Task<GameScene[]> GetScenesAsync(Guid adventureId, int take, int? skip, CancellationToken cancellationToken);

    Task<GameScene> RegenerateAsync(Guid adventureId, Guid sceneId, CancellationToken cancellationToken);

    Task DeleteSceneAsync(Guid adventureId, Guid sceneId, CancellationToken cancellationToken);

    Task<GameScene> SubmitActionAsync(Guid adventureId, string actionText, CancellationToken cancellationToken);

    Task<SceneEnrichmentResult> EnrichSceneAsync(Guid adventureId, Guid sceneId, CancellationToken cancellationToken);
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

            SceneGenerationOutput scene = await _sceneGenerationOrchestrator.GenerateInitialSceneAsync(adventureId, cancellationToken);
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

        Scene lastScene = scenes[0];
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

            SceneGenerationOutput nextScene = await _sceneGenerationOrchestrator.GenerateSceneAsync(adventureId,
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
            SceneGenerationOutputWithoutEnrichment nextScene = await _sceneGenerationOrchestrator.GenerateSceneWithoutEnrichmentAsync(adventureId, actionText, cancellationToken);
            return new GameScene
            {
                Text = nextScene.GeneratedScene.Scene,
                Choices = nextScene.GeneratedScene.Choices.ToList(),
                Tracker = new Tracker
                {
                    Story = new StoryTracker
                    {
                        Location = "",
                        Weather = "",
                        Time = DateTime.UtcNow
                    },
                    CharactersPresent = Array.Empty<string>()
                },
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

    public async Task<SceneEnrichmentResult> EnrichSceneAsync(
        Guid adventureId,
        Guid sceneId,
        CancellationToken cancellationToken)
    {
        Scene? scene = await _dbContext.Scenes
            .Where(s => s.Id == sceneId && s.AdventureId == adventureId)
            .FirstOrDefaultAsync(cancellationToken);

        if (scene == null)
        {
            throw new InvalidOperationException($"Scene {sceneId} not found for adventure {adventureId}");
        }

        if (scene.EnrichmentStatus == EnrichmentStatus.Enriched)
        {
            _logger.Warning("Scene {SceneId} is already enriched", sceneId);
            return new SceneEnrichmentResult
            {
                SceneId = sceneId,
                Tracker = scene.Metadata.Tracker,
                NewCharacters = scene.CharacterStates.Select(c => new CharacterInfo
                {
                    CharacterId = c.CharacterId,
                    Name = c.CharacterStats.CharacterIdentity.FullName ?? "",
                    Description = c.Description
                }).ToList(),
                NewLocations = new List<LocationInfo>(),
                NewLore = new List<LoreInfo>()
            };
        }

        try
        {
            SceneEnrichmentOutput output = await _sceneGenerationOrchestrator
                .EnrichSceneAsync(adventureId, sceneId, cancellationToken);

            return new SceneEnrichmentResult
            {
                SceneId = output.SceneId,
                Tracker = output.Tracker,
                NewCharacters = output.NewCharacters.Select(c => new CharacterInfo
                {
                    CharacterId = c.CharacterId,
                    Name = c.Name,
                    Description = c.Description
                }).ToList(),
                NewLocations = output.NewLocations.Select(l => new LocationInfo
                {
                    Name = l.Name,
                    Description = l.Description
                }).ToList(),
                NewLore = output.NewLore.Select(l => new LoreInfo
                {
                    Title = l.Title,
                    Summary = l.Summary
                }).ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to enrich scene {SceneId} for adventure {AdventureId}", sceneId, adventureId);
            throw;
        }
    }
}