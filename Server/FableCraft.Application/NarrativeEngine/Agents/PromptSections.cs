using System.Text.Json;
using System.Text.Json.Serialization;

using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;

namespace FableCraft.Application.NarrativeEngine.Agents;

/// <summary>
/// Builds formatted prompt sections with XML tags for narrative engine agents
/// </summary>
internal static class PromptSections
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true
    };

    private static readonly JsonSerializerOptions JsonOptionsIgnoreNull = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static JsonSerializerOptions GetJsonOptions(bool ignoreNull = false)
        => ignoreNull ? JsonOptionsIgnoreNull : JsonOptions;

    // ===== Story & Scene Sections =====

    public static string StoryTracker<T>(T tracker, bool ignoreNull = false) =>
        $"""
         <story_tracker>
         {JsonSerializer.Serialize(tracker, GetJsonOptions(ignoreNull))}
         </story_tracker>
         """;

    public static string CurrentSceneTracker(SceneContext[] sceneContext, bool ignoreNull = false)
    {
        var tracker = sceneContext
            .Where(x => x.Metadata.Tracker != null)
            .OrderByDescending(x => x.SequenceNumber)
            .FirstOrDefault()?.Metadata.Tracker;

        return tracker != null
            ? $"""
               <current_scene_tracker>
               {JsonSerializer.Serialize(tracker, GetJsonOptions(ignoreNull))}
               </current_scene_tracker>
               """
            : string.Empty;
    }

    public static string LastScenes(SceneContext[] sceneContext, int count, bool skipFirst = true)
    {
        var scenes = sceneContext
            .OrderByDescending(x => x.SequenceNumber);

        if (skipFirst)
            scenes = scenes.Skip(1).OrderByDescending(x => x.SequenceNumber);

        var formatted = string.Join("\n", scenes
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
        var formatted = string.Join("\n\n---\n\n", sceneContext
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

    public static string SceneContent(string? content) =>
        string.IsNullOrEmpty(content) ? string.Empty :
            $"""
             <scene_content>
             {content}
             </scene_content>
             """;

    public static string CurrentScene(string? content) =>
        string.IsNullOrEmpty(content) ? string.Empty :
            $"""
             <current_scene>
             {content}
             </current_scene>
             """;

    public static string PreviousScene(string? content) =>
        string.IsNullOrEmpty(content) ? string.Empty :
            $"""
             <previous_scene>
             {content}
             </previous_scene>
             """;

    public static string StorySummary(string? summary) =>
        string.IsNullOrEmpty(summary) ? string.Empty :
            $"""
             <story_summary>
             {summary}
             </story_summary>
             """;

    public static string PlayerAction(string action) =>
        $"""
         <player_action>
         {action}
         </player_action>
         """;

    // ===== Character Sections =====

    public static string MainCharacter(MainCharacter mainCharacter, string? latestDescription = null) =>
        $"""
         <main_character>
         Name: {mainCharacter.Name}
         {latestDescription ?? mainCharacter.Description}
         </main_character>
         """;

    public static string MainCharacterTracker(SceneContext[] sceneContext, bool ignoreNull = true)
    {
        var tracker = sceneContext
            .OrderByDescending(x => x.SequenceNumber)
            .Skip(1)
            .FirstOrDefault()?.Metadata.Tracker;

        if (tracker == null) return string.Empty;

        var options = GetJsonOptions(ignoreNull);
        return $"""
                <previous_tracker>
                {JsonSerializer.Serialize(tracker.MainCharacter, options)}
                </previous_tracker>
                <previous_development>
                {JsonSerializer.Serialize(tracker.MainCharacterDevelopment, options)}
                </previous_development>
                """;
    }

    public static string MainCharacterTrackerDirect<T1, T2>(T1? tracker, T2? development, bool ignoreNull = true)
    {
        if (tracker == null && development == null) return string.Empty;

        var options = GetJsonOptions(ignoreNull);
        var sb = new System.Text.StringBuilder();

        if (tracker != null)
            sb.AppendLine($"""
                           <main_character_tracker>
                           {JsonSerializer.Serialize(tracker, options)}
                           </main_character_tracker>
                           """);

        if (development != null)
            sb.AppendLine($"""
                           <previous_development>
                           {JsonSerializer.Serialize(development, options)}
                           </previous_development>
                           """);

        return sb.ToString().TrimEnd();
    }

    public static string ExistingCharacters(IEnumerable<CharacterContext> characters, IEnumerable<CharacterContext>? extendedCharacters = null)
    {
        var extended = extendedCharacters?.ToList() ?? [];
        var formatted = string.Join("\n\n", characters.Select(c =>
        {
            if (extended.Any(x => x.Name == c.Name))
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

        return $"""
                <existing_characters>
                {formatted}
                </existing_characters>
                """;
    }

    public static string CharacterStateContext(CharacterContext context, bool ignoreNull = true)
    {
        var options = GetJsonOptions(ignoreNull);
        return $"""
                <previous_character_state>
                {JsonSerializer.Serialize(context.CharacterState, options)}
                </previous_character_state>
                <previous_tracker>
                {JsonSerializer.Serialize(context.CharacterTracker, options)}
                </previous_tracker>
                <previous_development>
                {JsonSerializer.Serialize(context.DevelopmentTracker, options)}
                </previous_development>
                """;
    }

    public static string RecentScenesForCharacter(SceneContext[] sceneContext, string mainCharacterName, string characterName, int count = 3)
    {
        var scenes = string.Join("\n\n---\n\n", sceneContext
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
                {JsonSerializer.Serialize(characters, GetJsonOptions(ignoreNull))}
                </created_characters>
                """;
    }

    public static string CharacterCreationContext<T>(T context, bool ignoreNull = false) =>
        $"""
         <character_creation_context>
         {JsonSerializer.Serialize(context, GetJsonOptions(ignoreNull))}
         </character_creation_context>
         """;

    public static string NewCharacterRequests<T>(IEnumerable<T>? requests, bool ignoreNull = false)
    {
        var list = requests?.ToArray() ?? [];
        return $"""
                <new_characters_requests>
                {JsonSerializer.Serialize(list, GetJsonOptions(ignoreNull))}
                </new_characters_requests>
                """;
    }

    // ===== Narrative Direction Sections =====

    public static string SceneDirection<T>(T direction, bool ignoreNull = false) =>
        $"""
         <scene_direction>
         {JsonSerializer.Serialize(direction, GetJsonOptions(ignoreNull))}
         </scene_direction>
         """;

    public static string ContinuityCheck<T>(T check, bool ignoreNull = false) =>
        $"""
         <continuity_check>
         {JsonSerializer.Serialize(check, GetJsonOptions(ignoreNull))}
         </continuity_check>
         """;

    public static string SceneMetadata<T>(T metadata, bool ignoreNull = false) =>
        $"""
         <scene_metadata>
         {JsonSerializer.Serialize(metadata, GetJsonOptions(ignoreNull))}
         </scene_metadata>
         """;

    public static string LastSceneNarrativeDirection<T>(T? direction, bool ignoreNull = false) where T : class
    {
        if (direction == null) return string.Empty;

        return $"""
                <last_scene_narrative_direction>
                {JsonSerializer.Serialize(direction, GetJsonOptions(ignoreNull))}
                </last_scene_narrative_direction>
                """;
    }

    public static string LastNarrativeDirections(SceneContext[] sceneContext, int count = 1)
    {
        var directions = string.Join("\n", sceneContext
            .OrderByDescending(y => y.SequenceNumber)
            .Take(count)
            .Select(z => JsonSerializer.Serialize(z.Metadata.NarrativeMetadata, JsonOptions)));

        return $"""
                <last_narrative_directions>
                {directions}
                </last_narrative_directions>
                """;
    }

    public static string InitialInstruction(string guidance) =>
        $"""
         This is the first scene of the adventure. Create the initial narrative direction based on the main character and adventure setup.
         <initial_instruction>
         {guidance}
         </initial_instruction>
         """;

    // ===== Location & Lore Sections =====

    public static string NewLocations<T>(T[]? locations, bool ignoreNull = false) =>
        $"""
         <new_locations>
         {JsonSerializer.Serialize(locations ?? [], GetJsonOptions(ignoreNull))}
         </new_locations>
         """;

    public static string NewLore<T>(T[]? lore, bool ignoreNull = false) =>
        $"""
         <new_lore>
         {JsonSerializer.Serialize(lore ?? [], GetJsonOptions(ignoreNull))}
         </new_lore>
         """;

    public static string LocationRequest<T>(T request, bool ignoreNull = false) =>
        $"""
         <location_request>
         {JsonSerializer.Serialize(request, GetJsonOptions(ignoreNull))}
         </location_request>
         """;

    public static string LoreCreationContext<T>(T context, bool ignoreNull = false) =>
        $"""
         <lore_creation_context>
         {JsonSerializer.Serialize(context, GetJsonOptions(ignoreNull))}
         </lore_creation_context>
         """;

    // ===== Context Sections =====

    public static string ExtraContext<T>(T? context, bool ignoreNull = false) where T : class
    {
        if (context == null) return string.Empty;

        return $"""
                <extra_context>
                {JsonSerializer.Serialize(context, GetJsonOptions(ignoreNull))}
                </extra_context>
                """;
    }

    public static string Context<T>(T? context, bool ignoreNull = false) where T : class
    {
        if (context == null) return string.Empty;

        return $"""
                <context>
                {JsonSerializer.Serialize(context, GetJsonOptions(ignoreNull))}
                </context>
                """;
    }

    // ===== Tracker Sections =====

    public static string PreviousTrackers(SceneContext[] sceneContext, int count = 1, bool ignoreNull = false)
    {
        var options = GetJsonOptions(ignoreNull);
        var formatted = string.Join("\n\n", sceneContext
            .OrderByDescending(x => x.SequenceNumber)
            .Where(x => x.Metadata.Tracker != null)
            .Take(count)
            .Select(s => JsonSerializer.Serialize(s.Metadata.Tracker, options)));

        return $"""
                <previous_trackers>
                {formatted}
                </previous_trackers>
                """;
    }

    public static string PreviousStoryTrackers(SceneContext[] sceneContext, int count = 1, bool ignoreNull = true)
    {
        var options = GetJsonOptions(ignoreNull);
        var formatted = string.Join("\n\n", sceneContext
            .OrderByDescending(x => x.SequenceNumber)
            .Where(x => x.Metadata.Tracker != null)
            .Take(count)
            .Select(s => JsonSerializer.Serialize(s.Metadata.Tracker?.Story, options)));

        return $"""
                <previous_trackers>
                {formatted}
                </previous_trackers>
                """;
    }

    public static string AdventureStartTime(string startTime) =>
        $"""
         <adventure_start_time>
         {startTime}
         </adventure_start_time>
         """;

    // ===== Generic Tagged Content =====

    public static string Tagged(string tag, string content) =>
        $"""
         <{tag}>
         {content}
         </{tag}>
         """;

    public static string TaggedJson<T>(string tag, T content, bool ignoreNull = false) =>
        $"""
         <{tag}>
         {JsonSerializer.Serialize(content, GetJsonOptions(ignoreNull))}
         </{tag}>
         """;
}
