using System.Text.Json;

using FableCraft.Application.NarrativeEngine.Agents;
using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Application.NarrativeEngine.Plugins;
using FableCraft.Application.NarrativeEngine.Workflow;
using FableCraft.Infrastructure.Clients;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities;
using FableCraft.Infrastructure.Queue;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.SemanticKernel;

using Serilog;

using IKernelBuilder = FableCraft.Infrastructure.Llm.IKernelBuilder;

namespace FableCraft.Application.NarrativeEngine;

public class SceneGenerationOutput
{
    public required GeneratedScene GeneratedScene { get; set; }

    public required NarrativeDirectorOutput NarrativeDirectorOutput { get; set; }

    public required Tracker Tracker { get; set; }
}

internal sealed class SceneGenerationOrchestrator
{
    private class WorkflowBuilder(IEnumerable<IProcessor> processors)
    {
        private readonly List<IProcessor> _workflow = new();

        public WorkflowBuilder AddProcessor<T>() where T : IProcessor
        {
            var processor = processors.OfType<T>().FirstOrDefault();
            ArgumentNullException.ThrowIfNull(processor);
            _workflow.Add(processor);
            return this;
        }

        public List<IProcessor> Build() => _workflow;
    }

    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger _logger;
    private readonly IEnumerable<IProcessor> _processors;
    private readonly IMessageDispatcher _messageDispatcher;
    private const int NumberOfScenesToInclude = 20;

    public SceneGenerationOrchestrator(
        ILogger logger,
        ApplicationDbContext dbContext,
        IMessageDispatcher messageDispatcher,
        IEnumerable<IProcessor> processors)
    {
        _logger = logger;
        _dbContext = dbContext;
        _messageDispatcher = messageDispatcher;
        _processors = processors;
    }

    public async Task<SceneGenerationOutput> GenerateSceneAsync(Guid adventureId, string playerAction, CancellationToken cancellationToken)
    {
        var context = await GetGenerationContext(adventureId, playerAction, cancellationToken);

        if (context.Context.GenerationProcessStep == GenerationProcessStep.Completed)
        {
            await _dbContext.GenerationProcesses
                .Where(x => x.Id == context.ProcessId)
                .ExecuteDeleteAsync(cancellationToken);

            return new SceneGenerationOutput
            {
                GeneratedScene = context.Context.NewScene!,
                NarrativeDirectorOutput = context.Context.NewNarrativeDirection!,
                Tracker = context.Context.NewTracker!
            };
        }

        var workflow = new WorkflowBuilder(_processors)
            .AddProcessor<ContextGatherer>()
            .AddProcessor<NarrativeDirectorAgent>()
            .AddProcessor<ContentGenerator>()
            .AddProcessor<WriterAgent>()
            .AddProcessor<TrackerProcessor>()
            .AddProcessor<SaveGeneration>()
            .Build();
        foreach (IProcessor processor in workflow)
        {
            try
            {
                await processor.Invoke(context.Context, cancellationToken);
                await _dbContext.GenerationProcesses
                    .ExecuteUpdateAsync(x => x.SetProperty(y => y.Context, JsonSerializer.Serialize(context.Context)),
                        cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                await _dbContext.GenerationProcesses
                    .ExecuteUpdateAsync(x => x.SetProperty(y => y.Context, JsonSerializer.Serialize(context.Context)),
                        cancellationToken: cancellationToken);
                _logger.Error(ex,
                    "Error during scene generation for adventure {AdventureId} at step {GenerationProcessStep}",
                    adventureId,
                    context.Context.GenerationProcessStep);
                throw;
            }
        }

        await _dbContext.GenerationProcesses
            .Where(x => x.Id == context.ProcessId)
            .ExecuteDeleteAsync(cancellationToken);

        return new SceneGenerationOutput
        {
            GeneratedScene = context.Context.NewScene!,
            NarrativeDirectorOutput = context.Context.NewNarrativeDirection!,
            Tracker = context.Context.NewTracker!
        };
    }

    private async Task<(GenerationContext Context, Guid ProcessId)> GetGenerationContext(Guid adventureId, string playerAction, CancellationToken cancellationToken)
    {
        var adventure = await _dbContext
            .Adventures
            .Where(x => x.Id == adventureId)
            .Select(x => new { x.TrackerStructure, x.MainCharacter })
            .SingleAsync(cancellationToken: cancellationToken);

        var scenes = await _dbContext
            .Scenes
            .Where(x => x.AdventureId == adventureId)
            .Include(x => x.CharacterActions)
            .OrderByDescending(x => x.SequenceNumber)
            .Take(NumberOfScenesToInclude)
            .ToListAsync(cancellationToken);
        var adventureCharacters = await GetCharacters(adventureId, cancellationToken);

        var generationProcess = await _dbContext.GenerationProcesses.Where(x => x.AdventureId == adventureId).FirstOrDefaultAsync(cancellationToken: cancellationToken);
        GenerationContext context;
        if (generationProcess != null)
        {
            context = JsonSerializer.Deserialize<GenerationContext>(generationProcess.Context)!;
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (context == null || context.PlayerAction != playerAction)
            {
                (generationProcess, context) = await CreateNewProcess();
            }
            else
            {
                context.SceneContext = SceneContext();
                context.Characters = adventureCharacters;
                context.TrackerStructure = adventure.TrackerStructure;
                context.MainCharacter = adventure.MainCharacter;
                context.Summary = scenes.MaxBy(x => x.SequenceNumber)?.AdventureSummary;
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
                Summary = scenes.MaxBy(x => x.SequenceNumber)?.AdventureSummary
            };
            var process = new GenerationProcess()
            {
                AdventureId = adventureId,
                Context = JsonSerializer.Serialize(newContext),
            };
            await _dbContext.GenerationProcesses.AddAsync(new GenerationProcess
                {
                    AdventureId = adventureId,
                    Context = JsonSerializer.Serialize(newContext),
                },
                cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return (process, newContext);
        }

        SceneContext[] SceneContext()
        {
            return scenes.Select(x => new SceneContext
                {
                    SceneContent = x.NarrativeText,
                    PlayerChoice = x.CharacterActions.Single(y => y.Selected)
                        .ActionDescription,
                    Metadata = x.Metadata,
                    Characters = x.CharacterStates.Select(y => new CharacterContext
                    {
                        CharacterState = y.CharacterStats,
                        CharacterTracker = y.Tracker,
                        Description = y.Description,
                        Name = y.CharacterStats.CharacterIdentity.FullName!,
                        CharacterId = y.CharacterId
                    }),
                    SequenceNumber = x.SequenceNumber
                })
                .ToArray();
        }
    }

    /// <summary>
    /// Gets the latest character contexts for all characters in the adventure.
    /// </summary>
    /// <param name="adventureId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task<List<CharacterContext>> GetCharacters(Guid adventureId, CancellationToken cancellationToken)
    {
        var existingCharacters = await _dbContext
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
                SceneId = x.SceneId
            }).ToList();
    }

    public async Task<SceneGenerationOutput> GenerateInitialSceneAsync(Guid adventureId, CancellationToken cancellationToken)
    {
        var adventure = await _dbContext
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