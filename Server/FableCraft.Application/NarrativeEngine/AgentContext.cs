using System.Text.Json.Serialization;

namespace FableCraft.Application.NarrativeEngine;

internal sealed class NarrativeContext
{
    [JsonIgnore]
    public string AdventureId { get; set; }

    [JsonPropertyName("metadata")]
    public Metadata Metadata { get; set; }

    [JsonPropertyName("story_summary")]
    public string StorySummary { get; set; }

    [JsonPropertyName("scene_context")]
    public List<SceneContext> SceneContext { get; set; }

    [JsonPropertyName("tracker")]
    public CurrentSceneTracker Tracker { get; set; }

    [JsonPropertyName("characters")]
    public List<Character> Characters { get; set; }

    [JsonPropertyName("narrative_state")]
    public NarrativeState NarrativeState { get; set; }

    [JsonPropertyName("style_and_tone")]
    public StyleAndTone StyleAndTone { get; set; }
}

internal sealed class CurrentSceneTracker
{
    // Current scene time in ISO 8601 format
    public DateTime DateTime { get; set; }

    public string Location { get; set; }

    public string Weather { get; set; }

    public List<CharacterSceneTracker> CharacterPresent { get; set; }

    // Main character tracker
    public Dictionary<string, string> Tracker { get; set; }
}

internal sealed class CharacterSceneTracker
{
    public string CharacterName { get; set; }

    // Only set for complex character
    public Dictionary<string, string>? Tracker { get; set; }

    // Is the character complex (has detailed emotional and physical states, relationships, goals, memories) and should be simulated by separate character agent
    public bool ComplexCharacter { get; set; }
}

internal sealed class SceneContext
{
    [JsonPropertyName("scene_content")]
    public string SceneContent { get; set; }

    [JsonPropertyName("player_choice")]
    public string PlayerChoice { get; set; }
}

internal sealed class Character
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("current_location")]
    public string CurrentLocation { get; set; }

    [JsonPropertyName("emotional_state")]
    public EmotionalState EmotionalState { get; set; }

    [JsonPropertyName("physical_state")]
    public PhysicalState PhysicalState { get; set; }

    [JsonPropertyName("knowledge")]
    public List<Knowledge> Knowledge { get; set; }

    [JsonPropertyName("relationships")]
    public List<Relationship> Relationships { get; set; }

    [JsonPropertyName("goals")]
    public Goals Goals { get; set; }

    [JsonPropertyName("memories")]
    public Memories Memories { get; set; }
}

internal sealed class EmotionalState
{
    [JsonPropertyName("primary_emotion")]
    public string PrimaryEmotion { get; set; }

    [JsonPropertyName("intensity")]
    public int Intensity { get; set; }

    [JsonPropertyName("toward")]
    public string Toward { get; set; }
}

internal sealed class PhysicalState
{
    [JsonPropertyName("health")]
    public int Health { get; set; }

    [JsonPropertyName("energy")]
    public int Energy { get; set; }

    [JsonPropertyName("status_effects")]
    public List<string> StatusEffects { get; set; }
}

internal sealed class Knowledge
{
    [JsonPropertyName("description")]
    public string Description { get; set; }
}

internal sealed class Relationship
{
    [JsonPropertyName("character_id")]
    public string CharacterId { get; set; }

    [JsonPropertyName("relationship_type")]
    public string RelationshipType { get; set; }

    [JsonPropertyName("trust_level")]
    public int TrustLevel { get; set; }

    [JsonPropertyName("recent_interactions")]
    public List<string> RecentInteractions { get; set; }
}

internal sealed class Goals
{
    [JsonPropertyName("active_goal_id")]
    public string ActiveGoalId { get; set; }

    [JsonPropertyName("motivation")]
    public string Motivation { get; set; }
}

internal sealed class Memories
{
    [JsonPropertyName("episodic")]
    public List<EpisodicMemory> Episodic { get; set; }

