using System.Text.Json;

using FableCraft.Application.NarrativeEngine.Agents;
using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;

using Serilog;

namespace FableCraft.Application.NarrativeEngine.Workflow;

/// <summary>
///     Processor responsible for character tracking: main character tracker, character reflection,
///     character tracker updates, and chronicler. Depends on SceneTrackerProcessor having run first.
/// </summary>
internal sealed class CharacterTrackersProcessor(
    MainCharacterTrackerAgent mainCharacterTrackerAgent,
    CharacterReflectionAgent characterReflectionAgent,
    CharacterTrackerAgent characterTrackerAgent,
    CharacterContextGatherer characterContextGatherer,
    InitMainCharacterTrackerAgent initMainCharacterTrackerAgent,
    ChroniclerAgent chroniclerAgent,
    LoreCrafter loreCrafter,
    WorldInfoExtractorAgent worldInfoExtractorAgent,
    ILogger logger) : IProcessor
{
    public async Task Invoke(GenerationContext context, CancellationToken cancellationToken)
    {
        var storyTrackerResult = context.NewTracker?.Scene
                                 ?? throw new InvalidOperationException(
                                     "CharacterTrackersProcessor requires context.NewTracker.Scene to be populated. "
                                     + "Ensure SceneTrackerProcessor runs before this processor.");

        var chroniclerTask = ProcessChronicler(context, storyTrackerResult, cancellationToken);
        var mainNarrativeExtractionTask = context.SkipWorldInfoExtractor
            ? Task.CompletedTask
            : ExtractWorldInfoFromMainNarrative(context, storyTrackerResult, cancellationToken);

        var mainCharTrackerTask = context.NewTracker?.MainCharacter?.MainCharacter != null
            ? Task.FromResult(context.NewTracker.MainCharacter)
            : ProcessMainChar(context, storyTrackerResult, cancellationToken);

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

                    if (context.PendingReflectionCache.TryGetValue(character.CharacterId, out var cached)
                        && cached.Source == ReflectionSource.CharacterReflection)
                    {
                        logger.Information("Using cached reflection for {Character}", character.Name);
                        characterContext = cached.Result;
                    }
                    else
                    {
                        characterContext = await characterReflectionAgent.Invoke(context, character, storyTrackerResult, cancellationToken);

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

                    var trackerTask = characterTrackerAgent.Invoke(context, character, characterContext, storyTrackerResult, cancellationToken);
                    var contextGatheringTask = GatherAndStoreCharacterContext(context, characterContext, cancellationToken);
                    var worldInfoTask = context.SkipWorldInfoExtractor
                        ? Task.CompletedTask
                        : ExtractWorldInfoFromReflection(context, characterContext, storyTrackerResult, cancellationToken);

                    await Task.WhenAll(trackerTask, contextGatheringTask, worldInfoTask);

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

        await Task.WhenAll(mainCharTrackerTask, UnpackCharacterUpdates(context, characterUpdateTask), chroniclerTask, mainNarrativeExtractionTask);
    }

    private static LoreRequest ConvertToLoreRequest(ChroniclerLoreRequest req) => JsonSerializer.Deserialize<LoreRequest>(req.ToJsonString())!;

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

            logger.Information("Chronicler produced {Count} world events", chroniclerOutput.WorldEvents);
            lock (context)
            {
                foreach (var chroniclerOutputWorldEvent in chroniclerOutput.WorldEvents)
                {
                    context.NewWorldEvents.Add(chroniclerOutputWorldEvent);
                }
            }
        }

        if (context.ChroniclerLore.Length == 0 && context.ChroniclerOutput.LoreRequests.Length != 0)
        {
            logger.Information("Chronicler requested {Count} lore entries", context.ChroniclerOutput.LoreRequests.Length);
            var sceneContext = context.SceneContext ?? [];
            var loreResults = await Task.WhenAll(
                context.ChroniclerOutput.LoreRequests.Select(req =>
                    loreCrafter.Invoke(context, ConvertToLoreRequest(req), sceneContext, cancellationToken)));

            context.ChroniclerLore = loreResults;
            logger.Information("Created {Count} lore entries from chronicler requests", loreResults.Length);
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
}