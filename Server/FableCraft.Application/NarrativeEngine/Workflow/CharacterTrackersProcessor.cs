using FableCraft.Application.NarrativeEngine.Agents;
using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;

using Serilog;

namespace FableCraft.Application.NarrativeEngine.Workflow;

/// <summary>
///     Processor responsible for character tracking: main character tracker, character reflection,
///     character tracker updates, and chronicler. Depends on SceneTrackerProcessor having run first.
/// </summary>
internal sealed class CharacterTrackersProcessor(
    MainCharacterTrackerAgent mainCharacterTrackerAgent,
    ExperientialNarratorAgent experientialNarratorAgent,
    ClinicalAssessorAgent clinicalAssessorAgent,
    CharacterTrackerAgent characterTrackerAgent,
    CharacterContextGatherer characterContextGatherer,
    InitMainCharacterTrackerAgent initMainCharacterTrackerAgent,
    ChroniclerAgent chroniclerAgent,
    WorldInfoExtractorAgent worldInfoExtractorAgent,
    StorySummaryAgent storySummaryAgent,
    ILogger logger) : IProcessor
{
    public async Task Invoke(GenerationContext context, CancellationToken cancellationToken)
    {
        var storyTrackerResult = context.NewTracker?.Scene
                                 ?? throw new InvalidOperationException(
                                     "CharacterTrackersProcessor requires context.NewTracker.Scene to be populated. "
                                     + "Ensure SceneTrackerProcessor runs before this processor.");

        var chroniclerTask = Task.Run(() => ProcessChronicler(context, storyTrackerResult, cancellationToken), cancellationToken);
        var mainNarrativeExtractionTask = context.SkipWorldInfoExtractor
            ? Task.CompletedTask
            : Task.Run(() => ExtractWorldInfoFromMainNarrative(context, storyTrackerResult, cancellationToken), cancellationToken);

        var mainCharTrackerTask = context.NewTracker?.MainCharacter?.MainCharacter != null
            ? Task.FromResult(context.NewTracker.MainCharacter)
            : Task.Run(() => ProcessMainChar(context, storyTrackerResult, cancellationToken), cancellationToken);

        HashSet<string> alreadyProcessedCharacters;
        lock (context)
        {
            alreadyProcessedCharacters = (context.CharacterUpdates?.Select(x => x.Name) ?? [])
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        logger.Information("CharactersPresent from SceneTracker: [{CharactersPresent}]", string.Join(", ", storyTrackerResult.CharactersPresent));
        logger.Information("Known characters in context: [{KnownCharacters}]", string.Join(", ", context.Characters.Select(c => c.Name)));
        logger.Information("New characters in context: [{NewCharacters}]", string.Join(", ", context.NewCharacters?.Select(c => c.Name) ?? []));

        var allCharacters = context.Characters
            .Concat(context.NewCharacters ?? [])
            .ToList();

        Task<CharacterContext?>[] characterUpdateTask = [];
        if (allCharacters.Count != 0)
        {
            characterUpdateTask = allCharacters
                .Where(x => storyTrackerResult.CharactersPresent.Any(cp =>
                    string.Equals(cp, x.Name, StringComparison.OrdinalIgnoreCase)))
                .Select(async character =>
                {
                    // Skip if this character was already processed in a previous attempt
                    if (alreadyProcessedCharacters.Contains(character.Name))
                    {
                        if (context.ForceCharacterContextGathering && context.CharacterUpdates is not null)
                        {
                            var existingCharacterContext = context.CharacterUpdates
                                .FirstOrDefault(x => string.Equals(x.Name, character.Name, StringComparison.OrdinalIgnoreCase));
                            if (existingCharacterContext != null)
                            {
                                logger.Information("Character {Character} already processed. Running context gathering only (forced)", character.Name);
                                await GatherAndStoreCharacterContext(context, existingCharacterContext, cancellationToken);
                            }
                        }
                        else
                        {
                            logger.Information("Character {Character} already processed. Skipping tracking", character.Name);
                        }

                        return null;
                    }

                    CharacterContext characterContext;

                    string sceneRewrite;
                    bool isDead;

                    if (context.PendingReflectionCache.TryGetValue(character.CharacterId, out var cachedExperiential)
                        && cachedExperiential.Source == ReflectionSource.ExperientialNarrator)
                    {
                        logger.Information("Using cached experiential narrator for {Character}", character.Name);
                        sceneRewrite = cachedExperiential.Result.SceneRewrites
                            .OrderByDescending(x => x.SequenceNumber)
                            .First().Content;
                        isDead = cachedExperiential.Result.IsDead;
                    }
                    else
                    {
                        var experientialOutput = await experientialNarratorAgent.Invoke(context, character, storyTrackerResult, cancellationToken);
                        sceneRewrite = experientialOutput.SceneRewrite;
                        isDead = experientialOutput.IsDead;

                        var experientialContext = new CharacterContext
                        {
                            CharacterId = character.CharacterId,
                            CharacterState = character.CharacterState,
                            CharacterTracker = character.CharacterTracker,
                            Name = character.Name,
                            Description = character.Description,
                            Relationships = [],
                            SceneRewrites =
                            [
                                new CharacterSceneContext
                                {
                                    Content = sceneRewrite,
                                    SceneTracker = storyTrackerResult,
                                    SequenceNumber = character.SceneRewrites.MaxBy(x => x.SequenceNumber)
                                                         ?.SequenceNumber
                                                     + 1
                                                     ?? 0
                                }
                            ],
                            Importance = character.Importance,
                            SimulationMetadata = null,
                            IsDead = isDead
                        };

                        lock (context)
                        {
                            context.PendingReflectionCache[character.CharacterId] = new CachedReflectionResult
                            {
                                CharacterId = character.CharacterId,
                                CharacterName = character.Name,
                                Source = ReflectionSource.ExperientialNarrator,
                                Result = experientialContext
                            };
                        }
                    }

                    if (isDead)
                    {
                        logger.Information("Character {Character} died in scene, skipping reflection and tracker", character.Name);
                        lock (context)
                        {
                            context.PendingReflectionCache.Remove(character.CharacterId);
                        }

                        return new CharacterContext
                        {
                            CharacterId = character.CharacterId,
                            CharacterState = character.CharacterState,
                            CharacterTracker = character.CharacterTracker,
                            Name = character.Name,
                            Description = character.Description,
                            Relationships = [],
                            SceneRewrites =
                            [
                                new CharacterSceneContext
                                {
                                    Content = sceneRewrite,
                                    SceneTracker = storyTrackerResult,
                                    SequenceNumber = character.SceneRewrites.MaxBy(x => x.SequenceNumber)
                                                         ?.SequenceNumber
                                                     + 1
                                                     ?? 0
                                }
                            ],
                            Importance = character.Importance,
                            SimulationMetadata = null,
                            IsDead = true
                        };
                    }

                    var characterWithSceneRewrite = new CharacterContext
                    {
                        CharacterId = character.CharacterId,
                        CharacterState = character.CharacterState,
                        CharacterTracker = character.CharacterTracker,
                        Name = character.Name,
                        Description = character.Description,
                        Relationships = character.Relationships,
                        SceneRewrites = character.SceneRewrites.Concat(
                        [
                            new CharacterSceneContext
                            {
                                Content = sceneRewrite,
                                SceneTracker = storyTrackerResult,
                                SequenceNumber = character.SceneRewrites.MaxBy(x => x.SequenceNumber)
                                                     ?.SequenceNumber
                                                 + 1
                                                 ?? 0
                            }
                        ]).ToList(),
                        Importance = character.Importance,
                        SimulationMetadata = character.SimulationMetadata,
                        IsDead = false
                    };

                    var trackerTask = characterTrackerAgent.Invoke(context, character, sceneRewrite, storyTrackerResult, cancellationToken);

                    if (context.PendingReflectionCache.TryGetValue(character.CharacterId, out var cachedReflection)
                        && cachedReflection.Source == ReflectionSource.CharacterReflection)
                    {
                        logger.Information("Using cached clinical assessment for {Character}", character.Name);
                        characterContext = cachedReflection.Result;
                    }
                    else
                    {
                        var assessorOutput = await clinicalAssessorAgent.Invoke(context, character, sceneRewrite, storyTrackerResult, cancellationToken);

                        var characterRelationships = assessorOutput.Relationships.Select(relOutput =>
                        {
                            var existingRel = characterWithSceneRewrite.Relationships
                                .SingleOrDefault(x => string.Equals(x.TargetCharacterName, relOutput.Toward, StringComparison.OrdinalIgnoreCase));

                            return new CharacterRelationshipContext
                            {
                                TargetCharacterName = relOutput.Toward,
                                Data = relOutput.Data ?? new Dictionary<string, object>(),
                                UpdateTime = storyTrackerResult.Time,
                                SequenceNumber = existingRel?.SequenceNumber + 1 ?? 0,
                                Dynamic = relOutput.Dynamic ?? existingRel?.Dynamic ?? string.Empty
                            };
                        }).ToList();

                        characterContext = new CharacterContext
                        {
                            CharacterId = characterWithSceneRewrite.CharacterId,
                            CharacterState = assessorOutput.Identity ?? characterWithSceneRewrite.CharacterState,
                            CharacterTracker = characterWithSceneRewrite.CharacterTracker,
                            Name = characterWithSceneRewrite.Name,
                            Description = characterWithSceneRewrite.Description,
                            Relationships = characterRelationships,
                            SceneRewrites = characterWithSceneRewrite.SceneRewrites,
                            Importance = characterWithSceneRewrite.Importance,
                            SimulationMetadata = null,
                            IsDead = false
                        };

                        lock (context)
                        {
                            context.PendingReflectionCache[character.CharacterId] = new CachedReflectionResult
                            {
                                CharacterId = character.CharacterId,
                                CharacterName = character.Name,
                                Source = ReflectionSource.CharacterReflection,
                                Result = characterContext
                            };
                        }
                    }

                    var contextGatheringTask = GatherAndStoreCharacterContext(context, characterContext, cancellationToken);
                    var worldInfoTask = context.SkipWorldInfoExtractor
                        ? Task.CompletedTask
                        : ExtractWorldInfoFromReflection(context, characterContext, storyTrackerResult, cancellationToken);
                    var storySummaryTask = ProcessStorySummaryIfNeeded(context, characterContext, cancellationToken);

                    await Task.WhenAll(trackerTask, contextGatheringTask, worldInfoTask, storySummaryTask);

                    var tracker = await trackerTask;
                    characterContext.CharacterTracker = tracker.Tracker;
                    characterContext.IsDead = tracker.IsDead;

                    lock (context)
                    {
                        context.PendingReflectionCache.Remove(character.CharacterId);
                    }

                    return characterContext;
                })
                .ToArray();
        }

        var mcStorySummaryTask = ProcessMcStorySummaryIfNeeded(context, cancellationToken);

        await Task.WhenAll(mainCharTrackerTask, UnpackCharacterUpdates(context, characterUpdateTask), chroniclerTask, mainNarrativeExtractionTask, mcStorySummaryTask);
    }

    private async Task UnpackCharacterUpdates(GenerationContext context, Task<CharacterContext?>[] tasks)
    {
        foreach (var task in tasks)
        {
            var res = await task;
            if (res is not null)
            {
                lock (context)
                {
                    context.CharacterUpdates.Add(res);
                }
            }
        }
    }

    private async Task ProcessMainChar(GenerationContext context, SceneTracker sceneTrackerResult, CancellationToken cancellationToken)
    {
        if (context.SceneContext.Length == 0)
        {
            if (context.InitialMainCharacterTracker != null)
            {
                logger.Information("Using pre-defined initial main character tracker");
                context.NewTracker!.MainCharacter = new MainCharacterState
                {
                    MainCharacter = context.InitialMainCharacterTracker,
                    MainCharacterDescription = null
                };
                return;
            }

            await initMainCharacterTrackerAgent.Invoke(context, cancellationToken);
            return;
        }

        await mainCharacterTrackerAgent.Invoke(context, sceneTrackerResult, cancellationToken);
    }

    private async Task ProcessChronicler(GenerationContext context, SceneTracker storyTrackerResult, CancellationToken cancellationToken)
    {
        if (context.SkipChronicler)
        {
            logger.Information("Chronicler: Skipping (not selected for regeneration)");
            return;
        }

        if (context.ChroniclerOutput is null)
        {
            var chroniclerOutput = await chroniclerAgent.Invoke(context, storyTrackerResult, cancellationToken);
            context.ChroniclerOutput = chroniclerOutput;
            logger.Information("Chronicler completed");
        }
    }

    /// <summary>
    ///     Runs CharacterContextGatherer for a character and stores the result
    ///     in their most recent scene rewrite for use in the next invocation.
    /// </summary>
    private async Task GatherAndStoreCharacterContext(
        GenerationContext context,
        CharacterContext characterContext,
        CancellationToken cancellationToken)
    {
        try
        {
            var gatheredContext = await characterContextGatherer.Invoke(context, characterContext, cancellationToken);

            var lastRewrite = characterContext.SceneRewrites
                .OrderByDescending(x => x.SequenceNumber)
                .FirstOrDefault();

            lastRewrite!.GatheredContext = gatheredContext;
            logger.Information(
                "Stored gathered context for {CharacterName} in scene rewrite #{SequenceNumber}",
                characterContext.Name,
                lastRewrite.SequenceNumber);
        }
        catch (Exception ex)
        {
            logger.Warning(ex, "Failed to gather context for character {CharacterName}, continuing without it", characterContext.Name);
        }
    }

    private async Task ExtractWorldInfoFromMainNarrative(
        GenerationContext context,
        SceneTracker sceneTracker,
        CancellationToken cancellationToken)
    {
        const string sourceKey = "main";

        lock (context)
        {
            if (context.ProcessedWorldInfoSources.Contains(sourceKey))
            {
                logger.Information("Skipping world info extraction from main narrative (already processed)");
                return;
            }
        }

        var narrativeText = context.NewScene?.Scene;
        if (string.IsNullOrEmpty(narrativeText))
            return;

        try
        {
            var alreadyHandled = BuildAlreadyHandledContent(context);
            var result = await worldInfoExtractorAgent.Invoke(context, narrativeText, sceneTracker, alreadyHandled, cancellationToken);
            logger.Information("Extracted {ActivityCount} activities from main narrative",
                result.Activity.Count);

            lock (context)
            {
                context.WorldInfoExtractions ??= new WorldInfoExtractionOutput();
                context.WorldInfoExtractions.Activity.AddRange(result.Activity);
                context.ProcessedWorldInfoSources.Add(sourceKey);
            }
        }
        catch (Exception ex)
        {
            logger.Warning(ex, "Failed to extract world info from main narrative, continuing without it");
        }
    }

    private async Task ExtractWorldInfoFromReflection(
        GenerationContext context,
        CharacterContext characterContext,
        SceneTracker sceneTracker,
        CancellationToken cancellationToken)
    {
        var sourceKey = $"reflection:{characterContext.Name}";

        lock (context)
        {
            if (context.ProcessedWorldInfoSources.Contains(sourceKey))
            {
                logger.Information("Skipping world info extraction from {Character} reflection (already processed)", characterContext.Name);
                return;
            }
        }

        var lastRewrite = characterContext.SceneRewrites
            .OrderByDescending(x => x.SequenceNumber)
            .FirstOrDefault();

        if (lastRewrite == null || string.IsNullOrEmpty(lastRewrite.Content))
            return;

        try
        {
            var alreadyHandled = BuildAlreadyHandledContent(context);
            var result = await worldInfoExtractorAgent.Invoke(context, lastRewrite.Content, sceneTracker, alreadyHandled, cancellationToken);
            logger.Information("Extracted {ActivityCount} activities from {Character} reflection",
                result.Activity.Count, characterContext.Name);

            lock (context)
            {
                context.WorldInfoExtractions ??= new WorldInfoExtractionOutput();
                context.WorldInfoExtractions.Activity.AddRange(result.Activity);
                context.ProcessedWorldInfoSources.Add(sourceKey);
            }
        }
        catch (Exception ex)
        {
            logger.Warning(ex, "Failed to extract world info from {Character} reflection, continuing without it", characterContext.Name);
        }
    }

    private static AlreadyHandledContent BuildAlreadyHandledContent(GenerationContext context)
    {
        var creationRequests = context.NewScene?.CreationRequests;
        return new AlreadyHandledContent
        {
            Characters = creationRequests?.Characters,
            Locations = creationRequests?.Locations,
            Items = creationRequests?.Items,
            Lore = creationRequests?.Lore,
            WorldEvents = context.NewWorldEvents,
            BackgroundCharacters = context.NewBackgroundCharacters
        };
    }

    private async Task ProcessStorySummaryIfNeeded(
        GenerationContext context,
        CharacterContext characterContext,
        CancellationToken cancellationToken)
    {
        try
        {
            var orderedRewrites = characterContext.SceneRewrites
                .OrderBy(s => s.SequenceNumber)
                .ToList();

            if (orderedRewrites.Count == 0)
                return;

            var totalRewrites = orderedRewrites.Last().SequenceNumber + 1;

            if (totalRewrites <= CharacterAgent.SceneContext)
                return;

            var agedOutSequenceNumber = totalRewrites - CharacterAgent.SceneContext - 1;
            var agedOutRewrite = orderedRewrites.FirstOrDefault(r => r.SequenceNumber == agedOutSequenceNumber);

            if (agedOutRewrite == null)
                return;

            var previousSummary = characterContext.SceneRewrites
                .Where(s => !string.IsNullOrEmpty(s.StorySummary))
                .OrderByDescending(s => s.SequenceNumber)
                .FirstOrDefault()?.StorySummary ?? string.Empty;

            var result = await storySummaryAgent.InvokeForCharacter(
                context,
                characterContext,
                agedOutRewrite.Content,
                agedOutSequenceNumber,
                previousSummary,
                cancellationToken);

            orderedRewrites.Last().StorySummary = result.StorySummary;

            logger.Information(
                "Updated story summary for {CharacterName} (aged out scene #{SceneNumber})",
                characterContext.Name,
                agedOutSequenceNumber);
        }
        catch (Exception ex)
        {
            logger.Warning(ex, "Failed to process story summary for {CharacterName}, continuing without it", characterContext.Name);
        }
    }

    private async Task ProcessMcStorySummaryIfNeeded(
        GenerationContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            if (context.SceneContext.Length == 0)
                return;

            var newSceneNumber = context.SceneContext.Max(s => s.SequenceNumber) + 1;

            if (newSceneNumber < WriterAgent.SceneContextCount)
            {
                return;
            }

            var agedOutSequenceNumber = newSceneNumber - WriterAgent.SceneContextCount;
            var agedOutScene = context.SceneContext
                .FirstOrDefault(s => s.SequenceNumber == agedOutSequenceNumber);

            if (agedOutScene == null)
            {
                return;
            }

            var previousSummary = context.SceneContext
                .Where(s => !string.IsNullOrEmpty(s.Metadata.McStorySummary))
                .OrderByDescending(s => s.SequenceNumber)
                .FirstOrDefault()?.Metadata.McStorySummary ?? string.Empty;

            var result = await storySummaryAgent.InvokeForMc(
                context,
                agedOutScene.SceneContent,
                agedOutSequenceNumber,
                previousSummary,
                cancellationToken);

            context.NewMcStorySummary = result.StorySummary;

            logger.Information(
                "Updated MC story summary (aged out scene #{SceneNumber})",
                agedOutSequenceNumber);
        }
        catch (Exception ex)
        {
            logger.Warning(ex, "Failed to process MC story summary, continuing without it");
        }
    }
}