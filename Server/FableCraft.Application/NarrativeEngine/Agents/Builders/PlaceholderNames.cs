namespace FableCraft.Application.NarrativeEngine.Agents.Builders;

/// <summary>
/// Constants for prompt placeholder names used across agents.
/// All placeholders use double curly braces format: {{placeholder_name}}
/// </summary>
internal static class PlaceholderNames
{
    /// <summary>
    /// Content policy configuration. Used by most agents.
    /// </summary>
    public const string Jailbreak = "{{jailbreak}}";

    /// <summary>
    /// NPC character name injection. Used by character-related agents.
    /// </summary>
    public const string CharacterName = "{{CHARACTER_NAME}}";

    /// <summary>
    /// Character tracker schema/format definition.
    /// </summary>
    public const string CharacterTrackerStructure = "{{character_tracker_structure}}";

    /// <summary>
    /// Character tracker output template.
    /// </summary>
    public const string CharacterTrackerOutput = "{{character_tracker_output}}";

    /// <summary>
    /// Character development schema/format definition.
    /// </summary>
    public const string CharacterDevelopmentStructure = "{{character_development_structure}}";

    /// <summary>
    /// Character development output template.
    /// </summary>
    public const string CharacterDevelopmentOutput = "{{character_development_output}}";

    /// <summary>
    /// Main character tracker schema/format definition.
    /// </summary>
    public const string MainCharacterTrackerStructure = "{{main_character_tracker_structure}}";

    /// <summary>
    /// Main character tracker output template.
    /// </summary>
    public const string MainCharacterTrackerOutput = "{{main_character_tracker_output}}";

    /// <summary>
    /// Main character development output template.
    /// </summary>
    public const string MainCharacterDevelopmentOutput = "{{main_character_development_output}}";

    /// <summary>
    /// Story tracker schema/format definition.
    /// </summary>
    public const string StoryTrackerStructure = "{{story_tracker_structure}}";

    /// <summary>
    /// Story tracker output template.
    /// </summary>
    public const string StoryTrackerOutput = "{{story_tracker_output}}";
}
