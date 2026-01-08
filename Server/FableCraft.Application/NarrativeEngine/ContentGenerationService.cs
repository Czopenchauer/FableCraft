using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Application.NarrativeEngine.Workflow;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Serilog;

namespace FableCraft.Application.NarrativeEngine;

public class ContentGenerationResult
{
    public required Guid SceneId { get; init; }
    public required int SequenceNumber { get; init; }
    public required int NewCharactersCount { get; init; }
    public required int NewLocationsCount { get; init; }
    public required int NewLoreCount { get; init; }
    public required int NewItemsCount { get; init; }
    public required string[] NewCharacterNames { get; init; }
    public required string[] NewLocationNames { get; init; }
    public required string[] NewLoreTitles { get; init; }
    public required string[] NewItemNames { get; init; }
}

public sealed class ContentGenerationService(
    ILogger logger,
    ApplicationDbContext dbContext,
    IServiceProvider serviceProvider)
{
    private const int NumberOfScenesToInclude = 20;

    public async Task<ContentGenerationResult?> GenerateContentForAdventureAsync(
        Guid adventureId,
        CancellationToken cancellationToken)
    {
        var lastScene = await dbContext.Scenes
            .Where(x => x.AdventureId == adventureId)
            .Include(x => x.CharacterActions)
            .OrderByDescending(x => x.SequenceNumber)
            .FirstOrDefaultAsync(cancellationToken);

        if (lastScene is null)
        {
            logger.Warning("No scenes found for adventure {AdventureId}", adventureId);
            return null;
        }

        var context = await BuildContextForContentGeneration(adventureId, lastScene, cancellationToken);

        // Clear existing content fields to force regeneration
        context.NewCharacters = null;
        context.NewLocations = null;
        context.NewItems = null;

        var processors = serviceProvider.GetServices<IProcessor>();

        var sceneTrackerProcessor = processors.First(p => p is SceneTrackerProcessor);
        await sceneTrackerProcessor.Invoke(context, cancellationToken);

        var contentGenerator = processors.First(p => p is ContentGenerator);
        var characterTrackersProcessor = processors.First(p => p is CharacterTrackersProcessor);
        await Task.WhenAll(
            contentGenerator.Invoke(context, cancellationToken),
            characterTrackersProcessor.Invoke(context, cancellationToken));

        // Save the generated content and character updates to the database
        var saveProcessor = processors.First(p => p is SaveSceneEnrichment);
        await saveProcessor.Invoke(context, cancellationToken);

        logger.Information(
            "Content generated for adventure {AdventureId}, scene {SceneId}: {CharactersCount} characters, {LocationsCount} locations, {LoreCount} lore, {ItemsCount} items",
            adventureId,
            lastScene.Id,
            context.NewCharacters?.Length ?? 0,
            context.NewLocations?.Length ?? 0,
            context.NewLore?.Count ?? 0,
            context.NewItems?.Length ?? 0);

        return new ContentGenerationResult
        {
            SceneId = lastScene.Id,
            SequenceNumber = lastScene.SequenceNumber,
            NewCharactersCount = context.NewCharacters?.Length ?? 0,
            NewLocationsCount = context.NewLocations?.Length ?? 0,
            NewLoreCount = context.NewLore?.Count ?? 0,
            NewItemsCount = context.NewItems?.Length ?? 0,
            NewCharacterNames = context.NewCharacters?.Select(c => c.Name).ToArray() ?? [],
            NewLocationNames = context.NewLocations?.Select(l => l.Title).ToArray() ?? [],
            NewLoreTitles = context.NewLore?.Select(l => l.Title).ToArray() ?? [],
            NewItemNames = context.NewItems?.Select(i => i.Name).ToArray() ?? []
        };
    }

    private async Task<GenerationContext> BuildContextForContentGeneration(
        Guid adventureId,
        Scene lastScene,
        CancellationToken cancellationToken)
    {
        var adventure = await dbContext.Adventures
            .Where(x => x.Id == adventureId)
            .Include(x => x.AgentLlmPresets)
            .ThenInclude(x => x.LlmPreset)
            .Select(x => new
            {
                x.TrackerStructure,
                x.MainCharacter,
                x.AgentLlmPresets,
                PromptPaths = x.PromptPath,
                x.AdventureStartTime
            })
            .SingleAsync(cancellationToken);

        var scenes = await dbContext.Scenes
            .Where(x => x.AdventureId == adventureId)
            .Include(x => x.CharacterActions)
            .OrderByDescending(x => x.SequenceNumber)
            .Skip(1)
            .Take(NumberOfScenesToInclude)
            .ToListAsync(cancellationToken);

        var adventureCharacters = await GetCharacters(adventureId, cancellationToken);

        var createdLore = await dbContext.Scenes
            .AsSplitQuery()
            .Where(x => x.AdventureId == adventureId)
            .OrderByDescending(x => x.SequenceNumber)
            .Take(1)
            .Include(x => x.Lorebooks)
            .SelectMany(x => x.Lorebooks)
            .ToArrayAsync(cancellationToken);

        var context = new GenerationContext
        {
            AdventureId = adventureId,
            PlayerAction = lastScene.CharacterActions.FirstOrDefault(x => x.Selected)?.ActionDescription ?? string.Empty,
            GenerationProcessStep = GenerationProcessStep.SceneGenerated,
            NewSceneId = lastScene.Id,
            NewResolution = lastScene.Metadata.ResolutionOutput,
            NewScene = new GeneratedScene
            {
                Scene = lastScene.NarrativeText,
                Choices = lastScene.CharacterActions.Select(x => x.ActionDescription).ToArray()
            },
            NewTracker = lastScene.Metadata.Tracker
        };

        context.SetupRequiredFields(
            scenes.Select(SceneContext.CreateFromScene).ToArray(),
            adventure.TrackerStructure,
            adventure.MainCharacter,
            adventureCharacters,
            adventure.AgentLlmPresets.ToArray(),
            adventure.PromptPaths,
            adventure.AdventureStartTime,
            createdLore);

        return context;
    }

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
                    })
                    .ToList(),
                Relationships = x.CharacterRelationships
                    .GroupBy(r => r.TargetCharacterName)
                    .Select(g => g.OrderByDescending(r => r.SequenceNumber)
                        .First())
                    .Select(y => new CharacterRelationshipContext
                    {
                        TargetCharacterName = y!.TargetCharacterName,
                        Data = y.Data,
                        UpdateTime = y.UpdateTime,
                        SequenceNumber = y.SequenceNumber,
                        Dynamic = y.Dynamic!
                    })
                    .ToList(),
                SceneRewrites = x.CharacterSceneRewrites.Select(y => new CharacterSceneContext
                    {
                        Content = y.Content,
                        SceneTracker = y.SceneTracker,
                        SequenceNumber = y.SequenceNumber
                    })
                    .ToList(),
                Importance = x.Importance,
                SimulationMetadata = x.CharacterStates.Single().SimulationMetadata
            }).ToList();
    }
}
