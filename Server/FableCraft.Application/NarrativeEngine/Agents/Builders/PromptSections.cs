using System.Text.Json;
using System.Text.Json.Serialization;

using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;

namespace FableCraft.Application.NarrativeEngine.Agents;

/// <summary>
///     Builds formatted prompt sections with XML tags for narrative engine agents
/// </summary>
internal static class PromptSections
{
    private readonly static JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true
    };

    private readonly static JsonSerializerOptions JsonOptionsIgnoreNull = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static JsonSerializerOptions GetJsonOptions(bool ignoreNull = false)
    {
        return ignoreNull ? JsonOptionsIgnoreNull : JsonOptions;
    }

    public static string StoryTracker(StoryTracker tracker, bool ignoreNull = false)
    {
        return $"""
                <story_tracker>
                {tracker.ToJsonString(GetJsonOptions(ignoreNull))}
                </story_tracker>
                """;
    }

    public static string CurrentStoryTracker(SceneContext[] sceneContext)
    {
        Tracker? tracker = sceneContext
            .Where(x => x.Metadata.Tracker != null)
            .OrderByDescending(x => x.SequenceNumber)
            .FirstOrDefault()?.Metadata.Tracker;

        return tracker != null
            ? $"""
               <current_story_tracker>
               {tracker.Story.ToJsonString(GetJsonOptions())}
               </current_story_tracker>
               """
            : string.Empty;
    }

    public static string LastScenes(SceneContext[] sceneContext, int count)
    {
        var scenes = sceneContext
            .OrderByDescending(x => x.SequenceNumber);

        var formatted = string.Join("\n",
            scenes
                .Take(count)
                .Select(x => $"""
                              SCENE NUMBER: {x.SequenceNumber}
                              {x.SceneContent}
                              {x.PlayerChoice}
                              """));

        return $"""
                <last_scenes>
                {formatted}
                </last_scenes>
                """;
    }

    public static string RecentScenes(SceneContext[] sceneContext, int count = 3)
    {
        var formatted = string.Join("\n\n---\n\n",
            sceneContext
                .OrderByDescending(x => x.SequenceNumber)
                .TakeLast(count)
                .Select(s => $"""
                              SCENE NUMBER: {s.SequenceNumber}
                              {s.SceneContent}
                              {s.PlayerChoice}
                              """));

        return $"""
                <recent_scenes>
                {formatted}
                </recent_scenes>
                """;
    }

    public static string SceneContent(string? content)
    {
        return string.IsNullOrEmpty(content)
            ? string.Empty
            : $"""
               <scene_content>
               {content}
               </scene_content>
               """;
    }

    public static string CurrentScene(string? content)
    {
        return string.IsNullOrEmpty(content)
            ? string.Empty
            : $"""
               Here's the current narrative scene:
               <current_scene>
               {content}
               </current_scene>
               """;
    }

    public static string PreviousScene(string? content)
    {
        return string.IsNullOrEmpty(content)
            ? string.Empty
            : $"""
               <previous_scene>
               {content}
               </previous_scene>
               """;
    }

    public static string PlayerAction(string action)
    {
        return $"""
                <player_action>
                {action}
                </player_action>
                """;
    }

    public static string MainCharacter(GenerationContext context)
    {
        return $"""
                The main protagonist of the story. All scenes are written from their perspective:
                <main_character>
                Name: {context.MainCharacter.Name}
                {context.LatestTracker()?.MainCharacter?.MainCharacterDescription ?? context.MainCharacter.Description}
                </main_character>
                """;
    }

    public static string MainCharacterTracker(SceneContext[] sceneContext, bool ignoreNull = true)
    {
        if (sceneContext.Length == 0)
        {
            return string.Empty;
        }

        Tracker? tracker = sceneContext
            .OrderByDescending(x => x.SequenceNumber)
            .FirstOrDefault(x => x.Metadata.Tracker != null)?.Metadata.Tracker;

        JsonSerializerOptions options = GetJsonOptions(ignoreNull);
        return $"""
                Set of trackers for the main character, describing their current state and development in the previous scene :
                <main_character_tracker>
                {tracker!.MainCharacter.ToJsonString(options)}
                </main_character_tracker>
                """;
    }

    public static string ExistingCharacters(IEnumerable<CharacterContext> characters, IEnumerable<CharacterContext>? extendedCharacters = null)
    {
        var extended = extendedCharacters?.ToList() ?? [];
        var formatted = string.Join("\n\n",
            characters.Select(c =>
            {
                if (extended.Any(x => x.Name == c.Name))
                {
                    return $"""
                            <character>
                            Name: {c.Name}
                            {c.Description}

                            Current state of the character:
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

        return $"""
                List of existing characters in the story:
                <existing_characters>
                {formatted}
                </existing_characters>
                """;
    }

    public static string CharacterStateContext(CharacterContext context, bool ignoreNull = true)
    {
        JsonSerializerOptions options = GetJsonOptions(ignoreNull);
        return $"""
                Current state and development tracker for the character {context.Name}:
                <previous_character_state>
                {context.CharacterState.ToJsonString(options)}
                </previous_character_state>
                <previous_tracker>
                {context.CharacterTracker.ToJsonString(options)}
                </previous_tracker>
                <previous_development>
                {context.DevelopmentTracker.ToJsonString(options)}
                </previous_development>
                """;
    }

    public static string RecentScenesForCharacter(SceneContext[] sceneContext, string mainCharacterName, string characterName, int count = 3)
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

        return $"""
                CRITICAL! These scenes are written from the perspective of the main character {mainCharacterName}. Before updating the tracker, rewrite these scenes from the perspective of the character {characterName}. Make sure to include ONLY their thoughts, feelings, knowledge, and reactions to the events happening in each scene.
                <recent_scenes>
                {scenes}
                </recent_scenes>
                """;
    }

    public static string NewCharacters(CharacterContext[]? characters)
    {
        if (characters == null || characters.Length == 0) return string.Empty;

        var formatted = string.Join("\n\n", characters.Select(c => $"{c.Name}\n{c.Description}"));
        return $"""
                <new_characters>
                <character>
                {formatted}
                </character>
                </new_characters>
                """;
    }

    public static string CreatedCharacters<T>(T[]? characters, bool ignoreNull = false)
    {
        if (characters == null || characters.Length == 0) return string.Empty;

        return $"""
                <created_characters>
                {characters.ToJsonString(GetJsonOptions(ignoreNull))}
                </created_characters>
                """;
    }

    public static string CharacterCreationContext<T>(T context, bool ignoreNull = false)
    {
        return $"""
                <character_creation_context>
                {context.ToJsonString(GetJsonOptions(ignoreNull))}
                </character_creation_context>
                """;
    }

    public static string NewCharacterRequests<T>(IEnumerable<T>? requests, bool ignoreNull = false)
    {
        var list = requests?.ToArray() ?? [];
        return $"""
                <new_characters_requests>
                {list.ToJsonString(GetJsonOptions(ignoreNull))}
                </new_characters_requests>
                """;
    }

    public static string SceneDirection<T>(T direction, bool ignoreNull = false)
    {
        return $"""
                <scene_direction>
                {direction.ToJsonString(GetJsonOptions(ignoreNull))}
                </scene_direction>
                """;
    }

    public static string ContinuityCheck<T>(T check, bool ignoreNull = false)
    {
        return $"""
                <continuity_check>
                {check.ToJsonString(GetJsonOptions(ignoreNull))}
                </continuity_check>
                """;
    }

    public static string SceneMetadata<T>(T metadata, bool ignoreNull = false)
    {
        return $"""
                <scene_metadata>
                {metadata.ToJsonString(GetJsonOptions(ignoreNull))}
                </scene_metadata>
                """;
    }

    public static string LastSceneNarrativeDirection<T>(T? direction, bool ignoreNull = false) where T : class
    {
        if (direction == null) return string.Empty;

        return $"""
                <last_scene_narrative_direction>
                {direction.ToJsonString(GetJsonOptions(ignoreNull))}
                </last_scene_narrative_direction>
                """;
    }

    public static string LastNarrativeDirections(SceneContext[] sceneContext, int count = 1)
    {
        var directions = string.Join("\n",
            sceneContext
                .OrderByDescending(y => y.SequenceNumber)
                .Take(count)
                .Select(z => z.Metadata.NarrativeMetadata.ToJsonString(JsonOptions)));

        return $"""
                <last_narrative_directions>
                {directions}
                </last_narrative_directions>
                """;
    }

    public static string InitialInstruction(string guidance)
    {
        return $"""
                This is the first scene of the adventure. Create the initial narrative direction based on the main character and adventure setup.
                <initial_instruction>
                {guidance}
                </initial_instruction>
                """;
    }

    public static string NewLocations<T>(T[]? locations, bool ignoreNull = false)
    {
        return $"""
                <new_locations>
                {(locations ?? []).ToJsonString(GetJsonOptions(ignoreNull))}
                </new_locations>
                """;
    }

    public static string NewLore<T>(T[]? lore, bool ignoreNull = false)
    {
        return $"""
                <new_lore>
                {(lore ?? []).ToJsonString(GetJsonOptions(ignoreNull))}
                </new_lore>
                """;
    }

    public static string LocationRequest<T>(T request, bool ignoreNull = false)
    {
        return $"""
                <location_request>
                {request.ToJsonString(GetJsonOptions(ignoreNull))}
                </location_request>
                """;
    }

    public static string LoreCreationContext<T>(T context, bool ignoreNull = false)
    {
        return $"""
                <lore_creation_context>
                {context.ToJsonString(GetJsonOptions(ignoreNull))}
                </lore_creation_context>
                """;
    }

    public static string ItemRequest<T>(T request, bool ignoreNull = false)
    {
        return $"""
                <item_request>
                {request.ToJsonString(GetJsonOptions(ignoreNull))}
                </item_request>
                """;
    }

    public static string NewItems(GeneratedItem[]? items, bool ignoreNull = false)
    {
        return $"""
                <new_items>
                {(items ?? []).ToJsonString(GetJsonOptions(ignoreNull))}
                </new_items>
                """;
    }

    public static string Context(ContextBase? context, bool ignoreNull = false)
    {
        if (context == null) return string.Empty;

        return $"""
                Knowledge extracted from knowledge graph. Do not query the knowledge graph for these facts again.
                <knowledge_graph_context>
                {context.ContextBases.ToJsonString(GetJsonOptions(ignoreNull))}
                </knowledge_graph_context>
                """;
    }

    public static string PreviousStoryTrackers(SceneContext[] sceneContext, int count = 1, bool ignoreNull = true)
    {
        JsonSerializerOptions options = GetJsonOptions(ignoreNull);
        var formatted = string.Join("\n\n",
            sceneContext
                .OrderByDescending(x => x.SequenceNumber)
                .Take(count)
                .Select(s => s.Metadata.Tracker?.Story.ToJsonString(options)));

        return $"""
                Here are the story trackers from previous scene. Update their information based on the current scene:
                <previous_story_trackers>
                {formatted}
                </previous_story_trackers>
                """;
    }

    public static string AdventureStartTime(string startTime)
    {
        return $"""
                Set the initial time to:
                <adventure_start_time>
                {startTime}
                </adventure_start_time>
                """;
    }
}