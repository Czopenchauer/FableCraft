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
    InitMainCharacterTrackerAgent initMainCharacterTrackerAgent,
    ChroniclerAgent chroniclerAgent,
    LoreCrafter loreCrafter,
    ILogger logger) : IProcessor
{
    public async Task Invoke(GenerationContext context, CancellationToken cancellationToken)
    {
        var storyTrackerResult = context.NewTracker?.Scene
                                 ?? throw new InvalidOperationException(
                                     "CharacterTrackersProcessor requires context.NewTracker.Scene to be populated. "
                                     + "Ensure SceneTrackerProcessor runs before this processor.");

        var chroniclerTask = ProcessChronicler(context, storyTrackerResult, cancellationToken);

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
                        logger.Information("Character {Character} already processed. Skipping tracking", character.Name);
                        return null;
                    }

                    var characterContext = await characterReflectionAgent.Invoke(context, character, storyTrackerResult, cancellationToken);
                    var tracker = await characterTrackerAgent.Invoke(context, character, characterContext, storyTrackerResult, cancellationToken);
                    characterContext.CharacterTracker = tracker.Tracker;
                    characterContext.IsDead = tracker.IsDead;

                    return characterContext;
                })
                .ToArray();
        }

        await Task.WhenAll(mainCharTrackerTask, UnpackCharacterUpdates(context, characterUpdateTask), chroniclerTask);
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
            await initMainCharacterTrackerAgent.Invoke(context, cancellationToken);
            return;
        }

        await mainCharacterTrackerAgent.Invoke(context, sceneTrackerResult, cancellationToken);
    }

    private async Task ProcessChronicler(GenerationContext context, SceneTracker storyTrackerResult, CancellationToken cancellationToken)
    {
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
            var loreResults = await Task.WhenAll(
                context.ChroniclerOutput.LoreRequests.Select(req =>
                    loreCrafter.Invoke(context, ConvertToLoreRequest(req), cancellationToken)));

            context.ChroniclerLore = loreResults;
            logger.Information("Created {Count} lore entries from chronicler requests", loreResults.Length);
        }
    }
}