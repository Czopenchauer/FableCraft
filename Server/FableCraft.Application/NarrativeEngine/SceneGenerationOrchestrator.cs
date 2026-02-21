#pragma warning disable SKEXP0110 // Experimental Semantic Kernel agents

using System.Diagnostics;

using FableCraft.Application.Exceptions;
using FableCraft.Application.NarrativeEngine.Agents;
using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Application.NarrativeEngine.Workflow;
using FableCraft.Infrastructure;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

using Serilog;
using Serilog.Context;

namespace FableCraft.Application.NarrativeEngine;

public class GeneratedSceneOutput
{
    public required string Scene { get; init; }

    public required string[] Choices { get; init; }
}

public class SceneGenerationOutput
{
    public required Guid SceneId { get; set; }

    public required string? SubmittedAction { get; set; }

    public required GeneratedSceneOutput GeneratedScene { get; init; }

    public required string? ActionResolution { get; init; }

    public required TrackerDto? Tracker { get; init; }

    public required List<LoreDto>? NewLore { get; set; }

    public SceneMetadataDto? Metadata { get; set; }

    public static SceneGenerationOutput CreateFromScene(Scene scene)
    {
        return new SceneGenerationOutput
        {
            SceneId = scene.Id,
            GeneratedScene = new GeneratedSceneOutput
            {
                Scene = scene.NarrativeText,
                Choices = scene.CharacterActions.Select(x => x.ActionDescription)
                    .ToArray()
            },
            ActionResolution = scene.Metadata.ResolutionOutput,
            Tracker = scene.EnrichmentStatus == EnrichmentStatus.Enriched
                ? new TrackerDto
                {
                    Scene = scene.Metadata.Tracker!.Scene!,
                    MainCharacter = new MainCharacterTrackerDto
                    {
                        Tracker = scene.Metadata.Tracker!.MainCharacter!.MainCharacter!,
                        Description = scene.Metadata.Tracker.MainCharacter.MainCharacterDescription!
                    },
                    Characters = scene.CharacterStates.Select(x => new CharacterStateDto
                        {
                            CharacterId = x.CharacterId,
                            Name = x.CharacterStats.Name!,
                            State = x.CharacterStats,
                            Tracker = x.Tracker
                        })
                        .ToList()
                }
                : null,
            NewLore = scene.Lorebooks.Select(x => new LoreDto
                {
                    Title = x.Category,
                    Summary = x.Description
                })
                .ToList(),
            SubmittedAction = scene.CharacterActions.FirstOrDefault(x => x.Selected)?.ActionDescription,
            Metadata = scene.EnrichmentStatus == EnrichmentStatus.Enriched
                ? new SceneMetadataDto
                {
                    ResolutionOutput = scene.Metadata.ResolutionOutput,
                    GatheredContext = scene.Metadata.GatheredContext,
                    WriterObservation = scene.Metadata.WriterObservation,
                    ChroniclerState = scene.Metadata.ChroniclerState,
                    WriterGuidance = scene.Metadata.WriterGuidance
                }
                : null
        };
    }
}

public class SceneEnrichmentOutput
{
    public required Guid SceneId { get; set; }

    public required TrackerDto Tracker { get; init; }

    public required List<LoreDto> NewLore { get; init; }

    public SceneMetadataDto? Metadata { get; init; }

    public static SceneEnrichmentOutput CreateFromScene(Scene scene)
    {
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
                        Name = x.CharacterStats.Name!,
                        State = x.CharacterStats,
                        Tracker = x.Tracker
                    })
                    .ToList()
            },
            NewLore = scene.Lorebooks.Select(x => new LoreDto
            {
                Title = x.Category,
                Summary = x.Description
            }).ToList(),
            Metadata = new SceneMetadataDto
            {
                ResolutionOutput = scene.Metadata.ResolutionOutput,
                GatheredContext = scene.Metadata.GatheredContext,
                WriterObservation = scene.Metadata.WriterObservation,
                ChroniclerState = scene.Metadata.ChroniclerState,
                WriterGuidance = scene.Metadata.WriterGuidance
            }
        };
    }
}

public class LoreDto
{
    public string Title { get; set; } = null!;

    public string Summary { get; set; } = null!;
}

public class TrackerDto
{
    public required SceneTracker Scene { get; init; }

    public required MainCharacterTrackerDto MainCharacter { get; init; }

    public required List<CharacterStateDto> Characters { get; init; }
}

public class MainCharacterTrackerDto
{
    public required MainCharacterTracker Tracker { get; init; }

    public required string Description { get; init; }
}

public class CharacterStateDto
{
    public required Guid CharacterId { get; set; }

    public required string Name { get; set; } = null!;

    public required CharacterStats State { get; set; }

    public required CharacterTracker Tracker { get; set; }
}

public class SceneMetadataDto
{
    public string? ResolutionOutput { get; set; }

    public GatheredContext? GatheredContext { get; set; }

    public Dictionary<string, object>? WriterObservation { get; set; }

    public ChroniclerStoryState? ChroniclerState { get; set; }

    public string? WriterGuidance { get; set; }
}

