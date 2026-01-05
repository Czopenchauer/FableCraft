using System.Diagnostics;
using System.Text.Json;

using FableCraft.Application.Exceptions;
using FableCraft.Application.NarrativeEngine.Agents;
using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Application.NarrativeEngine.Workflow;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;

using Microsoft.EntityFrameworkCore;

using Serilog;

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
                            Name = x.CharacterStats.CharacterIdentity.FullName!,
                            Description = x.Description,
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
            SubmittedAction = scene.CharacterActions.FirstOrDefault(x => x.Selected)?.ActionDescription
        };
    }
}

public class SceneEnrichmentOutput
{
    public required Guid SceneId { get; set; }

    public required TrackerDto Tracker { get; init; }

    public required List<LoreDto> NewLore { get; init; }

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
                        Name = x.CharacterStats.CharacterIdentity.FullName!,
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

    public required string Description { get; set; } = null!;

    public required CharacterStats State { get; set; }

    public required CharacterTracker Tracker { get; set; }
}

internal sealed class SceneGenerationOrchestrator(
    ILogger logger,
    ApplicationDbContext dbContext,
    IEnumerable<IProcessor> processors)
{
    private const int NumberOfScenesToInclude = 20;

    public async Task<Scene> GenerateSceneAsync(
        Guid adventureId,
        string playerAction,
        CancellationToken cancellationToken)
    {
        var context = await GetOrCreateGenerationContext(adventureId, playerAction, cancellationToken);

        if (context.GenerationProcessStep == GenerationProcessStep.SceneGenerated)
        {
            return await GetGeneratedSceneWithoutEnrichment();
        }

        if (context.GenerationProcessStep == GenerationProcessStep.EnrichmentCompleted)
        {
            throw new InvalidOperationException("Cannot generate scene with enrichment completed");
        }

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true
        };
        var workflow = new[]
        {
            processors.First(p => p is ResolutionAgent),
            processors.First(p => p is WriterAgent),
            processors.First(p => p is SaveSceneWithoutEnrichment)
        };

        var stopwatch = Stopwatch.StartNew();
        foreach (IProcessor processor in workflow)
        {
            try
            {
                stopwatch.Restart();
                await processor.Invoke(context, cancellationToken);
                await dbContext.GenerationProcesses
                    .Where(x => x.AdventureId == adventureId)
                    .ExecuteUpdateAsync(x => x.SetProperty(y => y.Context, JsonSerializer.Serialize(context, options)),
                        cancellationToken);
                logger.Information("[Generation] Step {GenerationProcessStep} took {ElapsedMilliseconds} ms",
                    context.GenerationProcessStep,
                    stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                await dbContext.GenerationProcesses
                    .Where(x => x.AdventureId == adventureId)
                    .ExecuteUpdateAsync(x => x.SetProperty(y => y.Context, JsonSerializer.Serialize(context, options)),
                        cancellationToken);
                logger.Error(ex,
                    "Error during scene generation for adventure {AdventureId} at step {GenerationProcessStep}",
                    adventureId,
                    context.GenerationProcessStep);
                throw;
            }
        }

        return await GetGeneratedSceneWithoutEnrichment();

        async Task<Scene> GetGeneratedSceneWithoutEnrichment()
        {
            Scene newScene = await dbContext.Scenes
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
        Scene? scene = await dbContext.Scenes
            .Where(s => s.Id == sceneId && s.AdventureId == adventureId)
            .Include(x => x.CharacterStates)
            .Include(x => x.CharacterActions)
            .Include(x => x.Lorebooks)
            .FirstOrDefaultAsync(cancellationToken);

        if (scene == null)
        {
            throw new SceneNotFoundException(adventureId);
        }

        if (scene.EnrichmentStatus == EnrichmentStatus.Enriched)
        {
            return SceneEnrichmentOutput.CreateFromScene(scene);
        }

        await dbContext.Scenes
            .Where(s => s.Id == sceneId)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.EnrichmentStatus, EnrichmentStatus.Enriching),
                cancellationToken);

        GenerationContext context = await BuildEnrichmentContext(adventureId, cancellationToken);

        // Run ContentGenerator, TrackerProcessor, and ContextGatherer in parallel
        var parallelProcessors = new[]
        {
            processors.First(p => p is ContentGenerator),
            processors.First(p => p is TrackerProcessor)
        };

        var stopwatch = Stopwatch.StartNew();
        try
        {
            await Task.WhenAll(parallelProcessors.Select(p => p.Invoke(context, cancellationToken)).Append(processors.First(p => p is ContextGatherer).Invoke(context, cancellationToken)));
            await dbContext.GenerationProcesses
                .Where(x => x.AdventureId == adventureId)
                .ExecuteUpdateAsync(x => x.SetProperty(y => y.Context, context.ToJsonString()),
                    cancellationToken);
            logger.Information("[Enrichment] Parallel processing took {ElapsedMilliseconds} ms",
                stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            await dbContext.GenerationProcesses
                .Where(x => x.AdventureId == adventureId)
                .ExecuteUpdateAsync(x => x.SetProperty(y => y.Context, context.ToJsonString()),
                    cancellationToken);
            await dbContext.Scenes
                .Where(s => s.Id == sceneId)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.EnrichmentStatus, EnrichmentStatus.EnrichmentFailed),
                    cancellationToken);
            logger.Error(ex,
                "Error during scene Enrichment for adventure {AdventureId}",
                adventureId);
            throw;
        }

        // Run SaveSceneEnrichment after parallel processing completes
        stopwatch.Restart();
        try
        {
            var saveProcessor = processors.First(p => p is SaveSceneEnrichment);
            await saveProcessor.Invoke(context, cancellationToken);
            logger.Information("[Enrichment] SaveSceneEnrichment took {ElapsedMilliseconds} ms",
                stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            await dbContext.Scenes
                .Where(s => s.Id == sceneId)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.EnrichmentStatus, EnrichmentStatus.EnrichmentFailed),
                    cancellationToken);
            logger.Error(ex,
                "Error during SaveSceneEnrichment for adventure {AdventureId}",
                adventureId);
            throw;
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

        return SceneEnrichmentOutput.CreateFromScene(scene);
    }

    public async Task<SceneEnrichmentOutput> RegenerateEnrichmentAsync(
        Guid adventureId,
        Guid sceneId,
        List<string> agentsToRegenerate,
        CancellationToken cancellationToken)
    {
        Scene? scene = await dbContext.Scenes
            .Where(s => s.Id == sceneId && s.AdventureId == adventureId)
            .Include(x => x.CharacterStates)
            .Include(x => x.CharacterActions)
            .Include(x => x.Lorebooks)
            .FirstOrDefaultAsync(cancellationToken);

        if (scene == null)
        {
            throw new SceneNotFoundException(adventureId);
        }

        GenerationContext context = await BuildRegenerationContextFromScene(adventureId, scene, cancellationToken);

        ClearFieldsForAgents(context, agentsToRegenerate);

        var parallelProcessors = new[]
        {
            processors.First(p => p is ContentGenerator),
            processors.First(p => p is TrackerProcessor)
        };

        var stopwatch = Stopwatch.StartNew();
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

        // Run SaveSceneEnrichment after parallel processing completes
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

    private async Task<GenerationContext> BuildRegenerationContextFromScene(
        Guid adventureId,
        Scene scene,
        CancellationToken cancellationToken)
    {
        var adventure = await dbContext.Adventures
            .Where(x => x.Id == adventureId)
            .Include(x => x.AgentLlmPresets)
            .ThenInclude(x => x.LlmPreset)
            .Select(x => new
            {
                x.TrackerStructure, x.MainCharacter, x.AgentLlmPresets,
                PromptPaths = x.PromptPath, x.AdventureStartTime, x.WorldSettings,
                x.AuthorNotes
            })
            .SingleAsync(cancellationToken);

        // Get previous scenes for context (skip the current scene being regenerated)
        var scenes = await dbContext.Scenes
            .AsSplitQuery()
            .Where(x => x.AdventureId == adventureId && x.SequenceNumber < scene.SequenceNumber)
            .Include(x => x.CharacterActions)
            .Include(x => x.CharacterMemories)
            .Include(x => x.CharacterRelationships)
            .Include(x => x.CharacterSceneRewrites)
            .OrderByDescending(x => x.SequenceNumber)
            .Take(NumberOfScenesToInclude)
            .ToListAsync(cancellationToken);

        var createdLore = await dbContext.Scenes
            .AsSplitQuery()
            .Where(x => x.AdventureId == adventureId && x.SequenceNumber < scene.SequenceNumber)
            .OrderByDescending(x => x.SequenceNumber)
            .Take(1)
            .Include(x => x.Lorebooks)
            .SelectMany(x => x.Lorebooks)
            .ToArrayAsync(cancellationToken);

        // Skip the most recent character state as that's the one being regenerated
        var adventureCharacters = await GetCharactersForRegeneration(adventureId, cancellationToken);

        var context = new GenerationContext
        {
            AdventureId = adventureId,
            PlayerAction = scene.CharacterActions.FirstOrDefault(x => x.Selected)?.ActionDescription ?? string.Empty,
            GenerationProcessStep = GenerationProcessStep.SceneGenerated,
            NewSceneId = scene.Id,
            NewResolution = scene.Metadata.ResolutionOutput,
            NewScene = new GeneratedScene
            {
                Scene = scene.NarrativeText,
                Choices = scene.CharacterActions.Select(x => x.ActionDescription).ToArray()
            },
            NewTracker = scene.Metadata.Tracker != null
                ? new Tracker
                {
                    Scene = scene.Metadata.Tracker.Scene,
                    MainCharacter = scene.Metadata.Tracker.MainCharacter
                }
                : null,
            CharacterUpdates = scene.CharacterStates.Where(c => c.SequenceNumber != 0).Select(cs => new CharacterContext
            {
                CharacterId = cs.CharacterId,
                Name = cs.CharacterStats.CharacterIdentity.FullName!,
                Description = cs.Description,
                CharacterState = cs.CharacterStats,
                CharacterTracker = cs.Tracker,
                CharacterMemories = scene.CharacterMemories.Where(x => x.CharacterId == cs.CharacterId).Select(x => new MemoryContext
                {
                    MemoryContent = x.Summary,
                    Salience = x.Salience,
                    Data = x.Data,
                    SceneTracker = x.SceneTracker
                }).ToList(),
                Relationships = scene.CharacterRelationships.Where(x => x.CharacterId == cs.CharacterId).Select(x => new CharacterRelationshipContext
                {
                    TargetCharacterName = x.TargetCharacterName,
                    Data = x.Data,
                    StoryTracker = x.StoryTracker,
                    SequenceNumber = x.SequenceNumber
                }).ToList(),
                SceneRewrites = scene.CharacterSceneRewrites.Where(x => x.CharacterId == cs.CharacterId).Select(x => new CharacterSceneContext
                {
                    Content = x.Content,
                    StoryTracker = x.SceneTracker,
                    SequenceNumber = x.SequenceNumber
                }).ToList(),
            }).ToList(),
            // Sequence number 0 indicates newly introduced characters in this scene
            NewCharacters = scene.CharacterStates.Where(c => c.SequenceNumber == 0).Select(cs => new CharacterContext
            {
                CharacterId = cs.CharacterId,
                Name = cs.CharacterStats.CharacterIdentity.FullName!,
                Description = cs.Description,
                CharacterState = cs.CharacterStats,
                CharacterTracker = cs.Tracker,
                CharacterMemories = scene.CharacterMemories.Where(x => x.CharacterId == cs.CharacterId).Select(x => new MemoryContext
                {
                    MemoryContent = x.Summary,
                    Salience = x.Salience,
                    Data = x.Data,
                    SceneTracker = x.SceneTracker
                }).ToList(),
                Relationships = scene.CharacterRelationships.Where(x => x.CharacterId == cs.CharacterId).Select(x => new CharacterRelationshipContext
                {
                    TargetCharacterName = x.TargetCharacterName,
                    Data = x.Data,
                    StoryTracker = x.StoryTracker,
                    SequenceNumber = x.SequenceNumber
                }).ToList(),
                SceneRewrites = scene.CharacterSceneRewrites.Where(x => x.CharacterId == cs.CharacterId).Select(x => new CharacterSceneContext
                {
                    Content = x.Content,
                    StoryTracker = x.SceneTracker,
                    SequenceNumber = x.SequenceNumber
                }).ToList(),
            }).ToArray(),
            NewLore = scene.Lorebooks.Where(x => x.Category == nameof(LorebookCategory.Lore))
                .Select(lb => JsonSerializer.Deserialize<GeneratedLore>(lb.Content)!).ToArray(),
            NewLocations = scene.Lorebooks.Where(x => x.Category == nameof(LorebookCategory.Location))
                .Select(lb => JsonSerializer.Deserialize<LocationGenerationResult>(lb.Content)!).ToArray(),
            NewItems = scene.Lorebooks.Where(x => x.Category == nameof(LorebookCategory.Item))
                .Select(lb => JsonSerializer.Deserialize<GeneratedItem>(lb.Content)!).ToArray(),
        };

        context.SetupRequiredFields(
            scenes.Select(SceneContext.CreateFromScene).ToArray(),
            adventure.TrackerStructure,
            adventure.MainCharacter,
            adventureCharacters,
            adventure.AgentLlmPresets.ToArray(),
            adventure.PromptPaths,
            adventure.AdventureStartTime,
            adventure.WorldSettings,
            adventure.AuthorNotes,
            createdLore);

        return context;
    }

    private static void ClearFieldsForAgents(GenerationContext context, List<string> agentsToRegenerate)
    {
        foreach (var agent in agentsToRegenerate)
        {
            switch (agent)
            {
                case nameof(EnrichmentAgent.CharacterCrafter):
                    context.NewCharacters = null;
                    break;
                case nameof(EnrichmentAgent.LoreCrafter):
                    context.NewLore = null;
                    break;
                case nameof(EnrichmentAgent.LocationCrafter):
                    context.NewLocations = null;
                    break;
                case nameof(EnrichmentAgent.ItemCrafter):
                    context.NewItems = null;
                    break;

                case nameof(EnrichmentAgent.StoryTracker):
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
                    context.CharacterUpdates = null;
                    break;
            }
        }
    }

    private async Task<GenerationContext> BuildEnrichmentContext(
        Guid adventureId,
        CancellationToken cancellationToken)
    {
        var adventure = await dbContext.Adventures
            .Where(x => x.Id == adventureId)
            .Include(x => x.AgentLlmPresets)
            .ThenInclude(x => x.LlmPreset)
            .Select(x => new
            {
                x.TrackerStructure, x.MainCharacter, x.AgentLlmPresets,
                PromptPaths = x.PromptPath, x.AdventureStartTime, x.WorldSettings, x.AuthorNotes
            })
            .SingleAsync(cancellationToken);

        // Skip the most recent scene as that's the one being enriched, and it has separate field
        var scenes = await dbContext.Scenes
            .Where(x => x.AdventureId == adventureId)
            .Include(x => x.CharacterActions)
            .OrderByDescending(x => x.SequenceNumber)
            .Take(NumberOfScenesToInclude)
            .Skip(1)
            .ToListAsync(cancellationToken);

        var generationProcess = await dbContext.GenerationProcesses.FirstAsync(x => x.AdventureId == adventureId, cancellationToken: cancellationToken);
        var generationContext = generationProcess.GetContextAs<GenerationContext>();
        var adventureCharacters = await GetCharacters(adventureId, cancellationToken);
        var createdLore = await dbContext.Scenes
            .AsSplitQuery()
            .Where(x => x.AdventureId == adventureId)
            .OrderByDescending(x => x.SequenceNumber)
            .Take(1)
            .Include(x => x.Lorebooks)
            .SelectMany(x => x.Lorebooks)
            .ToArrayAsync(cancellationToken);
        generationContext.SetupRequiredFields(
            scenes.Select(SceneContext.CreateFromScene).ToArray(),
            adventure.TrackerStructure,
            adventure.MainCharacter,
            adventureCharacters,
            adventure.AgentLlmPresets.ToArray(),
            adventure.PromptPaths,
            adventure.AdventureStartTime,
            adventure.WorldSettings,
            adventure.AuthorNotes,
            createdLore);
        return generationContext;
    }

    private async Task<GenerationContext> GetOrCreateGenerationContext(Guid adventureId, string playerAction, CancellationToken cancellationToken)
    {
        var adventure = await dbContext
            .Adventures
            .Where(x => x.Id == adventureId)
            .Include(x => x.AgentLlmPresets)
            .ThenInclude(x => x.LlmPreset)
            .Select(x => new
            {
                x.TrackerStructure, x.MainCharacter, x.AgentLlmPresets,
                PromptPaths = x.PromptPath, x.AdventureStartTime, x.WorldSettings, x.AuthorNotes
            })
            .SingleAsync(cancellationToken);

        var scenes = await dbContext
            .Scenes
            .Where(x => x.AdventureId == adventureId)
            .Include(x => x.CharacterActions)
            .OrderByDescending(x => x.SequenceNumber)
            .Take(NumberOfScenesToInclude)
            .ToListAsync(cancellationToken);
        var adventureCharacters = await GetCharacters(adventureId, cancellationToken);
        GenerationProcess? generationProcess = await dbContext.GenerationProcesses.Where(x => x.AdventureId == adventureId).FirstOrDefaultAsync(cancellationToken);
        GenerationContext context;
        if (generationProcess != null)
        {
            context = generationProcess.GetContextAs<GenerationContext>();
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (context == null || context.PlayerAction != playerAction)
            {
                await dbContext.GenerationProcesses
                    .Where(x => x.AdventureId == adventureId)
                    .ExecuteDeleteAsync(cancellationToken);
                context = await CreateNewProcess();
            }
        }
        else
        {
            context = await CreateNewProcess();
        }

        if (context == null)
        {
            throw new InvalidOperationException("Failed to deserialize generation context from the database.");
        }

        var createdLore = await dbContext.Scenes
            .AsSplitQuery()
            .Where(x => x.AdventureId == adventureId)
            .OrderByDescending(x => x.SequenceNumber)
            .Take(1)
            .Include(x => x.Lorebooks)
            .SelectMany(x => x.Lorebooks)
            .ToArrayAsync(cancellationToken);

        context.SetupRequiredFields(
            scenes.Select(SceneContext.CreateFromScene).ToArray(),
            adventure.TrackerStructure,
            adventure.MainCharacter,
            adventureCharacters,
            adventure.AgentLlmPresets.ToArray(),
            adventure.PromptPaths,
            adventure.AdventureStartTime,
            adventure.WorldSettings,
            adventure.AuthorNotes,
            createdLore);

        return context;

        async Task<GenerationContext> CreateNewProcess()
        {
            var newContext = new GenerationContext
            {
                AdventureId = adventureId,
                PlayerAction = playerAction,
                GenerationProcessStep = GenerationProcessStep.NotStarted
            };
            var process = new GenerationProcess
            {
                AdventureId = adventureId,
                Context = newContext.ToJsonString()
            };
            await dbContext.GenerationProcesses.AddAsync(process,
                cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            return newContext;
        }
    }

    /// <summary>
    ///     Gets the latest character contexts for all characters in the adventure.
    /// </summary>
    /// <param name="adventureId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task<List<CharacterContext>> GetCharacters(Guid adventureId, CancellationToken cancellationToken)
    {
        var existingCharacters = await dbContext
            .Characters
            .AsSplitQuery()
            .Where(x => x.AdventureId == adventureId)
            .Include(x => x.CharacterStates.OrderByDescending(cs => cs.SequenceNumber).Take(1))
            .Include(x => x.CharacterMemories)
            .Include(x => x.CharacterRelationships)
            .Include(x => x.CharacterSceneRewrites.OrderByDescending(c => c.SequenceNumber).Take(20))
            .ToListAsync(cancellationToken);

        return existingCharacters
            .Select(x => new CharacterContext
            {
                Description = x.CharacterStates.Single()
                    .Description,
                Name = x.Name,
                CharacterState = x.CharacterStates.Single()
                    .CharacterStats,
                CharacterTracker = x.CharacterStates.Single()
                    .Tracker,
                CharacterId = x.Id,
                CharacterMemories = x.CharacterMemories.Select(y => new MemoryContext
                {
                    MemoryContent = y.Summary,
                    Salience = y.Salience,
                    Data = y.Data,
                    SceneTracker = y.SceneTracker
                }).ToList(),
                // Group by target and take latest relationship per target character
                Relationships = x.CharacterRelationships
                    .GroupBy(r => r.TargetCharacterName)
                    .Select(g => g.OrderByDescending(r => r.SequenceNumber).First())
                    .Select(y => new CharacterRelationshipContext
                    {
                        TargetCharacterName = y!.TargetCharacterName,
                        Data = y.Data,
                        StoryTracker = y.StoryTracker,
                        SequenceNumber = y.SequenceNumber
                    })
                    .ToList(),
                SceneRewrites = x.CharacterSceneRewrites.Select(y => new CharacterSceneContext
                {
                    Content = y.Content,
                    StoryTracker = y.SceneTracker,
                    SequenceNumber = y.SequenceNumber
                }).ToList(),
            }).ToList();
    }

    private async Task<List<CharacterContext>> GetCharactersForRegeneration(Guid adventureId, CancellationToken cancellationToken)
    {
        // Basically Current STATE - 1. Skip the latest states and take the one before that.
        var existingCharacters = await dbContext
            .Characters
            .AsSplitQuery()
            .Where(x => x.AdventureId == adventureId)
            .Include(x => x.CharacterStates.OrderByDescending(cs => cs.SequenceNumber).Skip(1).Take(1))
            .Include(x => x.CharacterMemories)
            .Include(x => x.CharacterRelationships)
            .Include(x => x.CharacterSceneRewrites.OrderByDescending(c => c.SequenceNumber).Skip(1).Take(20))
            .ToListAsync(cancellationToken);

        return existingCharacters
            .Select(x => new CharacterContext
            {
                Description = x.CharacterStates.Single()
                    .Description,
                Name = x.Name,
                CharacterState = x.CharacterStates.Single()
                    .CharacterStats,
                CharacterTracker = x.CharacterStates.Single()
                    .Tracker,
                CharacterId = x.Id,
                CharacterMemories = x.CharacterMemories.Select(y => new MemoryContext
                {
                    MemoryContent = y.Summary,
                    Salience = y.Salience,
                    Data = y.Data,
                    SceneTracker = y.SceneTracker
                }).ToList(),
                // Group by target and take second-latest relationship per target (skip the most recent)
                Relationships = x.CharacterRelationships
                    .GroupBy(r => r.TargetCharacterName)
                    .Select(g => g.OrderByDescending(r => r.SequenceNumber).Skip(1).FirstOrDefault())
                    .Where(r => r != null)
                    .Select(y => new CharacterRelationshipContext
                    {
                        TargetCharacterName = y!.TargetCharacterName,
                        Data = y.Data,
                        StoryTracker = y.StoryTracker,
                        SequenceNumber = y.SequenceNumber
                    })
                    .ToList()!,
                SceneRewrites = x.CharacterSceneRewrites.Select(y => new CharacterSceneContext()
                {
                    Content = y.Content,
                    StoryTracker = y.SceneTracker,
                    SequenceNumber = y.SequenceNumber
                }).ToList(),
            }).ToList();
    }
}