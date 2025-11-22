using FableCraft.Application.NarrativeEngine.Agents;
using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Application.NarrativeEngine.Plugins;
using FableCraft.Infrastructure.Clients;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.SemanticKernel;

using Serilog;

using IKernelBuilder = FableCraft.Infrastructure.Llm.IKernelBuilder;
using SceneMetadata = FableCraft.Infrastructure.Persistence.Entities.SceneMetadata;

namespace FableCraft.Application.NarrativeEngine.Orchestration;

internal sealed class SceneGenerationOrchestrator
{
    private readonly IKernelBuilder _kernelBuilder;
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger _logger;
    private readonly IRagSearch _ragSearch;
    private const int NumberOfScenesToInclude = 30;

    public SceneGenerationOrchestrator(IKernelBuilder kernelBuilder, IRagSearch ragSearch, ILogger logger, ApplicationDbContext dbContext)
    {
        _kernelBuilder = kernelBuilder;
        _ragSearch = ragSearch;
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task<GeneratedScene> GenerateSceneAsync(Guid adventureId, string playerAction, CancellationToken cancellationToken)
    {
        var adventure = await _dbContext
            .Adventures
            .Where(x => x.Id == adventureId)
            .Select(x => new { x.TrackerStructure, x.Summary, x.MainCharacter })
            .SingleAsync(cancellationToken: cancellationToken);

        var scenes = await _dbContext
            .Scenes
            .Where(x => x.AdventureId == adventureId)
            .Include(x => x.CharacterActions)
            .OrderByDescending(x => x.SequenceNumber)
            .Take(NumberOfScenesToInclude)
            .ToListAsync(cancellationToken);

        var summary = string.IsNullOrEmpty(adventure.Summary)
            ? string.Empty
            : $"""
               <story_summary>
               {adventure.Summary}
               </story_summary>

               """;
        var commonContext = summary
                            + $"""
                               <current_scene_number>
                               {scenes.LastOrDefault()?.SequenceNumber}
                               </current_scene_number>

                               <current_scene_tracker>
                               {scenes.LastOrDefault()?.SceneMetadata?.Tracker}
                               </current_scene_tracker>

                               <main_character>
                               {adventure.MainCharacter.Name}
                               {adventure.MainCharacter.Description}
                               </main_character>

                               <last_scenes>
                               {string.Join("\n", scenes.Select(x =>
                                   $"""
                                    SCENE NUMBER: {x.SequenceNumber}
                                    {x.GetSceneWithSelectedAction()}
                                    """))}
                               </last_scenes>
                               """;

        var narrativeContext = new NarrativeContext
        {
            AdventureId = adventureId.ToString(),
            TrackerStructure = adventure.TrackerStructure,
            SceneContext = scenes.Select(x => new SceneContext
                {
                    SceneContent = x.NarrativeText,
                    PlayerChoice = x.CharacterActions.Single(y => y.Selected)
                        .ActionDescription,
                    SceneMetadata = x.SceneMetadata
                })
                .ToArray(),
            StorySummary = adventure.Summary,
            PlayerAction = playerAction,
            CommonContext = commonContext,
        };

        Microsoft.SemanticKernel.IKernelBuilder kernel = _kernelBuilder.WithBase();
        var kgPlugin = new KnowledgeGraphPlugin(_ragSearch, adventureId.ToString());
        kernel.Plugins.Add(KernelPluginFactory.CreateFromObject(kgPlugin));
        Kernel kernelWithKg = kernel.Build();
        var narrativeAgent = new NarrativeDirector(_logger);
        var narrativeDirectorOutput = await narrativeAgent.Invoke(kernelWithKg, narrativeContext);
        narrativeContext.Characters.AddRange();
        var writer = new Writer(_logger);
        var sceneContent = await writer.Invoke(kernelWithKg, narrativeContext, narrativeDirectorOutput, cancellationToken);

        var newScene = new Scene
        {
            AdventureId = adventureId,
            NarrativeText = sceneContent.Scene,
            CharacterActions = sceneContent.Choices.Select(x => new MainCharacterAction
                {
                    ActionDescription = x
                })
                .ToList(),
            SequenceNumber = scenes[^1].SequenceNumber + 1,
            SceneMetadata = new SceneMetadata()
            {
                NarrativeMetadata = narrativeDirectorOutput,
                Tracker = null
            },
            CreatedAt = default
        };

        IExecutionStrategy strategy = _dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using IDbContextTransaction transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                await _dbContext.Scenes.AddAsync(newScene, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });

        return sceneContent;
    }

    public async Task<GeneratedScene> GenerateInitialSceneAsync(Guid adventureId, CancellationToken cancellationToken)
    {
        var adventure = await _dbContext
            .Adventures
            .Select(x => new { x.Id, x.AuthorNotes, x.FirstSceneGuidance })
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
                      """;
        return await GenerateSceneAsync(adventureId, prompt, cancellationToken);
    }
}