internal sealed class SceneGenerationOrchestrator(
    ILogger logger,
    ApplicationDbContext dbContext,
    IEnumerable<IProcessor> processors,
    IHostApplicationLifetime hostLifetime,
    IGenerationContextBuilder contextBuilder)
{

    public async Task<Scene> GenerateSceneAsync(
        Guid adventureId,
        string playerAction,
        CancellationToken cancellationToken)
    {
        var (context, step) = await contextBuilder.GetOrCreateGenerationContextAsync(adventureId, playerAction, cancellationToken);
        ProcessExecutionContext.SceneId.Value = context.NewSceneId;

        if (step == GenerationProcessStep.SceneGenerated)
        {
            return await GetGeneratedSceneWithoutEnrichment();
        }

        if (step == GenerationProcessStep.GeneratingScene)
        {
            throw new SceneGenerationConcurrencyException(adventureId);
        }

        var workflow = new[] { processors.First(p => p is WriterAgent), processors.First(p => p is SaveSceneWithoutEnrichment) };

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            hostLifetime.ApplicationStopping);
        var stopwatch = Stopwatch.StartNew();
        foreach (var processor in workflow)
        {
            try
            {
                stopwatch.Restart();
                await processor.Invoke(context, linkedCts.Token);
                await dbContext.GenerationProcesses
                    .Where(x => x.AdventureId == adventureId)
                    .ExecuteUpdateAsync(x => x.SetProperty(y => y.Context, context.ToJsonString()),
                        cancellationToken);
            }
            catch (Exception)
            {
                logger.Error("Error during generating scene. Processor: {Processor} for adventure {AdventureId}", processor.GetType().Name, adventureId);
                await dbContext.GenerationProcesses
                    .Where(x => x.AdventureId == adventureId)
                    .ExecuteUpdateAsync(x => x.SetProperty(y => y.Context, context.ToJsonString()).SetProperty(y => y.Step, GenerationProcessStep.NotStarted),
                        cancellationToken);
                throw;
            }
        }

        await dbContext.GenerationProcesses
            .Where(x => x.AdventureId == adventureId)
            .ExecuteUpdateAsync(x => x.SetProperty(y => y.Context, context.ToJsonString()).SetProperty(y => y.Step, GenerationProcessStep.SceneGenerated),
                cancellationToken);

        return await GetGeneratedSceneWithoutEnrichment();

        async Task<Scene> GetGeneratedSceneWithoutEnrichment()
        {
            var newScene = await dbContext.Scenes
                .Include(x => x.CharacterActions)
                .Where(x => x.AdventureId == adventureId)
                .OrderByDescending(x => x.SequenceNumber)
                .FirstAsync(cancellationToken);
            return newScene;
        }
    }

    public async Task<SceneEnrichmentOutput> EnrichSceneAsync(
        Guid adventureId,
        Guid sceneId,
        CancellationToken cancellationToken)
    {
        var scene = await dbContext.Scenes
            .Where(s => s.Id == sceneId && s.AdventureId == adventureId)
            .Include(x => x.CharacterStates)
            .Include(x => x.CharacterActions)
            .Include(x => x.Lorebooks)
            .FirstOrDefaultAsync(cancellationToken);

        if (scene == null)
        {
            throw new SceneNotFoundException(adventureId);
        }

        if (scene.EnrichmentStatus == EnrichmentStatus.Enriching)
        {
            throw new SceneEnrichmentConcurrencyException(sceneId);
        }

        if (scene.EnrichmentStatus == EnrichmentStatus.Enriched)
        {
            return SceneEnrichmentOutput.CreateFromScene(scene);
        }

        var context = await contextBuilder.BuildEnrichmentContextAsync(adventureId, cancellationToken);

        var stopwatch = Stopwatch.StartNew();
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            hostLifetime.ApplicationStopping);
        try
        {
            await dbContext.Scenes
                .Where(s => s.Id == sceneId)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.EnrichmentStatus, EnrichmentStatus.Enriching),
                    cancellationToken);
            await processors.First(p => p is SceneTrackerProcessor).Invoke(context, linkedCts.Token);
            logger.Information("[Enrichment] SceneTrackerProcessor took {ElapsedMilliseconds} ms",
                stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            await dbContext.Scenes
                .Where(s => s.Id == sceneId)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.EnrichmentStatus, EnrichmentStatus.EnrichmentFailed),
                    cancellationToken);
            logger.Error(ex, "Error during SceneTrackerProcessor for adventure {AdventureId}", adventureId);
            throw;
        }

        var parallelProcessors = new[]
        {
            processors.First(p => p is ContentGenerator), processors.First(p => p is CharacterTrackersProcessor), processors.First(p => p is SimulationOrchestrator),
            processors.First(p => p is ContextGatherer)
        };

        stopwatch.Restart();
        using (LogContext.PushProperty("AdventureId", adventureId))
        {
            using (LogContext.PushProperty("SceneId", context.NewSceneId))
            {
                try
                {
                    await Task.WhenAll(parallelProcessors.Select(p => p.Invoke(context, linkedCts.Token)));
                    await processors.First(x => x is SaveSceneEnrichment).Invoke(context, linkedCts.Token);
                    logger.Information("[Enrichment] Parallel processing took {ElapsedMilliseconds} ms",
                        stopwatch.ElapsedMilliseconds);
                }
                catch (Exception ex)
                {
                    await dbContext.GenerationProcesses
                        .Where(x => x.AdventureId == adventureId)
                        .ExecuteUpdateAsync(x => x.SetProperty(y => y.Context, context.ToJsonString()),
                            CancellationToken.None);
                    await dbContext.Scenes
                        .Where(s => s.Id == sceneId)
                        .ExecuteUpdateAsync(s => s.SetProperty(x => x.EnrichmentStatus, EnrichmentStatus.EnrichmentFailed),
                            CancellationToken.None);
                    logger.Error(ex,
                        "Error during scene Enrichment for adventure {AdventureId}",
                        adventureId);
                    throw;
                }
            }
        }

        scene = await dbContext.Scenes
            .AsNoTracking()
            .Where(s => s.AdventureId == adventureId)
            .OrderByDescending(x => x.SequenceNumber)
            .Take(1)
            .Include(x => x.CharacterStates)
            .Include(x => x.CharacterActions)
            .Include(x => x.Lorebooks)
            .SingleAsync(cancellationToken);
        await dbContext.GenerationProcesses
            .Where(x => x.AdventureId == adventureId)
            .ExecuteDeleteAsync(cancellationToken);

        return SceneEnrichmentOutput.CreateFromScene(scene);
    }

    public async Task<SceneEnrichmentOutput> RegenerateEnrichmentAsync(
        Guid adventureId,
        Guid sceneId,
        List<string> agentsToRegenerate,
        CancellationToken cancellationToken)
    {
        var scene = await dbContext.Scenes
            .Where(s => s.Id == sceneId && s.AdventureId == adventureId)
            .Include(x => x.CharacterStates)
            .Include(x => x.CharacterActions)
            .Include(x => x.Lorebooks)
            .FirstOrDefaultAsync(cancellationToken);

        if (scene == null)
        {
            throw new SceneNotFoundException(adventureId);
        }

        var context = await contextBuilder.BuildRegenerationContextAsync(adventureId, scene, cancellationToken);

        ClearFieldsForAgents(context, agentsToRegenerate);

        var stopwatch = Stopwatch.StartNew();
        try
        {
            await processors.First(p => p is SceneTrackerProcessor).Invoke(context, cancellationToken);
            logger.Information("[Enrichment Regeneration] SceneTrackerProcessor took {ElapsedMilliseconds} ms",
                stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error during SceneTrackerProcessor regeneration for adventure {AdventureId}", adventureId);
            throw;
        }

        var parallelProcessors = new[]
        {
            processors.First(p => p is ContentGenerator), processors.First(p => p is CharacterTrackersProcessor), processors.First(p => p is SimulationOrchestrator)
        };

        stopwatch.Restart();
        try
        {
            await Task.WhenAll(parallelProcessors.Select(p => p.Invoke(context, cancellationToken)));
            logger.Information("[Enrichment Regeneration] Parallel processing took {ElapsedMilliseconds} ms",
                stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            logger.Error(ex,
                "Error during enrichment regeneration for adventure {AdventureId}",
                adventureId);
            throw;
        }

        stopwatch.Restart();
        try
        {
            var saveProcessor = processors.First(p => p is SaveSceneEnrichment);
            await saveProcessor.Invoke(context, cancellationToken);
            logger.Information("[Enrichment Regeneration] SaveSceneEnrichment took {ElapsedMilliseconds} ms",
                stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            logger.Error(ex,
                "Error during SaveSceneEnrichment regeneration for adventure {AdventureId}",
                adventureId);
            throw;
        }

        scene = await dbContext.Scenes
            .Where(s => s.Id == sceneId)
            .Include(x => x.CharacterStates)
            .Include(x => x.Lorebooks)
            .FirstAsync(cancellationToken);

        return SceneEnrichmentOutput.CreateFromScene(scene);
    }

    private static void ClearFieldsForAgents(GenerationContext context, List<string> agentsToRegenerate)
    {
        foreach (var agent in agentsToRegenerate)
        {
            switch (agent)
            {
                case nameof(EnrichmentAgent.CharacterCrafter):
                    context.NewCharacters = [];
                    break;
                case nameof(EnrichmentAgent.LoreCrafter):
                    context.NewLore = [];
                    break;
                case nameof(EnrichmentAgent.LocationCrafter):
                    context.NewLocations = null;
                    break;
                case nameof(EnrichmentAgent.ItemCrafter):
                    context.NewItems = null;
                    break;

                case nameof(EnrichmentAgent.SceneTracker):
                    context.NewTracker?.Scene = null;
                    break;
                case nameof(EnrichmentAgent.MainCharacterTracker):
                    if (context.NewTracker?.MainCharacter != null)
                    {
                        context.NewTracker.MainCharacter.MainCharacter = null!;
                        context.NewTracker.MainCharacter.MainCharacterDescription = null;
                    }

                    break;
                case nameof(EnrichmentAgent.CharacterTracker):
                    context.CharacterUpdates = [];
                    break;
            }
        }
    }
}