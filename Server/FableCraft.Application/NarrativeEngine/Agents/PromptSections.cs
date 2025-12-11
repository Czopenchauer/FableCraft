using System.Text.Json;

using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;

namespace FableCraft.Application.NarrativeEngine.Agents;

/// <summary>
/// Common prompt section builders for narrative engine agents
/// </summary>
internal static class PromptSections
{
    /// <summary>
    /// Formats scene context with scene numbers for last N scenes
    /// </summary>
    public static string FormatLastScenes(SceneContext[] sceneContext, int count)
    {
        return string.Join("\n",
            sceneContext
                .OrderByDescending(x => x.SequenceNumber)
                .Skip(1)
                .Take(count)
                .Select(x => $"""
                              SCENE NUMBER: {x.SequenceNumber}
                              {x.SceneContent}
                              {x.PlayerChoice}
                              """));
    }

    /// <summary>
    /// Formats characters list with XML character tags
    /// </summary>
    public static string FormatCharactersList(IEnumerable<CharacterContext> characters, IEnumerable<CharacterContext>? extendedCharacters = null)
    {
        var chars = extendedCharacters ?? Array.Empty<CharacterContext>();
        return string.Join("\n\n",
            characters.Select(c =>
            {
                if (chars.Any(x => x.Name == c.Name))
                {
                    return $"""
                            <character>
                            Name: {c.Name}
                            {c.Description}
                            {c.CharacterState}
                            </character>
                            """;
                }

                return $"""
                        <character>
                        Name: {c.Name}
                        {c.Description}
                        </character>
                        """;
            }));
    }

    /// <summary>
    /// Formats main character info
    /// </summary>
    public static string FormatMainCharacter(MainCharacter mainCharacter, string? latestDescription = null)
    {
        return $"""
                Name: {mainCharacter.Name}
                {latestDescription ?? mainCharacter.Description}
                """;
    }

    /// <summary>
    /// Gets the latest tracker from scene context
    /// </summary>
    public static Tracker? GetLatestTracker(SceneContext[] sceneContext)
    {
        return sceneContext
            .Where(x => x.Metadata.Tracker != null)
            .OrderByDescending(x => x.SequenceNumber)
            .FirstOrDefault()?.Metadata.Tracker;
    }
}

/// <summary>
/// Extension methods for ChatHistoryBuilder to add common prompt sections
/// </summary>
internal static class PromptSectionExtensions
{
    /// <summary>
    /// Adds story tracker section
    /// </summary>
    public static ChatHistoryBuilder WithStoryTracker<T>(this ChatHistoryBuilder builder, T tracker, bool ignoreNull = false)
        => builder.WithTaggedJson("story_tracker", tracker, ignoreNull);

    /// <summary>
    /// Adds main character section
    /// </summary>
    public static ChatHistoryBuilder WithMainCharacter(this ChatHistoryBuilder builder, MainCharacter mainCharacter, string? latestDescription)
        => builder.WithTaggedContent("main_character", PromptSections.FormatMainCharacter(mainCharacter, latestDescription));

    /// <summary>
    /// Adds existing characters section
    /// </summary>
    public static ChatHistoryBuilder WithExistingCharacters(
        this ChatHistoryBuilder builder,
        IEnumerable<CharacterContext> characters,
        IEnumerable<CharacterContext>? contextGatheredRelevantCharacters = null)
        => builder.WithTaggedContent("existing_characters", PromptSections.FormatCharactersList(characters, contextGatheredRelevantCharacters));

    /// <summary>
    /// Adds current scene tracker section if tracker exists
    /// </summary>
    public static ChatHistoryBuilder WithCurrentSceneTracker(this ChatHistoryBuilder builder, SceneContext[] sceneContext, bool ignoreNull = false)
    {
        var tracker = PromptSections.GetLatestTracker(sceneContext);
        return tracker != null
            ? builder.WithTaggedJson("current_scene_tracker", tracker, ignoreNull)
            : builder;
    }

    /// <summary>
    /// Adds story summary section
    /// </summary>
    public static ChatHistoryBuilder WithStorySummary(this ChatHistoryBuilder builder, string? summary)
        => summary != null ? builder.WithTaggedContent("story_summary", summary) : builder;

    /// <summary>
    /// Adds last scenes section
    /// </summary>
    public static ChatHistoryBuilder WithLastScenes(this ChatHistoryBuilder builder, SceneContext[] sceneContext, int count)
        => builder.WithTaggedContent("last_scenes", PromptSections.FormatLastScenes(sceneContext, count));

    /// <summary>
    /// Adds extra context (RAG gathered context) section
    /// </summary>
    public static ChatHistoryBuilder WithExtraContext<T>(this ChatHistoryBuilder builder, T? context, bool ignoreNull = false) where T : class
        => builder.WithTaggedJsonIfNotNull("extra_context", context, ignoreNull);

