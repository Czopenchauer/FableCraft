namespace FableCraft.Application.NarrativeEngine.Agents.Builders;

/// <summary>
///     Constants for prompt placeholder names used across agents.
///     All placeholders use double curly braces format: {{placeholder_name}}
/// </summary>
internal static class PlaceholderNames
{
    /// <summary>
    ///     Content policy configuration. Used by most agents.
    /// </summary>
    public const string ContentPolicy = "{{content_policy}}";

    /// <summary>
    ///     NPC character name injection. Used by character-related agents.
    /// </summary>
    public const string CharacterName = "{{CHARACTER_NAME}}";

    /// <summary>
    ///     Character tracker schema/format definition.
    /// </summary>
    public const string CharacterTrackerStructure = "{{character_tracker_structure}}";

    /// <summary>
    ///     Character tracker output template.
    /// </summary>
    public const string CharacterTrackerOutput = "{{character_tracker_output}}";

    /// <summary>
    ///     Main character tracker schema/format definition.
    /// </summary>
    public const string MainCharacterTrackerStructure = "{{main_character_tracker_structure}}";

    /// <summary>
    ///     Main character tracker output template.
    /// </summary>
    public const string MainCharacterTrackerOutput = "{{main_character_tracker_output}}";

    /// <summary>
    ///     Scene tracker schema/format definition.
    /// </summary>
    public const string SceneTrackerStructure = "{{scene_tracker_structure}}";

    /// <summary>
    ///     Scene tracker output template.
    /// </summary>
    public const string SceneTrackerOutput = "{{scene_tracker_output}}";

    /// <summary>
    ///     World setting configuration for chronicler context.
    /// </summary>
    public const string WorldSetting = "{{world_setting}}";

    /// <summary>
    ///     Scene bible with tone, themes, and content calibration.
    /// </summary>
    public const string StoryBible = "{{story_bible}}";

    public const string ProgressionSystem = "{{progression_system}}";

    /// <summary>
    ///     Skills schema definition extracted from the main character tracker structure.
    /// </summary>
    public const string SkillsSchema = "{{skills_schema}}";

    /// <summary>
    ///     Abilities schema definition extracted from the main character tracker structure.
    /// </summary>
    public const string AbilitiesSchema = "{{abilities_schema}}";

    /// <summary>
    ///     Carried (non-worn inventory) schema definition extracted from the main character tracker structure.
    /// </summary>
    public const string CarriedSchema = "{{carried_schema}}";

    /// <summary>
    ///     Assets (currency, property, debts) schema definition extracted from the main character tracker structure.
    /// </summary>
    public const string AssetsSchema = "{{assets_schema}}";

    /// <summary>
    ///     Full tracker definition schema used by the TrackerDeBloaterAgent.
    /// </summary>
    public const string TrackerDefinition = "{{tracker_definition}}";

    /// <summary>
    ///     Current tracker state JSON used by the TrackerDeBloaterAgent.
    /// </summary>
    public const string TrackerState = "{{tracker_state}}";
}