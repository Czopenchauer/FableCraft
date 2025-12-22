using FableCraft.Application.NarrativeEngine.Agents;
using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Application.NarrativeEngine.Workflow;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Serilog;

namespace FableCraft.Application.NarrativeEngine;

public class GatheredContextResult
{
    public required Guid SceneId { get; init; }
    public required int SequenceNumber { get; init; }
    public required string CurrentSituation { get; init; }
    public required string ContextContinuity { get; init; }
    public required string[] KeyElementsInPlay { get; init; }
    public required string[] PrimaryFocusAreas { get; init; }
    public required int WorldContextCount { get; init; }
    public required int NarrativeContextCount { get; init; }
    public required int DroppedContextCount { get; init; }
}

public sealed class ContextGatheringService(
    ILogger logger,
    ApplicationDbContext dbContext,
    IServiceProvider serviceProvider)
{
    private const int NumberOfScenesToInclude = 20;

    public async Task<GatheredContextResult?> GatherContextForAdventureAsync(
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

        var context = await BuildContextForGathering(adventureId, lastScene, cancellationToken);

        // Clear any existing gathered context to force regeneration
        context.ContextGathered = null;

        var processors = serviceProvider.GetServices<IProcessor>();
        var contextGatherer = processors.First(p => p is ContextGatherer);
        await contextGatherer.Invoke(context, cancellationToken);

        if (context.ContextGathered != null)
        {
            lastScene.Metadata.GatheredContext = new GatheredContext
            {
                AnalysisSummary = new GatheredContextAnalysis
                {
                    CurrentSituation = context.ContextGathered.AnalysisSummary.CurrentSituation,
                    KeyElementsInPlay = context.ContextGathered.AnalysisSummary.KeyElementsInPlay,
                    PrimaryFocusAreas = context.ContextGathered.AnalysisSummary.PrimaryFocusAreas,
                    ContextContinuity = context.ContextGathered.AnalysisSummary.ContextContinuity
                },
                WorldContext = context.ContextGathered.WorldContext.Select(x => new GatheredContextItem
                {
                    Topic = x.Topic,
                    Content = x.Content
                }).ToArray(),
                NarrativeContext = context.ContextGathered.NarrativeContext.Select(x => new GatheredContextItem
                {
                    Topic = x.Topic,
                    Content = x.Content
                }).ToArray(),
                DroppedContext = context.ContextGathered.DroppedContext.Select(x => new GatheredDroppedContext
                {
                    Topic = x.Topic,
                    Reason = x.Reason
                }).ToArray()
            };

            dbContext.Scenes.Update(lastScene);
            await dbContext.SaveChangesAsync(cancellationToken);

            logger.Information(
                "Context gathered for adventure {AdventureId}, scene {SceneId}: {WorldCount} world items, {NarrativeCount} narrative items",
                adventureId,
                lastScene.Id,
                context.ContextGathered.WorldContext.Length,
                context.ContextGathered.NarrativeContext.Length);

            return new GatheredContextResult
            {
                SceneId = lastScene.Id,
                SequenceNumber = lastScene.SequenceNumber,
                CurrentSituation = context.ContextGathered.AnalysisSummary.CurrentSituation,
                ContextContinuity = context.ContextGathered.AnalysisSummary.ContextContinuity,
                KeyElementsInPlay = context.ContextGathered.AnalysisSummary.KeyElementsInPlay,
                PrimaryFocusAreas = context.ContextGathered.AnalysisSummary.PrimaryFocusAreas,
                WorldContextCount = context.ContextGathered.WorldContext.Length,
                NarrativeContextCount = context.ContextGathered.NarrativeContext.Length,
                DroppedContextCount = context.ContextGathered.DroppedContext.Length
            };
        }

        logger.Warning("Context gathering returned no results for adventure {AdventureId}", adventureId);
        return null;
    }

    private async Task<GenerationContext> BuildContextForGathering(
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
                x.AdventureStartTime,
                x.WorldSettings,
                x.AuthorNotes
            })
            .SingleAsync(cancellationToken);

        var scenes = await dbContext.Scenes
            .Where(x => x.AdventureId == adventureId)
            .Include(x => x.CharacterActions)
            .OrderByDescending(x => x.SequenceNumber)
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
            NewNarrativeDirection = lastScene.Metadata.NarrativeMetadata,
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
            adventure.WorldSettings,
            adventure.AuthorNotes,
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
                Description = x.CharacterStates.Single().Description,
                Name = x.Name,
                CharacterState = x.CharacterStates.Single().CharacterStats,
                CharacterTracker = x.CharacterStates.Single().Tracker,
                CharacterId = x.Id,
                CharacterMemories = x.CharacterMemories.Select(y => new MemoryContext
                {
                    MemoryContent = y.Summary,
                    Salience = y.Salience,
                    Data = y.Data,
                    StoryTracker = y.StoryTracker
                }).ToList(),
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
                    StoryTracker = y.StoryTracker,
                    SequenceNumber = y.SequenceNumber
                }).ToList(),
            }).ToList();
    }
}