    /// <summary>
    /// Adds player action section
    /// </summary>
    public static ChatHistoryBuilder WithPlayerAction(this ChatHistoryBuilder builder, string playerAction)
        => builder.WithTaggedContent("player_action", playerAction);

    /// <summary>
    /// Adds scene direction section
    /// </summary>
    public static ChatHistoryBuilder WithSceneDirection<T>(this ChatHistoryBuilder builder, T sceneDirection, bool ignoreNull = false)
        => builder.WithTaggedJson("scene_direction", sceneDirection, ignoreNull);

    /// <summary>
    /// Adds previous trackers section
    /// </summary>
    public static ChatHistoryBuilder WithMainCharacterTracker(this ChatHistoryBuilder builder, SceneContext[] sceneContext, int count = 1, bool ignoreNull = false)
    {
        var trackers = sceneContext.OrderByDescending(x => x.SequenceNumber).Skip(1).First().Metadata.Tracker!;
        return builder
            .WithTaggedJson("previous_tracker", trackers.MainCharacter, ignoreNull)
            .WithTaggedJson("previous_development", trackers.MainCharacterDevelopment, ignoreNull);
    }

    /// <summary>
    /// Adds scene content section
    /// </summary>
    public static ChatHistoryBuilder WithSceneContent(this ChatHistoryBuilder builder, string? sceneContent)
        => sceneContent != null ? builder.WithTaggedContent("scene_content", sceneContent) : builder;

    /// <summary>
    /// Adds previous scene section
    /// </summary>
    public static ChatHistoryBuilder WithPreviousScene(this ChatHistoryBuilder builder, string? sceneContent)
        => sceneContent != null ? builder.WithTaggedContent("previous_scene", sceneContent) : builder;

    /// <summary>
    /// Adds current scene section
    /// </summary>
    public static ChatHistoryBuilder WithCurrentScene(this ChatHistoryBuilder builder, string? sceneContent)
        => sceneContent != null ? builder.WithTaggedContent("current_scene", sceneContent) : builder;

    /// <summary>
    /// Adds new locations section
    /// </summary>
    public static ChatHistoryBuilder WithNewLocations<T>(this ChatHistoryBuilder builder, T[]? locations, bool ignoreNull = false)
        => builder.WithTaggedJson("new_locations", locations ?? Array.Empty<T>(), ignoreNull);

    /// <summary>
    /// Adds new lore section
    /// </summary>
    public static ChatHistoryBuilder WithNewLore<T>(this ChatHistoryBuilder builder, T[]? lore, bool ignoreNull = false)
        => builder.WithTaggedJson("new_lore", lore ?? Array.Empty<T>(), ignoreNull);

    /// <summary>
    /// Adds new characters requests section
    /// </summary>
    public static ChatHistoryBuilder WithNewCharacterRequests<T>(this ChatHistoryBuilder builder, T[]? requests, string? preface = null, bool ignoreNull = false)
    {
        if (preface != null)
        {
            builder.WithUserMessage(preface);
        }

        return builder.WithTaggedJson("new_characters_requests", requests ?? Array.Empty<T>(), ignoreNull);
    }

    /// <summary>
    /// Adds continuity check section
    /// </summary>
    public static ChatHistoryBuilder WithContinuityCheck<T>(this ChatHistoryBuilder builder, T continuityCheck, bool ignoreNull = false)
        => builder.WithTaggedJson("continuity_check", continuityCheck, ignoreNull);

    /// <summary>
    /// Adds scene metadata section
    /// </summary>
    public static ChatHistoryBuilder WithSceneMetadata<T>(this ChatHistoryBuilder builder, T metadata, bool ignoreNull = false)
        => builder.WithTaggedJson("scene_metadata", metadata, ignoreNull);

    /// <summary>
    /// Adds last scene narrative direction section
    /// </summary>
    public static ChatHistoryBuilder WithLastSceneNarrativeDirection<T>(this ChatHistoryBuilder builder, T? direction, bool ignoreNull = false) where T : class
        => builder.WithTaggedJsonIfNotNull("last_scene_narrative_direction", direction, ignoreNull);

    /// <summary>
    /// Adds character creation context section
    /// </summary>
    public static ChatHistoryBuilder WithCharacterCreationContext<T>(this ChatHistoryBuilder builder, T context, bool ignoreNull = false)
        => builder.WithTaggedJson("character_creation_context", context, ignoreNull);

    /// <summary>
    /// Adds location request section
    /// </summary>
    public static ChatHistoryBuilder WithLocationRequest<T>(this ChatHistoryBuilder builder, T request, bool ignoreNull = false)
        => builder.WithTaggedJson("location_request", request, ignoreNull);

