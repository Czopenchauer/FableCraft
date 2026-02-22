using FableCraft.Application.NarrativeEngine.Agents;
using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Serilog;

namespace FableCraft.Application.NarrativeEngine;

/// <summary>
///     Service for running character reflection and tracking on scenes where it was missed.
///     Used for maintenance tasks to backfill character state data.
/// </summary>
public sealed class CharacterReflectionMaintenanceService(
    ILogger logger,
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    IServiceProvider serviceProvider)
{
    /// <summary>
    ///     Processes character reflection for the last scene where the character was present.
    /// </summary>
    /// <param name="adventureId">The adventure ID</param>
    /// <param name="characterId">The character ID to process</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the maintenance operation, or null if character not found</returns>
    public async Task<CharacterReflectionMaintenanceResult?> ProcessCharacterReflectionAsync(
        Guid adventureId,
        Guid characterId,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var character = await dbContext.Characters
            .Where(c => c.Id == characterId && c.AdventureId == adventureId)
            .FirstOrDefaultAsync(cancellationToken);

        if (character is null)
        {
            logger.Warning("Character {CharacterId} not found in adventure {AdventureId}", characterId, adventureId);
            return null;
        }

        var lastScene = await FindLastSceneForCharacterAsync(dbContext, adventureId, character.Name, cancellationToken);
        if (lastScene is null)
        {
            logger.Warning("No scenes found where character {CharacterName} was present", character.Name);
            return new CharacterReflectionMaintenanceResult
            {
                CharacterId = characterId,
                CharacterName = character.Name,
                SceneId = Guid.Empty,
                SceneSequenceNumber = -1,
                AlreadyProcessed = false,
                Message = "No scenes found where character was present"
            };
        }

        var existingRewrite = await dbContext.CharacterSceneRewrites
            .AnyAsync(r => r.CharacterId == characterId && r.SceneId == lastScene.Id, cancellationToken);

        if (existingRewrite)
        {
            logger.Information(
                "Character {CharacterName} already has a scene rewrite for scene {SceneId} (sequence {SequenceNumber})",
                character.Name, lastScene.Id, lastScene.SequenceNumber);

            return new CharacterReflectionMaintenanceResult
            {
                CharacterId = characterId,
                CharacterName = character.Name,
                SceneId = lastScene.Id,
                SceneSequenceNumber = lastScene.SequenceNumber,
                AlreadyProcessed = true,
                Message = "Character already has a scene rewrite for this scene"
            };
        }

        var characterContextBeforeScene = await BuildCharacterContextBeforeSceneAsync(
            dbContext, characterId, character, lastScene.Id, cancellationToken);

        var generationContextBuilder = serviceProvider.GetRequiredService<IGenerationContextBuilder>();
        var generationContext = await generationContextBuilder.BuildRegenerationContextAsync(
            adventureId, lastScene, cancellationToken);

        var sceneTracker = lastScene.Metadata.Tracker?.Scene;
        if (sceneTracker is null)
        {
            logger.Warning("Scene {SceneId} has no tracker metadata", lastScene.Id);
            return new CharacterReflectionMaintenanceResult
            {
                CharacterId = characterId,
                CharacterName = character.Name,
                SceneId = lastScene.Id,
                SceneSequenceNumber = lastScene.SequenceNumber,
                AlreadyProcessed = false,
                Message = "Scene has no tracker metadata"
            };
        }

        logger.Information(
            "Running character reflection for {CharacterName} on scene {SceneSequenceNumber}",
            character.Name, lastScene.SequenceNumber);

        var characterReflectionAgent = serviceProvider.GetRequiredService<CharacterReflectionAgent>();
        var characterTrackerAgent = serviceProvider.GetRequiredService<CharacterTrackerAgent>();

        var reflectionResult = await characterReflectionAgent.Invoke(
            generationContext, characterContextBeforeScene, sceneTracker, cancellationToken);

        var (tracker, isDead) = await characterTrackerAgent.Invoke(
            generationContext, characterContextBeforeScene, reflectionResult, sceneTracker, cancellationToken);

        reflectionResult.CharacterTracker = tracker;
        reflectionResult.IsDead = isDead;

        await SaveReflectionResultsAsync(dbContext, character, lastScene, reflectionResult, cancellationToken);

        logger.Information(
            "Successfully processed character reflection for {CharacterName} on scene {SceneSequenceNumber}",
            character.Name, lastScene.SequenceNumber);

        return new CharacterReflectionMaintenanceResult
        {
            CharacterId = characterId,
            CharacterName = character.Name,
            SceneId = lastScene.Id,
            SceneSequenceNumber = lastScene.SequenceNumber,
            AlreadyProcessed = false,
            Message = "Successfully processed character reflection"
        };
    }

    private async Task<Scene?> FindLastSceneForCharacterAsync(
        ApplicationDbContext dbContext,
        Guid adventureId,
        string characterName,
        CancellationToken cancellationToken)
    {
        var scenes = await dbContext.Scenes
            .Where(s => s.AdventureId == adventureId)
            .OrderByDescending(s => s.SequenceNumber)
            .ToListAsync(cancellationToken);

        return scenes.FirstOrDefault(s =>
            s.Metadata?.Tracker?.Scene?.CharactersPresent?
                .Contains(characterName, StringComparer.OrdinalIgnoreCase) == true);
    }

    private async Task<CharacterContext> BuildCharacterContextBeforeSceneAsync(
        ApplicationDbContext dbContext,
        Guid characterId,
        Character character,
        Guid targetSceneId,
        CancellationToken cancellationToken)
    {
        var latestState = await dbContext.CharacterStates
            .Where(cs => cs.CharacterId == characterId && cs.SceneId != targetSceneId)
            .OrderByDescending(cs => cs.SequenceNumber)
            .FirstOrDefaultAsync(cancellationToken);

        var memories = await dbContext.CharacterMemories
            .Where(m => m.CharacterId == characterId && m.SceneId != targetSceneId)
            .ToListAsync(cancellationToken);

        var relationships = await dbContext.CharacterRelationships
            .Where(r => r.CharacterId == characterId && r.SceneId != targetSceneId)
            .ToListAsync(cancellationToken);

        var latestRelationships = relationships
            .GroupBy(r => r.TargetCharacterName)
            .Select(g => g.OrderByDescending(r => r.SequenceNumber).First())
            .ToList();

        var sceneRewrites = await dbContext.CharacterSceneRewrites
            .Where(sr => sr.CharacterId == characterId && sr.SceneId != targetSceneId)
            .OrderByDescending(sr => sr.SequenceNumber)
            .Take(20)
            .ToListAsync(cancellationToken);

        return new CharacterContext
        {
            CharacterId = characterId,
            Name = character.Name,
            Description = character.Description,
            Importance = character.Importance,
            CharacterState = latestState?.CharacterStats ?? new CharacterStats
            {
                Name = character.Name,
                Motivations = null,
                Routine = null
            },
            CharacterTracker = latestState?.Tracker,
            CharacterMemories = memories.Select(m => new MemoryContext
            {
                MemoryContent = m.Summary,
                Salience = m.Salience,
                Data = m.Data,
                SceneTracker = m.SceneTracker
            }).ToList(),
            Relationships = latestRelationships.Select(r => new CharacterRelationshipContext
            {
                TargetCharacterName = r.TargetCharacterName,
                Data = r.Data,
                UpdateTime = r.UpdateTime,
                SequenceNumber = r.SequenceNumber,
                Dynamic = r.Dynamic!
            }).ToList(),
            SceneRewrites = sceneRewrites.Select(sr => new CharacterSceneContext
            {
                Content = sr.Content,
                SceneTracker = sr.SceneTracker,
                SequenceNumber = sr.SequenceNumber
            }).ToList(),
            SimulationMetadata = latestState?.SimulationMetadata,
            IsDead = latestState?.IsDead ?? false
        };
    }

    private async Task SaveReflectionResultsAsync(
        ApplicationDbContext dbContext,
        Character character,
        Scene scene,
        CharacterContext reflectionResult,
        CancellationToken cancellationToken)
    {
        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            var currentCharacter = await dbContext.Characters
                .FirstAsync(c => c.Id == character.Id, cancellationToken);

            var newState = new CharacterState
            {
                CharacterId = character.Id,
                CharacterStats = reflectionResult.CharacterState,
                Tracker = reflectionResult.CharacterTracker!,
                SequenceNumber = currentCharacter.Version + 1,
                Scene = scene,
                SceneId = scene.Id,
                IsDead = reflectionResult.IsDead,
                SimulationMetadata = reflectionResult.SimulationMetadata
            };
            dbContext.CharacterStates.Add(newState);

            var newMemories = reflectionResult.CharacterMemories.Select(m => new CharacterMemory
            {
                CharacterId = character.Id,
                SceneId = scene.Id,
                Scene = scene,
                SceneTracker = m.SceneTracker,
                Summary = m.MemoryContent,
                Salience = m.Salience,
                Data = m.Data
            });
            dbContext.CharacterMemories.AddRange(newMemories);

            var newRelationships = reflectionResult.Relationships.Select(r => new CharacterRelationship
            {
                CharacterId = character.Id,
                TargetCharacterName = r.TargetCharacterName,
                SceneId = scene.Id,
                Scene = scene,
                Data = r.Data,
                SequenceNumber = r.SequenceNumber,
                UpdateTime = r.UpdateTime,
                Dynamic = r.Dynamic?.ToString()
            });
            dbContext.CharacterRelationships.AddRange(newRelationships);

            var newRewrites = reflectionResult.SceneRewrites.Select(sr => new CharacterSceneRewrite
            {
                CharacterId = character.Id,
                SceneId = scene.Id,
                Scene = scene,
                Content = sr.Content,
                SequenceNumber = sr.SequenceNumber,
                SceneTracker = sr.SceneTracker!
            });
            dbContext.CharacterSceneRewrites.AddRange(newRewrites);

            currentCharacter.Version += 1;
            dbContext.Characters.Update(currentCharacter);

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        });
    }

    /// <summary>
    ///     Runs context gathering for all characters in an adventure.
    ///     This is a maintenance operation to backfill GatheredContext.
    /// </summary>
    public async Task<CharacterContextGatheringMaintenanceResult> GatherContextForAllCharactersAsync(
    Guid adventureId,
    CancellationToken cancellationToken)
{
    await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

    var adventure = await dbContext.Adventures
        .Include(a => a.MainCharacter)
        .FirstOrDefaultAsync(a => a.Id == adventureId, cancellationToken);

    if (adventure is null)
    {
        return new CharacterContextGatheringMaintenanceResult
        {
            Success = false,
            Message = "Adventure not found",
            ProcessedCharacters = []
        };
    }

    var characters = await dbContext.Characters
        .Where(c => c.AdventureId == adventureId)
        .Include(c => c.CharacterStates.OrderByDescending(cs => cs.SequenceNumber).Take(1))
        .Include(c => c.CharacterSceneRewrites.OrderByDescending(sr => sr.SequenceNumber).Take(20))
        .ToListAsync(cancellationToken);

    if (characters.Count == 0)
    {
        return new CharacterContextGatheringMaintenanceResult
        {
            Success = true,
            Message = "No characters found in adventure",
            ProcessedCharacters = []
        };
    }

    var lastScene = await dbContext.Scenes
        .Where(s => s.AdventureId == adventureId)
        .OrderByDescending(s => s.SequenceNumber)
        .FirstOrDefaultAsync(cancellationToken);

    if (lastScene is null)
    {
        return new CharacterContextGatheringMaintenanceResult
        {
            Success = false,
            Message = "No scenes found in adventure",
            ProcessedCharacters = []
        };
    }

    var generationContextBuilder = serviceProvider.GetRequiredService<IGenerationContextBuilder>();
    var generationContext = await generationContextBuilder.BuildRegenerationContextAsync(
        adventureId, lastScene, cancellationToken);

    var characterContextGatherer = serviceProvider.GetRequiredService<CharacterContextGatherer>();

    var results = new List<CharacterContextGatheringResult>();
    var tasks = characters.Select(async character =>
    {
        try
        {
            var latestState = character.CharacterStates.FirstOrDefault();
            var sceneRewrites = character.CharacterSceneRewrites
                .OrderByDescending(sr => sr.SequenceNumber)
                .Take(20)
                .ToList();

            if (sceneRewrites.Count == 0)
            {
                return new CharacterContextGatheringResult
                {
                    CharacterId = character.Id,
                    CharacterName = character.Name,
                    Success = false,
                    Message = "No scene rewrites found"
                };
            }

            var characterContext = new CharacterContext
            {
                CharacterId = character.Id,
                Name = character.Name,
                Description = character.Description,
                Importance = character.Importance,
                CharacterState = latestState?.CharacterStats ?? new CharacterStats { Name = character.Name, Motivations = null, Routine = null },
                CharacterTracker = latestState?.Tracker,
                CharacterMemories = [],
                Relationships = [],
                SceneRewrites = sceneRewrites.Select(sr => new CharacterSceneContext
                {
                    Content = sr.Content,
                    SceneTracker = sr.SceneTracker,
                    SequenceNumber = sr.SequenceNumber,
                    GatheredContext = sr.GatheredContext
                }).ToList(),
                SimulationMetadata = latestState?.SimulationMetadata,
                IsDead = latestState?.IsDead ?? false
            };

            var gatheredContext = await characterContextGatherer.Invoke(
                generationContext, characterContext, cancellationToken);

            // Save to the latest scene rewrite
            var latestRewrite = sceneRewrites.First();
            latestRewrite.GatheredContext = gatheredContext;

            return new CharacterContextGatheringResult
            {
                CharacterId = character.Id,
                CharacterName = character.Name,
                Success = true,
                WorldContextCount = gatheredContext.WorldContext.Length,
                NarrativeContextCount = gatheredContext.NarrativeContext.Length,
                Message = "Context gathered successfully"
            };
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to gather context for character {CharacterName}", character.Name);
            return new CharacterContextGatheringResult
            {
                CharacterId = character.Id,
                CharacterName = character.Name,
                Success = false,
                Message = ex.Message
            };
        }
    }).ToList();

    var taskResults = await Task.WhenAll(tasks);
    results.AddRange(taskResults);

    // Save all changes
    await dbContext.SaveChangesAsync(cancellationToken);

    var successCount = results.Count(r => r.Success);
    logger.Information(
        "Context gathering completed for adventure {AdventureId}: {SuccessCount}/{TotalCount} characters processed",
        adventureId, successCount, results.Count);

    return new CharacterContextGatheringMaintenanceResult
    {
        Success = true,
        Message = $"Processed {successCount}/{results.Count} characters",
        ProcessedCharacters = results
    };
}
}

/// <summary>
///     Result of a character reflection maintenance operation.
/// </summary>
public sealed class CharacterReflectionMaintenanceResult
{
    public required Guid CharacterId { get; init; }

    public required string CharacterName { get; init; }

    public required Guid SceneId { get; init; }

    public required int SceneSequenceNumber { get; init; }

    public required bool AlreadyProcessed { get; init; }

    public required string? Message { get; init; }
}

/// <summary>
///     Result of context gathering for a single character.
/// </summary>
public sealed class CharacterContextGatheringResult
{
    public required Guid CharacterId { get; init; }
    public required string CharacterName { get; init; }
    public required bool Success { get; init; }
    public int WorldContextCount { get; init; }
    public int NarrativeContextCount { get; init; }
    public string? Message { get; init; }
}

/// <summary>
///     Result of context gathering maintenance operation.
/// </summary>
public sealed class CharacterContextGatheringMaintenanceResult
{
    public required bool Success { get; init; }
    public required string Message { get; init; }
    public required List<CharacterContextGatheringResult> ProcessedCharacters { get; init; }
}
