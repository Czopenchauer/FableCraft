using System.Text.Json;
using System.Text.Json.Serialization;

using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;

namespace FableCraft.Application.NarrativeEngine.Agents.Builders;

/// <summary>
///     Builds formatted prompt sections with XML tags for narrative engine agents
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

    public static JsonSerializerOptions GetJsonOptions(bool ignoreNull = false) => ignoreNull ? JsonOptionsIgnoreNull : JsonOptions;

    public static string SceneTracker(GenerationContext context, SceneTracker sceneTracker)
    {
        var previousTime = context.LatestTracker()?.Scene;
        return $"""
                Current Time, Location and general Scene information:
                <scene_tracker>
                Time: {sceneTracker.Time}
                Location: {sceneTracker.Location}
                Weather: {sceneTracker.Weather}
                </scene_tracker>

                Previous time: {previousTime?.Time}
                """;
    }

    public static string CurrentSceneTracker(GenerationContext context) =>
        context.LatestTracker()?.Scene != null
            ? $"""
               <current_scene_tracker>
               {context.LatestTracker()?.Scene.ToJsonString(GetJsonOptions())}
               </current_scene_tracker>
               """
            : string.Empty;

    public static string LastScenes(SceneContext[] sceneContext, int count)
    {
        if (sceneContext.Length == 0)
        {
            return string.Empty;
        }

        var scenes = sceneContext
            .OrderByDescending(x => x.SequenceNumber)
            .Take(count);

        // {x.PlayerChoice}
        var formatted = string.Join("\n",
            scenes
                .OrderBy(x => x.SequenceNumber)
                .Select(x => $"""
                              Time: {x.Metadata.Tracker!.Scene!.Time}
                              Location: {x.Metadata.Tracker.Scene.Location}
                              Weather: {x.Metadata.Tracker.Scene.Weather}
                              {x.SceneContent}
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
                .Skip(1)
                .Take(count)
                .OrderBy(x => x.SequenceNumber)
                .Select(s => $"""
                              SCENE NUMBER: {s.SequenceNumber}
                              {s.SceneContent}
                              """));

        return $"""
                <recent_scenes>
                {formatted}
                </recent_scenes>
                """;
    }

    public static string SceneContent(string? content) =>
        string.IsNullOrEmpty(content)
            ? string.Empty
            : $"""
               <scene_content>
               {content}
               </scene_content>
               """;

    public static string CurrentScene(GenerationContext context) =>
        string.IsNullOrEmpty(context.NewScene?.Scene)
            ? string.Empty
            : $"""
               Here's the current narrative scene written from {context.MainCharacter.Name}'s perspective:
               <current_scene>
               {context.NewScene.Scene}
               </current_scene>
               """;

    public static string PreviousScene(string? content) =>
        string.IsNullOrEmpty(content)
            ? string.Empty
            : $"""
               <previous_scene>
               {content}
               </previous_scene>
               """;

    public static string PlayerAction(string action) =>
        $"""
         <player_action>
         {action}
         </player_action>
         """;

    public static string MainCharacter(GenerationContext context)
    {
        var tracker = context.LatestTracker()?.MainCharacter?.MainCharacter;
        return $"""
                <main_character>
                Name: {context.MainCharacter.Name}
                Appearance: {tracker?.Appearance}
                GeneralBuild: {tracker?.GeneralBuild}
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

        var tracker = sceneContext
            .OrderByDescending(x => x.SequenceNumber)
            .FirstOrDefault(x => x.Metadata.Tracker != null)?.Metadata.Tracker;

        var options = GetJsonOptions(ignoreNull);
        return $"""
                Current tracker:
                <main_character_tracker>
                {tracker!.MainCharacter.ToJsonString(options)}
                </main_character_tracker>
                """;
    }

    public static string ExistingCharacters(IEnumerable<CharacterContext> characters)
    {
        var formatted = string.Join("\n\n",
            characters.Select(c => $"""
                                    <character>
                                    Name: {c.Name}
                                    Location: {c.CharacterTracker?.Location}
                                    Appearance: {c.CharacterTracker?.Appearance}
                                    GeneralBuild: {c.CharacterTracker?.GeneralBuild}
                                    {c.Description}
                                    </character>
                                    """));

        return $"""
                List of existing characters in the story:
                <existing_characters>
                {formatted}
                </existing_characters>
                """;
    }

    /// <summary>
    ///     Returns a minimal list of characters with only name and current location.
    ///     Use fetch_character_details tool to get full character information.
    /// </summary>
    public static string ExistingCharactersMinimal(IEnumerable<CharacterContext> characters)
    {
        var formatted = string.Join("\n",
            characters.Select(c => $"- {c.Name} (Location: {c.CharacterTracker?.Location ?? "unknown"})"));

        return $"""
                List of existing characters in the story (use fetch_character_details tool to get full details):
                <existing_characters>
                {formatted}
                </existing_characters>
                """;
    }

    public static string CharacterForEmulation(IEnumerable<CharacterContext> characters, GenerationContext context)
    {
        var names = string.Join("\n- ",
            characters.Select(c => c.Name));

        var profiles = string.Join("\n",
            characters.Select(c =>
                $"""
                 <profile name="{c.Name}">
                 Appearance: {c.CharacterTracker?.Appearance}
                 GeneralBuild: {c.CharacterTracker?.GeneralBuild}
                 Location: {c.CharacterTracker?.Location ?? "unknown"}
                 {c.Description}
                 </profile>
                 """
            ));

        return $"""
                ## Profiled Characters for Emulation

                The following characters have full profiles. You MUST call `emulate_character_action()` before writing ANY speech, action, or reaction from these characters.

                **EMULATION LIST (call function for these):**{names}

                ---

                <character_profiles>
                {profiles}
                </character_profiles>
                """;
    }

    public static string CharacterStateContext(CharacterContext context, bool ignoreNull = true)
    {
        var options = GetJsonOptions(ignoreNull);
        return $"""
                Current state tracker for the character {context.Name}:
                <character_state>
                {context.CharacterState.ToJsonString(options)}
                </character_state>
                <tracker>
                {context.CharacterTracker.ToJsonString(options)}
                </tracker>
                """;
    }

    public static string RecentScenesForCharacter(CharacterContext context, int count = 5)
    {
        var scenes = string.Join("\n\n---\n\n",
            context.SceneRewrites
                .OrderByDescending(x => x.SequenceNumber)
                .Take(count)
                .OrderBy(x => x.SequenceNumber)
                .Select(s => $"""
                              SCENE NUMBER: {s.SequenceNumber}
                              TIME: {s.SceneTracker?.Time}
                              Location: {s.SceneTracker?.Location}

                              {s.Content}
                              """));

        return $"""
                Recent scenes involving the character {context.Name} from their perspective:
                <recent_scenes>
                {scenes}
                </recent_scenes>
                """;
    }

    public static string NewCharacters(List<CharacterContext> characters)
    {
        if (characters == null || characters.Count == 0) return string.Empty;

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

    public static string CharacterCreationContext<T>(T context, bool ignoreNull = false) =>
        $"""
         <character_creation_context>
         {context.ToJsonString(GetJsonOptions(ignoreNull))}
         </character_creation_context>
         """;

    public static string NewCharacterRequests<T>(IEnumerable<T>? requests, bool ignoreNull = false)
    {
        if (requests == null)
        {
            return string.Empty;
        }

        var list = requests?.ToArray() ?? [];
        return $"""
                <new_characters_requests>
                {list.ToJsonString(GetJsonOptions(ignoreNull))}
                </new_characters_requests>
                """;
    }

    public static string ActionResolution(GenerationContext context) =>
        $"""
         <action_resolution>
         {context.NewResolution.ToJsonString()}
         </action_resolution>
         """;

    public static string ResolutionOutput(string? resolution)
    {
        if (string.IsNullOrEmpty(resolution)) return string.Empty;

        return $"""
                <resolution_output>
                {resolution}
                </resolution_output>
                """;
    }

    public static string InitialInstruction(string guidance) =>
        $"""
         This is the first scene of the adventure. Create the initial narrative direction based on the main character and adventure setup. Here's what the player provided as guidance for the first scene:
         <initial_instruction>
         {guidance}
         </initial_instruction>

         Generate the first narrative direction for the story based on the above information.
         """;

    /// <summary>
    ///     Formats extra lore entries that were added during adventure creation.
    ///     These provide additional world context for the first scene.
    /// </summary>
    public static string ExtraLoreEntries(List<ExtraLoreContext> extraLoreEntries)
    {
        if (extraLoreEntries == null || extraLoreEntries.Count == 0)
        {
            return string.Empty;
        }

        var formattedEntries = string.Join("\n\n",
            extraLoreEntries.Select(e => $"""
                                          <extra_lore category="{e.Category}">
                                          **{e.Title}**
                                          {e.Content}
                                          </extra_lore>
                                          """));

        return $"""
                The following additional lore was specified for this adventure. Use this information to enrich the first scene:
                <extra_lore_entries>
                {formattedEntries}
                </extra_lore_entries>
                """;
    }

    public static string NewLocations<T>(T[]? locations, bool ignoreNull = false)
    {
        if (locations == null || locations.Length == 0) return string.Empty;

        return $"""
                <new_locations>
                {(locations ?? []).ToJsonString(GetJsonOptions(ignoreNull))}
                </new_locations>
                """;
    }

    public static string LocationRequest<T>(T request, bool ignoreNull = false) =>
        $"""
         <location_request>
         {request.ToJsonString(GetJsonOptions(ignoreNull))}
         </location_request>
         """;

    public static string LoreCreationContext<T>(T context, bool ignoreNull = false) =>
        $"""
         <lore_creation_context>
         {context.ToJsonString(GetJsonOptions(ignoreNull))}
         </lore_creation_context>
         """;

    public static string ItemRequest<T>(T request, bool ignoreNull = false) =>
        $"""
         <item_request>
         {request.ToJsonString(GetJsonOptions(ignoreNull))}
         </item_request>
         """;

    public static string NewItems(GeneratedItem[]? items, bool ignoreNull = false)
    {
        if (items == null || items.Length == 0) return string.Empty;

        return $"""
                <new_items>
                {(items ?? []).ToJsonString(GetJsonOptions(ignoreNull))}
                </new_items>
                """;
    }

    public static string PreviouslyCreatedContent(GenerationContext context)
    {
        var hasLore = context.PreviouslyGeneratedLore.Length > 0;
        var hasItems = context.PreviouslyGeneratedItems.Length > 0;

        if (!hasLore && !hasItems)
        {
            return string.Empty;
        }

        var loreContent = hasLore
            ? string.Join("\n", context.PreviouslyGeneratedLore.Select(x => $"- {x.Content}"))
            : "None";

        var itemContent = hasItems
            ? string.Join("\n", context.PreviouslyGeneratedItems.Select(x => $"- {x.Content}"))
            : "None";

        return $"""
                <previously_created_content>
                **Previously Created Lore**:
                {loreContent}

                **Previously Created Items**:
                {itemContent}
                </previously_created_content>
                """;
    }

    public static string Context(GenerationContext generationContext)
    {
        var context = generationContext.SceneContext.OrderByDescending(x => x.SequenceNumber).FirstOrDefault()?.Metadata.GatheredContext;
        if (context == null)
        {
            return string.Empty;
        }

        var worldContext = context.WorldContext.Length > 0
            ? string.Join("\n", context.WorldContext.Select(c => $"- **{c.Topic}**: {c.Content}"))
            : "No world context available.";

        var narrativeContext = context.NarrativeContext.Length > 0
            ? string.Join("\n", context.NarrativeContext.Select(c => $"- **{c.Topic}**: {c.Content}"))
            : "No narrative context available.";

        var loreContent = generationContext.PreviouslyGeneratedLore.Length > 0
            ? string.Join("\n", generationContext.PreviouslyGeneratedLore.Select(x => $"- {x.Content}"))
            : "None";

        var locationContent = generationContext.PreviouslyGeneratedLocations.Length > 0
            ? string.Join("\n", generationContext.PreviouslyGeneratedLocations.Select(x => $"- {x.Content}"))
            : "None";

        var itemContent = generationContext.PreviouslyGeneratedItems.Length > 0
            ? string.Join("\n", generationContext.PreviouslyGeneratedItems.Select(x => $"- {x.Content}"))
            : "None";

        return $"""
                <knowledge_graph_context>
                **World Knowledge** (locations, lore, items, events):
                {worldContext}

                **Narrative Knowledge** (main character memories, goals, relationships):
                {narrativeContext}

                **Recently Created Lore**:
                {loreContent}

                **Recently Created Locations**:
                {locationContent}

                **Recently Created Items**:
                {itemContent}
                </knowledge_graph_context>
                """;
    }

    /// <summary>
    ///     Gets the gathered context from the previous scene's metadata.
    ///     Used when ContextGatherer runs after scene generation.
    /// </summary>
    public static string PreviousSceneGatheredContext(GenerationContext generationContext)
    {
        var gatheredContext = generationContext.SceneContext
            .OrderByDescending(x => x.SequenceNumber)
            .FirstOrDefault()?.Metadata.GatheredContext;

        if (gatheredContext == null)
        {
            return string.Empty;
        }

        var worldContext = gatheredContext.WorldContext.Length > 0
            ? string.Join("\n", gatheredContext.WorldContext.Select(c => $"- **{c.Topic}**: {c.Content}"))
            : "No world context available.";

        var narrativeContext = gatheredContext.NarrativeContext.Length > 0
            ? string.Join("\n", gatheredContext.NarrativeContext.Select(c => $"- **{c.Topic}**: {c.Content}"))
            : "No narrative context available.";

        return $"""
                <previous_gathered_context>
                **World Knowledge**:
                {worldContext}

                **Narrative Knowledge**:
                {narrativeContext}

                **Additional Data**:
                {gatheredContext.AdditionalProperties.ToJsonString()}
                </previous_gathered_context>
                """;
    }

    public static string PreviousSceneTrackers(SceneContext[] sceneContext, int count = 1, bool ignoreNull = true)
    {
        var options = GetJsonOptions(ignoreNull);
        var formatted = string.Join("\n\n",
            sceneContext
                .OrderByDescending(x => x.SequenceNumber)
                .Take(count)
                .Select(s => s.Metadata.Tracker?.Scene.ToJsonString(options)));

        return $"""
                Here are the scene trackers from previous scene. Update their information based on the current scene:
                <previous_scene_trackers>
                {formatted}
                </previous_scene_trackers>
                """;
    }

    public static string AdventureStartTime(string startTime) =>
        $"""
         Set the initial time to:
         <adventure_start_time>
         {startTime}
         </adventure_start_time>
         """;

    public static string WorldSettings(string promptPath)
    {
        var worldSettingsPath = Path.Combine(promptPath, "WorldSettings.md");
        var worldSettings = File.Exists(worldSettingsPath) ? File.ReadAllText(worldSettingsPath) : null;

        if (string.IsNullOrWhiteSpace(worldSettings))
        {
            return string.Empty;
        }

        return $"""
                The following world settings define the rules, systems, and context of this adventure's world:
                <world_settings>
                {worldSettings}
                </world_settings>
                """;
    }

    public static string PreviousCharacterObservations(SceneContext[] sceneContext)
    {
        var observations = sceneContext
            .OrderByDescending(x => x.SequenceNumber)
            .FirstOrDefault()?.Metadata.WriterObservation;

        if (observations == null)
        {
            return string.Empty;
        }

        return $"""
                <previous_character_observations>
                Observations from the previous scene generation:
                {observations.ToJsonString()}
                </previous_character_observations>
                """;
    }

    /// <summary>
    ///     Formats background character profiles from the previous scene for the Writer agent.
    ///     These characters have partial profiles and should be written directly without emulation.
    /// </summary>
    public static string BackgroundCharacterProfiles(List<BackgroundCharacter> previousBackgroundCharacters)
    {
        if (previousBackgroundCharacters == null || previousBackgroundCharacters.Count == 0)
        {
            return string.Empty;
        }

        var formattedCharacters = string.Join("\n",
            previousBackgroundCharacters.Select(c => $"""
                                                      <background_character name="{c.Name}">
                                                      {string.Join("\n", c.ToJsonString())}
                                                      </background_character>
                                                      """));

        return $"""
                ## Previously Established Background Characters

                The following background characters have partial profiles from previous scenes.

                <background_characters>
                {formattedCharacters}
                </background_characters>
                """;
    }

    /// <summary>
    ///     Formats writer guidance from the Chronicler agent for the Writer agent.
    ///     Retrieves the guidance from the previous scene's metadata.
    /// </summary>
    public static string ChroniclerGuidance(SceneContext[] sceneContext)
    {
        var writerGuidance = sceneContext
            .OrderByDescending(x => x.SequenceNumber)
            .FirstOrDefault()?.Metadata.WriterGuidance;

        if (string.IsNullOrEmpty(writerGuidance))
        {
            return string.Empty;
        }

        return $"""
                <chronicler_guidance>
                The Chronicler has analyzed the story's narrative fabric and provides the following guidance for this scene:
                {writerGuidance}
                </chronicler_guidance>
                """;
    }

    /// <summary>
    ///     Formats character emulation outputs for the CharacterReflectionAgent.
    ///     These reveal the character's internal experience during the scene.
    /// </summary>
    public static string CharacterEmulationOutputs(GenerationContext context, string characterName)
    {
        if (!context.CharacterEmulationOutputs.TryGetValue(characterName, out var outputs) || outputs.Count == 0)
        {
            return string.Empty;
        }

        var formatted = string.Join("\n\n---\n\n",
            outputs.OrderBy(o => o.SequenceNumber).Select(o => $"""
                <emulation sequence="{o.SequenceNumber}">
                **Situation:** {o.Stimulus}
                **Query:** {o.Query}

                {o.Response}
                </emulation>
                """));

        return $"""
                ## Your Internal Experience During This Scene

                The following are your internal responses as generated during the scene.
                These reveal your actual thoughts, feelings, and reactions that informed your actions.

                <character_emulation_outputs>
                {formatted}
                </character_emulation_outputs>
                """;
    }
}