using System.Text.Json;

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

namespace FableCraft.Application.NarrativeEngine;

public class SceneGenerationOutput
{
    public required GeneratedScene GeneratedScene { get; set; }

    public required NarrativeDirectorOutput NarrativeDirectorOutput { get; set; }

    public required Tracker Tracker { get; set; }
}

internal sealed class SceneGenerationOrchestrator
{
    private readonly IKernelBuilder _kernelBuilder;
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger _logger;
    private readonly IRagSearch _ragSearch;
    private readonly WriterAgent _writerAgent;
    private readonly TrackerAgent _trackerAgent;
    private readonly LoreCrafter _loreCrafter;
    private readonly CharacterCrafter _characterCrafter;
    private readonly CharacterStateTracker _characterStateTracker;
    private readonly ContextGatherer _contextGatherer;
    private readonly NarrativeDirectorAgent _narrativeDirectorAgent;
    private const int NumberOfScenesToInclude = 20;

    public SceneGenerationOrchestrator(
        IKernelBuilder kernelBuilder,
        IRagSearch ragSearch,
        ILogger logger,
        ApplicationDbContext dbContext,
        NarrativeDirectorAgent narrativeDirectorAgent,
        WriterAgent writerAgent,
        TrackerAgent trackerAgent,
        CharacterCrafter characterCrafter,
        LoreCrafter loreCrafter,
        CharacterStateTracker characterStateTracker,
        ContextGatherer contextGatherer)
    {
        _kernelBuilder = kernelBuilder;
        _ragSearch = ragSearch;
        _logger = logger;
        _dbContext = dbContext;
        _narrativeDirectorAgent = narrativeDirectorAgent;
        _writerAgent = writerAgent;
        _trackerAgent = trackerAgent;
        _characterCrafter = characterCrafter;
        _loreCrafter = loreCrafter;
        _characterStateTracker = characterStateTracker;
        _contextGatherer = contextGatherer;
    }

