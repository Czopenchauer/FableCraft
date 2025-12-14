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

    public required NarrativeDirectorOutput? NarrativeDirectorOutput { get; init; }

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
            NarrativeDirectorOutput = scene.Metadata.NarrativeMetadata,
            Tracker = scene.EnrichmentStatus == EnrichmentStatus.Enriched
                ? new TrackerDto
                {
                    Story = scene.Metadata.Tracker!.Story,
                    MainCharacter = new MainCharacterTrackerDto
                    {
                        Tracker = scene.Metadata.Tracker!.MainCharacter!.MainCharacter!,
                        Development = scene.Metadata.Tracker!.MainCharacter!.MainCharacterDevelopment!,
                        Description = scene.Metadata.Tracker.MainCharacter.MainCharacterDescription!
                    },
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
                Story = scene.Metadata.Tracker!.Story,
                MainCharacter = new MainCharacterTrackerDto
                {
                    Tracker = scene.Metadata.Tracker!.MainCharacter!.MainCharacter!,
                    Development = scene.Metadata.Tracker!.MainCharacter!.MainCharacterDevelopment!,
                    Description = scene.Metadata.Tracker!.MainCharacter.MainCharacterDescription!
                },
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
}

public class LoreDto
{
    public string Title { get; set; } = null!;

    public string Summary { get; set; } = null!;
}

public class TrackerDto
{
    public required StoryTracker Story { get; init; }

    public required MainCharacterTrackerDto MainCharacter { get; init; }

    public required List<CharacterStateDto> Characters { get; init; }
}

public class MainCharacterTrackerDto
{
    public required CharacterTracker Tracker { get; init; }

    public required CharacterDevelopmentTracker Development { get; init; }

    public required string Description { get; init; }
}

public class CharacterStateDto
{
    public required Guid CharacterId { get; set; }

    public required string Name { get; set; } = null!;

    public required string Description { get; set; } = null!;

    public required CharacterStats State { get; set; }

    public required CharacterTracker Tracker { get; set; }

    public required CharacterDevelopmentTracker Development { get; set; }
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
            processors.First(p => p is ContextGatherer),
            processors.First(p => p is NarrativeDirectorAgent),
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
            SceneEnrichmentOutput.CreateFromScene(scene);
        }

        await dbContext.Scenes
            .Where(s => s.Id == sceneId)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.EnrichmentStatus, EnrichmentStatus.Enriching),
                cancellationToken);

        GenerationContext context = await BuildEnrichmentContext(adventureId, cancellationToken);

        var workflow = new[]
        {
            processors.First(p => p is ContentGenerator),
            processors.First(p => p is TrackerProcessor),
            processors.First(p => p is SaveSceneEnrichment)
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
                    .ExecuteUpdateAsync(x => x.SetProperty(y => y.Context, context.ToJsonString()),
                        cancellationToken);
                logger.Information("[Enrichment] Step {GenerationProcessStep} took {ElapsedMilliseconds} ms",
                    context.GenerationProcessStep,
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
                    "Error during scene Enrichment for adventure {AdventureId} at step {GenerationProcessStep}",
                    adventureId,
                    context.GenerationProcessStep);
                throw;
            }
        }

        return SceneEnrichmentOutput.CreateFromScene(scene);
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
                PromptPaths = x.PromptPath, x.AdventureStartTime
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
        generationContext.SetupRequiredFields(
            scenes.Select(SceneContext.CreateFromScene).ToArray(),
            adventure.TrackerStructure,
            adventure.MainCharacter,
            adventureCharacters,
            adventure.AgentLlmPresets.ToArray(),
            adventure.PromptPaths,
            adventure.AdventureStartTime);
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
                PromptPaths = x.PromptPath, x.AdventureStartTime
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

        context.SetupRequiredFields(
            scenes.Select(SceneContext.CreateFromScene).ToArray(),
            adventure.TrackerStructure,
            adventure.MainCharacter,
            adventureCharacters,
            adventure.AgentLlmPresets.ToArray(),
            adventure.PromptPaths,
            adventure.AdventureStartTime);

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
            .Where(x => x.AdventureId == adventureId)
            .GroupBy(x => x.CharacterId)
            .ToListAsync(cancellationToken);
        return existingCharacters
            .Select(g => g.OrderByDescending(x => x.SequenceNumber).First())
            .Select(x => new CharacterContext
            {
                Description = x.Description,
                Name = x.CharacterStats.CharacterIdentity.FullName!,
                CharacterState = x.CharacterStats,
                CharacterTracker = x.Tracker,
                DevelopmentTracker = x.DevelopmentTracker,
                CharacterId = x.CharacterId,
                SceneId = x.SceneId,
                SequenceNumber = x.SequenceNumber
            }).ToList();
    }
}