    /// <summary>
    /// Adds lore creation context section
    /// </summary>
    public static ChatHistoryBuilder WithLoreCreationContext<T>(this ChatHistoryBuilder builder, T context, bool ignoreNull = false)
        => builder.WithTaggedJson("lore_creation_context", context, ignoreNull);

    /// <summary>
    /// Adds initial instruction for first scene scenarios
    /// </summary>
    public static ChatHistoryBuilder WithInitialInstruction(this ChatHistoryBuilder builder, string guidance)
        => builder.WithUserMessage($"""
                                    This is the first scene of the adventure. Create the initial narrative direction based on the main character and adventure setup.
                                    <initial_instruction>
                                    {guidance}
                                    </initial_instruction>
                                    """);

    /// <summary>
    /// Adds context section (for RAG context)
    /// </summary>
    public static ChatHistoryBuilder WithContext<T>(this ChatHistoryBuilder builder, T? context, bool ignoreNull = false) where T : class
        => builder.WithTaggedJsonIfNotNull("context", context, ignoreNull);

    /// <summary>
    /// Adds created characters section
    /// </summary>
    public static ChatHistoryBuilder WithCreatedCharacters<T>(this ChatHistoryBuilder builder, T[]? characters, bool ignoreNull = false) where T : class
        => characters?.Length > 0 ? builder.WithTaggedJson("created_characters", characters, ignoreNull) : builder;

    /// <summary>
    /// Adds recent scenes with perspective shift instruction
    /// </summary>
    public static ChatHistoryBuilder WithRecentScenesForCharacter(
        this ChatHistoryBuilder builder,
        SceneContext[] sceneContext,
        string mainCharacterName,
        string characterName,
        int count = 3)
    {
        var scenes = string.Join("\n\n---\n\n",
            sceneContext
                .OrderByDescending(x => x.SequenceNumber)
                .TakeLast(count)
                .Select(s => $"""
                              SCENE NUMBER: {s.SequenceNumber}
                              {s.SceneContent}
                              {s.PlayerChoice}
                              """));

        return builder.WithUserMessage($"""
                                        CRITICAL! These scenes are written from the perspective of the main character {mainCharacterName}. Before updating the tracker, rewrite these scenes from the perspective of the character {characterName}. Make sure to include ONLY their thoughts, feelings, knowledge, and reactions to the events happening in each scene.
                                        <recent_scenes>
                                        {scenes}
                                        </recent_scenes>
                                        """);
    }

    /// <summary>
    /// Adds character state context (previous tracker, development, state)
    /// </summary>
    public static ChatHistoryBuilder WithCharacterStateContext(
        this ChatHistoryBuilder builder,
        CharacterContext context,
        bool ignoreNull = true)
    {
        return builder
            .WithTaggedJson("previous_character_state", context.CharacterState, ignoreNull)
            .WithTaggedJson("previous_tracker", context.CharacterTracker, ignoreNull)
            .WithTaggedJson("previous_development", context.DevelopmentTracker, ignoreNull);
    }

    /// <summary>
    /// Adds last narrative directions section
    /// </summary>
    public static ChatHistoryBuilder WithLastNarrativeDirections(this ChatHistoryBuilder builder, SceneContext[] sceneContext, int count = 1)
    {
        var options = ChatHistoryBuilder.GetJsonOptions();
        var directions = string.Join("\n",
            sceneContext
                .OrderByDescending(y => y.SequenceNumber)
                .Take(count)
                .Select(z => JsonSerializer.Serialize(z.Metadata.NarrativeMetadata, options)));

        return builder.WithTaggedContent("last_narrative_directions", directions);
    }

    /// <summary>
    /// Adds recent scenes section (without perspective shift)
    /// </summary>
    public static ChatHistoryBuilder WithRecentScenes(this ChatHistoryBuilder builder, SceneContext[] sceneContext, int count = 3)
    {
        var scenes = string.Join("\n\n---\n\n",
            sceneContext
                .OrderByDescending(x => x.SequenceNumber)
                .TakeLast(count)
                .Select(s => $"""
                              SCENE NUMBER: {s.SequenceNumber}
                              {s.SceneContent}
                              {s.PlayerChoice}
                              """));

        return builder.WithTaggedContent("recent_scenes", scenes);
    }

    /// <summary>
    /// Adds main character tracker section
    /// </summary>
    public static ChatHistoryBuilder WithMainCharacterTracker<T>(this ChatHistoryBuilder builder, T? tracker, bool ignoreNull = true) where T : class
        => builder.WithTaggedJsonIfNotNull("main_character_tracker", tracker, ignoreNull);

    /// <summary>
    /// Adds previous development section
    /// </summary>
    public static ChatHistoryBuilder WithPreviousDevelopment<T>(this ChatHistoryBuilder builder, T? development, bool ignoreNull = true) where T : class
        => builder.WithTaggedJsonIfNotNull("previous_development", development, ignoreNull);
}