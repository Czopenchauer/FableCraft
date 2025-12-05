using System.Text.Json;

using FableCraft.Application.NarrativeEngine.Agents;
using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Application.NarrativeEngine.Workflow;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;

using Microsoft.EntityFrameworkCore;

using Serilog;

namespace FableCraft.Application.NarrativeEngine;

public class SceneGenerationOutput
{
    public required Guid SceneId { get; set; }

    public required GeneratedScene GeneratedScene { get; init; }

    public required NarrativeDirectorOutput NarrativeDirectorOutput { get; init; }

    public required Tracker Tracker { get; init; }
}

internal sealed class SceneGenerationOrchestrator(
    ILogger logger,
    ApplicationDbContext dbContext,
    IEnumerable<IProcessor> processors)
{
    private const int NumberOfScenesToInclude = 20;

    public async Task<SceneGenerationOutput> GenerateSceneAsync(Guid adventureId, string playerAction, CancellationToken cancellationToken)
    {
        var context = await GetGenerationContext(adventureId, playerAction, cancellationToken);

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
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        foreach (IProcessor processor in workflow)
        {
            try
            {
                await processor.Invoke(context.Context, cancellationToken);
                await dbContext.GenerationProcesses
                    .Where(x => x.Id == context.ProcessId)
                    .ExecuteUpdateAsync(x => x.SetProperty(y => y.Context, JsonSerializer.Serialize(context.Context, options)),
                        cancellationToken: cancellationToken);
                logger.Information("[Generation] Step {GenerationProcessStep} took {ElapsedMilliseconds} ms",
                    context.Context.GenerationProcessStep,
                    stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                await dbContext.GenerationProcesses
                    .Where(x => x.Id == context.ProcessId)
                    .ExecuteUpdateAsync(x => x.SetProperty(y => y.Context, JsonSerializer.Serialize(context.Context, options)),
                        cancellationToken: cancellationToken);
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

            var newScene = await dbContext.Scenes
                .Include(x => x.CharacterActions)
                .Where(x => x.AdventureId == adventureId)
                .OrderByDescending(x => x.SequenceNumber).FirstAsync(cancellationToken);
            return new SceneGenerationOutput
            {
                SceneId = newScene.Id,
                GeneratedScene = new GeneratedScene
                {
                    Scene = newScene.NarrativeText,
                    Choices = newScene.CharacterActions.Select(x => x.ActionDescription).ToArray()
                },
                NarrativeDirectorOutput = context.Context.NewNarrativeDirection!,
                Tracker = context.Context.NewTracker!
            };
        }
    }

    private async Task<(GenerationContext Context, Guid ProcessId)> GetGenerationContext(Guid adventureId, string playerAction, CancellationToken cancellationToken)
    {
        var adventure = await dbContext
            .Adventures
            .Where(x => x.Id == adventureId)
            .Select(x => new { x.TrackerStructure, x.MainCharacter })
            .SingleAsync(cancellationToken: cancellationToken);

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
        var generationProcess = await dbContext.GenerationProcesses.Where(x => x.AdventureId == adventureId).FirstOrDefaultAsync(cancellationToken: cancellationToken);
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
                Summary = scenes.Where(x => !string.IsNullOrEmpty(x.AdventureSummary)).OrderByDescending(x => x.SequenceNumber).FirstOrDefault()?.AdventureSummary
            };
            var process = new GenerationProcess
            {
                AdventureId = adventureId,
                Context = JsonSerializer.Serialize(newContext, options),
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
                        .ActionDescription ?? playerAction,
                    Metadata = x.Metadata,
                    Characters = x.CharacterStates.Select(y => new CharacterContext
                    {
                        CharacterState = y.CharacterStats,
                        CharacterTracker = y.Tracker,
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

    /// <summary>
    /// Gets the latest character contexts for all characters in the adventure.
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
                CharacterId = x.CharacterId,
                SceneId = x.SceneId,
                SequenceNumber = x.SequenceNumber
            }).ToList();
    }

    public async Task<SceneGenerationOutput> GenerateInitialSceneAsync(Guid adventureId, CancellationToken cancellationToken)
    {
        var adventure = await dbContext
            .Adventures
            .Select(x => new { x.Id, x.AuthorNotes, x.FirstSceneGuidance, x.AdventureStartTime })
            .SingleAsync(x => x.Id == adventureId, cancellationToken: cancellationToken);
        var prompt = $"""
                      Generate adventure's opening scene based on the following context.
                      Guide about story style and tone:
                      <adventure_author_notes>
                      {adventure.AuthorNotes}
                      </adventure_author_notes>

                      Guide about the first scene specifics. How should it start, what mood to set, important elements to include:
                      <first_scene_guidance>
                      {adventure.FirstSceneGuidance}
                      </first_scene_guidance>

                      Start time of the adventure:
                      <adventure_start_time>
                      {adventure.AdventureStartTime}
                      </adventure_start_time>
                      """;
        return await GenerateSceneAsync(adventureId, prompt, cancellationToken);
    }
}