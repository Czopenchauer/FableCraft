using System.Diagnostics;
using System.Text.Json;

using FableCraft.Application.NarrativeEngine.Agents;
using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Application.NarrativeEngine.Workflow;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;
using FableCraft.Infrastructure.Queue;

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

    public static SceneGenerationOutput CreateFromScene(Scene scene, MainCharacter mainCharacter)
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
                        Tracker = scene.Metadata.Tracker!.MainCharacter!,
                        Development = scene.Metadata.Tracker!.MainCharacterDevelopment!,
                        Description = scene.Metadata.MainCharacterDescription ?? mainCharacter.Description
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

    public required string[] CharactersOnScene { get; set; }

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
    IEnumerable<IProcessor> processors,
    IMessageDispatcher messageDispatcher)
{
    private const int NumberOfScenesToInclude = 20;

    public async Task<SceneGenerationOutput> GenerateFullSceneAsync(Guid adventureId, string playerAction, CancellationToken cancellationToken)
    {
        (GenerationContext Context, Guid ProcessId) context = await GetGenerationContext(adventureId, playerAction, cancellationToken);

        if (context.Context.GenerationProcessStep == GenerationProcessStep.Completed)
        {
            return await GetGeneratedScene();
        }

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true
        };
        var workflow = BuildWorkflow(processors, context.Context.GenerationProcessStep);
        var stopwatch = Stopwatch.StartNew();
        foreach (IProcessor processor in workflow)
        {
            try
            {
                await processor.Invoke(context.Context, cancellationToken);
                await dbContext.GenerationProcesses
                    .Where(x => x.Id == context.ProcessId)
                    .ExecuteUpdateAsync(x => x.SetProperty(y => y.Context, JsonSerializer.Serialize(context.Context, options)),
                        cancellationToken);
                logger.Information("[Generation] Step {GenerationProcessStep} took {ElapsedMilliseconds} ms",
                    context.Context.GenerationProcessStep,
                    stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                await dbContext.GenerationProcesses
                    .Where(x => x.Id == context.ProcessId)
                    .ExecuteUpdateAsync(x => x.SetProperty(y => y.Context, JsonSerializer.Serialize(context.Context, options)),
                        cancellationToken);
                logger.Error(ex,
                    "Error during scene generation for adventure {AdventureId} at step {GenerationProcessStep}",
                    adventureId,
                    context.Context.GenerationProcessStep);
                throw;
            }
        }

        return await GetGeneratedScene();

        async Task<SceneGenerationOutput> GetGeneratedScene()
        {
            await dbContext.GenerationProcesses
                .Where(x => x.Id == context.ProcessId)
                .ExecuteDeleteAsync(cancellationToken);

            Scene scene = await dbContext.Scenes
                .Include(x => x.CharacterActions)
                .Include(x => x.CharacterStates)
                .Include(x => x.Lorebooks)
                .Where(x => x.AdventureId == adventureId)
                .OrderByDescending(x => x.SequenceNumber).FirstAsync(cancellationToken);
            return SceneGenerationOutput.CreateFromScene(scene, context.Context.MainCharacter);
        }
    }

    public async Task<Scene> GenerateSceneAsync(
        Guid adventureId,
        string playerAction,
        CancellationToken cancellationToken)
    {
        (GenerationContext Context, Guid ProcessId) context = await GetGenerationContext(adventureId, playerAction, cancellationToken);

        if (context.Context.GenerationProcessStep == GenerationProcessStep.SceneSavedAwaitingEnrichment)
        {
            return await GetGeneratedSceneWithoutEnrichment();
        }

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true
        };
        var workflow = BuildWorkflowForInitialGeneration(processors, context.Context.GenerationProcessStep);
        var stopwatch = Stopwatch.StartNew();
        foreach (IProcessor processor in workflow)
        {
            try
            {
                stopwatch.Restart();
                await processor.Invoke(context.Context, cancellationToken);
                await dbContext.GenerationProcesses
                    .Where(x => x.Id == context.ProcessId)
                    .ExecuteUpdateAsync(x => x.SetProperty(y => y.Context, JsonSerializer.Serialize(context.Context, options)),
                        cancellationToken);
                logger.Information("[Generation] Step {GenerationProcessStep} took {ElapsedMilliseconds} ms",
                    context.Context.GenerationProcessStep,
                    stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                await dbContext.GenerationProcesses
                    .Where(x => x.Id == context.ProcessId)
                    .ExecuteUpdateAsync(x => x.SetProperty(y => y.Context, JsonSerializer.Serialize(context.Context, options)),
                        cancellationToken);
                logger.Error(ex,
                    "Error during scene generation for adventure {AdventureId} at step {GenerationProcessStep}",
                    adventureId,
                    context.Context.GenerationProcessStep);
                throw;
            }
        }

        return await GetGeneratedSceneWithoutEnrichment();

        async Task<Scene> GetGeneratedSceneWithoutEnrichment()
        {
            await dbContext.GenerationProcesses
                .Where(x => x.Id == context.ProcessId)
                .ExecuteDeleteAsync(cancellationToken);

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
            .Include(s => s.CharacterActions)
            .Where(s => s.Id == sceneId && s.AdventureId == adventureId)
            .FirstOrDefaultAsync(cancellationToken);

        if (scene == null)
        {
            throw new InvalidOperationException($"Scene {sceneId} not found for adventure {adventureId}");
        }

        if (scene.EnrichmentStatus == EnrichmentStatus.Enriched)
        {
            throw new InvalidOperationException("Scene is already enriched");
        }

        await dbContext.Scenes
            .Where(s => s.Id == sceneId)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.EnrichmentStatus, EnrichmentStatus.Enriching),
                cancellationToken);

        try
        {
            GenerationContext context = await BuildEnrichmentContext(adventureId, scene, cancellationToken);

            var workflow = BuildEnrichmentWorkflow(processors);

            foreach (IProcessor processor in workflow)
            {
                await processor.Invoke(context, cancellationToken);
            }

            await UpdateSceneWithEnrichment(scene, context, cancellationToken);

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
        catch (Exception ex)
        {
            await dbContext.Scenes
                .Where(s => s.Id == sceneId)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.EnrichmentStatus, EnrichmentStatus.EnrichmentFailed),
                    cancellationToken);
            logger.Error(ex, "Failed to enrich scene {SceneId} for adventure {AdventureId}", sceneId, adventureId);
            throw;
        }
    }

    private async Task<GenerationContext> BuildEnrichmentContext(
        Guid adventureId,
        Scene scene,
        CancellationToken cancellationToken)
    {
        var adventure = await dbContext.Adventures
            .Where(x => x.Id == adventureId)
            .Select(x => new { x.TrackerStructure, x.MainCharacter, x.FastPreset, x.ComplexPreset })
            .SingleAsync(cancellationToken);

        var scenes = await dbContext.Scenes
            .Where(x => x.AdventureId == adventureId)
            .Include(x => x.CharacterActions)
            .OrderByDescending(x => x.SequenceNumber)
            .Take(NumberOfScenesToInclude)
            .ToListAsync(cancellationToken);

        var characters = await GetCharacters(adventureId, cancellationToken);

        return new GenerationContext
        {
            AdventureId = adventureId,
            PlayerAction = scene.CharacterActions.FirstOrDefault(a => a.Selected)?.ActionDescription ?? "",
            LlmPreset = adventure.FastPreset
                        ?? throw new InvalidOperationException("No LLM preset configured for this adventure."),
            ComplexPreset = adventure.ComplexPreset
                            ?? throw new InvalidOperationException("No complex LLM preset configured for this adventure."),
            SceneContext = scenes.Select(x => new SceneContext
            {
                SceneContent = x.NarrativeText,
                PlayerChoice = x.CharacterActions.FirstOrDefault(y => y.Selected)?.ActionDescription ?? "",
                Metadata = x.Metadata,
                Characters = x.CharacterStates.Select(y => new CharacterContext
                {
                    CharacterState = y.CharacterStats,
                    CharacterTracker = y.Tracker,
                    DevelopmentTracker = y.DevelopmentTracker,
                    Description = y.Description,
                    Name = y.CharacterStats.CharacterIdentity.FullName!,
                    CharacterId = y.CharacterId,
                    SequenceNumber = x.SequenceNumber
                }),
                SequenceNumber = x.SequenceNumber
            }).ToArray(),
            Characters = characters,
            TrackerStructure = adventure.TrackerStructure,
            MainCharacter = adventure.MainCharacter,
            Summary = scenes.Where(x => !string.IsNullOrEmpty(x.AdventureSummary))
                .OrderByDescending(x => x.SequenceNumber)
                .FirstOrDefault()?.AdventureSummary,
            NewNarrativeDirection = scene.Metadata.NarrativeMetadata,
            NewScene = new GeneratedScene
            {
                Scene = scene.NarrativeText,
                Choices = scene.CharacterActions.Select(a => a.ActionDescription).ToArray()
            },
            GenerationProcessStep = GenerationProcessStep.EnrichmentStarted
        };
    }

    private static List<IProcessor> BuildEnrichmentWorkflow(IEnumerable<IProcessor> processors)
    {
        return new[]
        {
            processors.First(p => p is ContentGenerator),
            processors.First(p => p is TrackerProcessor)
        }.ToList();
    }

    private async Task UpdateSceneWithEnrichment(
        Scene scene,
        GenerationContext context,
        CancellationToken cancellationToken)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true
        };

        scene.Metadata.Tracker = context.NewTracker!;
        scene.Metadata.MainCharacterDescription = context.NewMainCharacterDescription;
        scene.EnrichmentStatus = EnrichmentStatus.Enriched;

        var newCharacterEntities = context.NewCharacters?.Select(x => new Character
                                   {
                                       AdventureId = context.AdventureId,
                                       CharacterId = x.CharacterId,
                                       Description = x.Description,
                                       CharacterStats = x.CharacterState,
                                       Tracker = x.CharacterTracker!,
                                       DevelopmentTracker = x.DevelopmentTracker!,
                                       SequenceNumber = scene.SequenceNumber,
                                       SceneId = scene.Id
                                   }).ToList()
                                   ?? new List<Character>();

        var updatedCharacterEntities = context.CharacterUpdates?.Select(x => new Character
                                       {
                                           AdventureId = context.AdventureId,
                                           CharacterId = x.CharacterId,
                                           Description = x.Description,
                                           CharacterStats = x.CharacterState,
                                           Tracker = x.CharacterTracker!,
                                           DevelopmentTracker = x.DevelopmentTracker!,
                                           SequenceNumber = scene.SequenceNumber,
                                           SceneId = scene.Id
                                       }).ToList()
                                       ?? new List<Character>();

        newCharacterEntities.AddRange(updatedCharacterEntities);
        scene.CharacterStates = newCharacterEntities;

        var loreEntities = context.NewLore?.Select(x => new LorebookEntry
                           {
                               AdventureId = context.AdventureId,
                               Description = x.Summary,
                               Category = x.Title,
                               Content = JsonSerializer.Serialize(x, options),
                               ContentType = ContentType.json
                           }).ToList()
                           ?? new List<LorebookEntry>();

        var locationEntities = context.NewLocations?.Select(x => new LorebookEntry
                               {
                                   AdventureId = context.AdventureId,
                                   Description = x.NarrativeData.ShortDescription,
                                   Content = JsonSerializer.Serialize(x, options),
                                   Category = x.EntityData.Name,
                                   ContentType = ContentType.json
                               }).ToList()
                               ?? new List<LorebookEntry>();
        
        var itemsEntities = context.NewItems?.Select(x => new LorebookEntry
                               {
                                   AdventureId = context.AdventureId,
                                   Description = x.NarrativeData.ShortDescription,
                                   Content = JsonSerializer.Serialize(x, options),
                                   Category = x.EntityData.Name,
                                   ContentType = ContentType.json
                               }).ToList()
                               ?? new List<LorebookEntry>();

        loreEntities.AddRange(locationEntities);
        loreEntities.AddRange(itemsEntities);

        foreach (LorebookEntry loreEntry in loreEntities)
        {
            scene.Lorebooks.Add(loreEntry);
        }

        dbContext.Scenes.Update(scene);
        await dbContext.SaveChangesAsync(cancellationToken);
        await messageDispatcher.PublishAsync(new SceneGeneratedEvent
            {
                AdventureId = scene.AdventureId,
                SceneId = scene.Id
            },
            cancellationToken);
    }

    private async Task<(GenerationContext Context, Guid ProcessId)> GetGenerationContext(Guid adventureId, string playerAction, CancellationToken cancellationToken)
    {
        var adventure = await dbContext
            .Adventures
            .Where(x => x.Id == adventureId)
            .Select(x => new
            {
                x.TrackerStructure, x.MainCharacter, x.FastPreset,
                Complex = x.ComplexPreset
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
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true
        };
        GenerationProcess? generationProcess = await dbContext.GenerationProcesses.Where(x => x.AdventureId == adventureId).FirstOrDefaultAsync(cancellationToken);
        LlmPreset llmPreset = adventure.FastPreset
                              ?? throw new InvalidOperationException("No LLM preset configured for this adventure.");
        LlmPreset complexPreset = adventure.Complex
                                  ?? throw new InvalidOperationException("No LLM preset configured for this adventure.");
        GenerationContext context;
        if (generationProcess != null)
        {
            context = JsonSerializer.Deserialize<GenerationContext>(generationProcess.Context, options)!;
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (context == null || context.PlayerAction != playerAction)
            {
                await dbContext.GenerationProcesses
                    .Where(x => x.AdventureId == adventureId)
                    .ExecuteDeleteAsync(cancellationToken);
                (generationProcess, context) = await CreateNewProcess();
            }
            else
            {
                context.SceneContext = SceneContext();
                context.Characters = adventureCharacters;
                context.TrackerStructure = adventure.TrackerStructure;
                context.MainCharacter = adventure.MainCharacter;
                context.Summary = scenes.Where(x => !string.IsNullOrEmpty(x.AdventureSummary)).OrderByDescending(x => x.SequenceNumber).FirstOrDefault()?.AdventureSummary;
                context.LlmPreset = llmPreset;
                context.ComplexPreset = complexPreset;
            }
        }
        else
        {
            (generationProcess, context) = await CreateNewProcess();
        }

        if (context == null)
        {
            throw new InvalidOperationException("Failed to deserialize generation context from the database.");
        }

        return (context, generationProcess.Id);

        async Task<(GenerationProcess process, GenerationContext context)> CreateNewProcess()
        {
            var newContext = new GenerationContext
            {
                AdventureId = adventureId,
                SceneContext = SceneContext(),
                PlayerAction = playerAction,
                GenerationProcessStep = GenerationProcessStep.NotStarted,
                TrackerStructure = adventure.TrackerStructure,
                MainCharacter = adventure.MainCharacter,
                Characters = adventureCharacters,
                Summary = scenes.Where(x => !string.IsNullOrEmpty(x.AdventureSummary)).OrderByDescending(x => x.SequenceNumber).FirstOrDefault()?.AdventureSummary,
                LlmPreset = llmPreset,
                ComplexPreset = complexPreset
            };
            var process = new GenerationProcess
            {
                AdventureId = adventureId,
                Context = JsonSerializer.Serialize(newContext, options)
            };
            await dbContext.GenerationProcesses.AddAsync(process,
                cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            return (process, newContext);
        }

        SceneContext[] SceneContext()
        {
            return scenes.Select(x => new SceneContext
                {
                    SceneContent = x.NarrativeText,
                    PlayerChoice = x.CharacterActions.FirstOrDefault(y => y.Selected)?
                                       .ActionDescription
                                   ?? playerAction,
                    Metadata = x.Metadata,
                    Characters = x.CharacterStates.Select(y => new CharacterContext
                    {
                        CharacterState = y.CharacterStats,
                        CharacterTracker = y.Tracker,
                        DevelopmentTracker = y.DevelopmentTracker,
                        Description = y.Description,
                        Name = y.CharacterStats.CharacterIdentity.FullName!,
                        CharacterId = y.CharacterId,
                        SequenceNumber = x.SequenceNumber
                    }),
                    SequenceNumber = x.SequenceNumber
                })
                .ToArray();
        }
    }

    private static List<IProcessor> BuildWorkflow(IEnumerable<IProcessor> processors, GenerationProcessStep currentStep)
    {
        var fullWorkflow = new (GenerationProcessStep step, Type processorType)[]
        {
            (GenerationProcessStep.NotStarted, typeof(ContextGatherer)),
            (GenerationProcessStep.ContextGatheringFinished, typeof(NarrativeDirectorAgent)),
            (GenerationProcessStep.NarrativeDirectionFinished, typeof(ContentGenerator)),
            (GenerationProcessStep.ContentCreationFinished, typeof(WriterAgent)),
            (GenerationProcessStep.SceneGenerationFinished, typeof(TrackerProcessor)),
            (GenerationProcessStep.TrackerFinished, typeof(SaveGeneration))
        };

        return fullWorkflow
            .Where(w => w.step >= currentStep)
            .Select(w => processors.First(p => p.GetType() == w.processorType))
            .ToList();
    }

    private static List<IProcessor> BuildWorkflowForInitialGeneration(IEnumerable<IProcessor> processors, GenerationProcessStep currentStep)
    {
        var workflow = new (GenerationProcessStep step, Type processorType)[]
        {
            (GenerationProcessStep.NotStarted, typeof(ContextGatherer)),
            (GenerationProcessStep.ContextGatheringFinished, typeof(NarrativeDirectorAgent)),
            (GenerationProcessStep.NarrativeDirectionFinished, typeof(WriterAgent)),
            (GenerationProcessStep.SceneGenerationFinished, typeof(SaveSceneWithoutEnrichment))
        };

        return workflow
            .Where(w => w.step >= currentStep)
            .Select(w => processors.First(p => p.GetType() == w.processorType))
            .ToList();
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