    [JsonPropertyName("reflective_summary")]
    public string ReflectiveSummary { get; set; }
}

internal sealed class EpisodicMemory
{
    [JsonPropertyName("scene_id")]
    public string SceneId { get; set; }

    [JsonPropertyName("summary")]
    public string Summary { get; set; }

    [JsonPropertyName("emotional_impact")]
    public int EmotionalImpact { get; set; }

    [JsonPropertyName("importance")]
    public int Importance { get; set; }
}

internal sealed class NarrativeState
{
    [JsonPropertyName("objectives")]
    public Objectives Objectives { get; set; }

    [JsonPropertyName("story_arc")]
    public StoryArc StoryArc { get; set; }

    [JsonPropertyName("pacing")]
    public Pacing Pacing { get; set; }

    [JsonPropertyName("tension")]
    public Tension Tension { get; set; }
}

internal sealed class Objectives
{
    [JsonPropertyName("long_term")]
    public List<LongTermObjective> LongTerm { get; set; }

    [JsonPropertyName("medium_term")]
    public List<MediumTermObjective> MediumTerm { get; set; }

    [JsonPropertyName("short_term")]
    public List<ShortTermObjective> ShortTerm { get; set; }
}

internal sealed class LongTermObjective
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonPropertyName("progress")]
    public int Progress { get; set; }

    [JsonPropertyName("introduced_at_scene_id")]
    public string IntroducedAtSceneId { get; set; }
}

internal sealed class MediumTermObjective
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonPropertyName("progress")]
    public int Progress { get; set; }

    [JsonPropertyName("parent_objective_id")]
    public string ParentObjectiveId { get; set; }
}

internal sealed class ShortTermObjective
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonPropertyName("progress")]
    public int Progress { get; set; }

    [JsonPropertyName("must_resolve_by_scene")]
    public int? MustResolveByScene { get; set; }
}

internal sealed class StoryArc
{
    [JsonPropertyName("current_position")]
    public string CurrentPosition { get; set; }

    [JsonPropertyName("current_beat_type")]
    public string CurrentBeatType { get; set; }

    [JsonPropertyName("arc_progress")]
    public int ArcProgress { get; set; }
}

internal sealed class Pacing
{
    [JsonPropertyName("current_intensity")]
    public int CurrentIntensity { get; set; }

    [JsonPropertyName("recent_scene_pacing")]
    public List<ScenePacing> RecentScenePacing { get; set; }

    [JsonPropertyName("recommended_next_pacing")]
    public string RecommendedNextPacing { get; set; }
}

internal sealed class ScenePacing
{
    [JsonPropertyName("scene_id")]
    public string SceneId { get; set; }

    [JsonPropertyName("intensity")]
    public int Intensity { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; }
}

internal sealed class Tension
{
    [JsonPropertyName("overall_tension")]
    public int OverallTension { get; set; }

    [JsonPropertyName("tension_sources")]
    public List<TensionSource> TensionSources { get; set; }

    [JsonPropertyName("unresolved_cliffhangers")]
    public List<string> UnresolvedCliffhangers { get; set; }
}

internal sealed class TensionSource
{
    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonPropertyName("intensity")]
    public int Intensity { get; set; }
}

internal sealed class StyleAndTone
{
    [JsonPropertyName("narrative_voice")]
    public string NarrativeVoice { get; set; }

    [JsonPropertyName("genre")]
    public string Genre { get; set; }

    [JsonPropertyName("tone")]
    public string Tone { get; set; }

    [JsonPropertyName("themes")]
    public List<string> Themes { get; set; }

    [JsonPropertyName("writing_style_notes")]
    public string WritingStyleNotes { get; set; }
}

internal sealed class Metadata
{
    [JsonPropertyName("total_scenes_generated")]
    public int TotalScenesGenerated { get; set; }

    [JsonPropertyName("story_duration")]
    public string StoryDuration { get; set; }
}