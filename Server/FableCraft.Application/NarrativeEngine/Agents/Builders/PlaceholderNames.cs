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
    public const string Jailbreak = "{{jailbreak}}";

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

    public const string IdentitySchema = "{{identity_schema}}";

    public const string RelationshipSchema = "{{relationship_schema}}";
}