    public async Task<SceneGenerationOutput> GenerateSceneAsync(Guid adventureId, string playerAction, CancellationToken cancellationToken)
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
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true
        };
        string currentTracker = string.Empty;
        if (scenes.LastOrDefault()?.Metadata?.Tracker != null)
        {
            currentTracker = $"""
                              <current_scene_tracker>
                              {JsonSerializer.Serialize(scenes.LastOrDefault()?.Metadata?.Tracker, options)}
                              </current_scene_tracker>
                              """;
        }

        var commonContext = summary
                            + $"""
                               <current_scene_number>
                               {scenes.LastOrDefault()?.SequenceNumber}
                               </current_scene_number>

                               {currentTracker}

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
        var contextBases = await _contextGatherer.Invoke(adventureId, commonContext, cancellationToken);
        commonContext += $"""
                          <additional_context>
                          {string.Join("\n\n", contextBases.Select(x => $"""
                                                                         QUESTION:
                                                                         {x.Query}
                                                                         ANSWER:
                                                                         {x.Response}
                                                                         """))}
                          </additional_context>
                          """;

        var charactersOnScene = await GetCharacters(scenes, cancellationToken);
        if (charactersOnScene.Any())
        {
            commonContext += $"""
                                 <characters_on_scene>
                                 {string.Join("\n\n", charactersOnScene.Select(x => $"""
                                                                                     <character>
                                                                                     {x.Name}
                                                                                     {x.Description}
                                                                                     {x.CharacterTracker}
                                                                                     </character>
                                                                                     """))}
                                 </characters_on_scene>
                              """;
        }

        Microsoft.SemanticKernel.IKernelBuilder kernel = _kernelBuilder.WithBase();
        var kgPlugin = new KnowledgeGraphPlugin(_ragSearch, adventureId.ToString(), _logger);
        kernel.Plugins.Add(KernelPluginFactory.CreateFromObject(kgPlugin));
        Kernel kernelWithKg = kernel.Build();

        var narrativeContext = new NarrativeContext
        {
            AdventureId = adventureId,
            SceneContext = scenes.Select(x => new SceneContext
                {
                    SceneContent = x.NarrativeText,
                    PlayerChoice = x.CharacterActions.Single(y => y.Selected)
                        .ActionDescription,
                    Metadata = x.Metadata
                })
                .ToArray(),
            StorySummary = adventure.Summary,
            PlayerAction = playerAction,
            CommonContext = commonContext,
            KernelKg = kernelWithKg,
            Characters = charactersOnScene,
        };

        var narrativeDirectorOutput = await _narrativeDirectorAgent.Invoke(narrativeContext, cancellationToken);
        var newLoreTask = narrativeDirectorOutput.CreationRequests.Lore.Select(x => _loreCrafter.Invoke(kernelWithKg, x, cancellationToken)).ToList();
        var newLore = await Task.WhenAll(newLoreTask);
        var characterCreationTasks = narrativeDirectorOutput.CreationRequests.Characters.Select(x => _characterCrafter.Invoke(
            kernelWithKg,
            narrativeContext,
            x,
            newLore,
            cancellationToken)).ToList();
        var characterCreations = await Task.WhenAll(characterCreationTasks);
        narrativeContext.Characters.AddRange(characterCreations);

        var sceneContent = await _writerAgent.Invoke(narrativeContext, characterCreations, newLore, narrativeDirectorOutput, cancellationToken);
        var trackerTask = _trackerAgent.Invoke(
            narrativeContext,
            sceneContent,
            cancellationToken);
        var characterUpdatesTask = charactersOnScene
            .ExceptBy(characterCreations.Select(x => x.Name), x => x.Name)
            .Select(x => _characterStateTracker.Invoke(adventureId, narrativeContext, x, sceneContent, cancellationToken))
            .ToList();

        var tracker = await trackerTask;
        var characterUpdates = await Task.WhenAll(characterUpdatesTask);

        var newScene = new Scene
        {
            AdventureId = adventureId,
            NarrativeText = sceneContent.Scene,
            CharacterActions = sceneContent.Choices.Select(x => new MainCharacterAction
                {
                    ActionDescription = x
                })
                .ToList(),
            SequenceNumber = scenes.LastOrDefault()?.SequenceNumber ?? 0 + 1,
            Metadata = new Metadata
            {
                NarrativeMetadata = narrativeDirectorOutput,
                Tracker = tracker
            },
            CreatedAt = DateTime.UtcNow,
        };

        var characters = await _dbContext.Characters
            .Where(x => characterUpdates.Select(y => y.Name).Contains(x.Name))
            .Include(character => character.CharacterStates)
            .ToListAsync(cancellationToken: cancellationToken);
        foreach (var updatedCharacter in characterUpdates)
        {
            var characterEntity = characters.Single(x => x.Name == updatedCharacter.Name);
            characterEntity.CharacterStates.Add(new CharacterState
            {
                CharacterStats = updatedCharacter.CharacterState,
                Tracker = updatedCharacter.CharacterTracker!,
                SequenceNumber = characterEntity.CharacterStates.Last().SequenceNumber + 1,
                SceneId = newScene.Id,
            });
        }

        var newCharacters = characterCreations.Select(x => new Character
        {
            Name = x.Name,
            Description = x.Description,
            AdventureId = adventureId,
            CharacterStates =
            [
                new CharacterState
                {
                    CharacterStats = x.CharacterState,
                    Tracker = x.CharacterTracker!,
                    SequenceNumber = 0,
                    SceneId = newScene.Id
                }
            ]
        }).ToList();

        var newLoreEntities = newLore.Select(x => new LorebookEntry
        {
            AdventureId = adventureId,
            Description = x.Summary,
            Category = x.Title,
            Content = JsonSerializer.Serialize(x, options),
            ContentType = ContentType.Json,
        }).ToList();

        IExecutionStrategy strategy = _dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using IDbContextTransaction transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                await _dbContext.Scenes.AddAsync(newScene, cancellationToken);
                _dbContext.Characters.AddRange(newCharacters);
                _dbContext.LorebookEntries.AddRange(newLoreEntities);
                _dbContext.Characters.UpdateRange(characters);
                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });

        return new SceneGenerationOutput
        {
            Tracker = tracker,
            GeneratedScene = sceneContent,
            NarrativeDirectorOutput = narrativeDirectorOutput
        };
    }

    private async Task<List<CharacterContext>> GetCharacters(List<Scene> scenes, CancellationToken cancellationToken)
    {
        if (scenes.Any())
        {
            // Shit query but not a problem for now
            var existingCharacters = await _dbContext
                .CharacterStates
                .Where(x => x.SceneId == scenes.Last().Id)
                .Include(x => x.Character)
                .GroupBy(x => x.CharacterId)
                .ToListAsync(cancellationToken);
            return existingCharacters
                .Select(g => g.OrderByDescending(x => x.SequenceNumber).First())
                .Select(x => new CharacterContext
                {
                    Description = x.Character.Description,
                    Name = x.CharacterStats.CharacterIdentity.FullName!,
                    CharacterState = x.CharacterStats,
                    CharacterTracker = x.Tracker,
                }).ToList();
        }

        return [];
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
                      {adventure.AdventureStartTime:o}
                      </adventure_start_time>
                      """;
        return await GenerateSceneAsync(adventureId, prompt, cancellationToken);
    }
}