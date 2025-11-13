using System.Text.Json.Serialization;

namespace FableCraft.Application.NarrativeEngine.Models;

/// <summary>
///     Complete scene generation output matching required JSON structure
/// </summary>
public class SceneGenerationOutput
{
    [JsonPropertyName("scene")]
    public required SceneContent Scene { get; set; }

    [JsonPropertyName("narrative_updates")]
    public required NarrativeUpdates NarrativeUpdates { get; set; }

    [JsonPropertyName("new_entities")]
    public required NewEntities NewEntities { get; set; }

    [JsonPropertyName("character_updates")]
    public required List<CharacterUpdate> CharacterUpdates { get; set; }

    [JsonPropertyName("metadata")]
    public required SceneMetadata Metadata { get; set; }
}

public class SceneContent
{
    [JsonPropertyName("text")]
    public required string Text { get; set; }

    [JsonPropertyName("choices")]
    public required List<string> Choices { get; set; }
}

public class NarrativeUpdates
{
    [JsonPropertyName("objectives")]
    public required ObjectivesProgress Objectives { get; set; }

    [JsonPropertyName("new_plot_threads")]
    public required List<PlotThread> NewPlotThreads { get; set; }
}

public class ObjectivesProgress
{
    [JsonPropertyName("long_term")]
    public required List<ObjectiveStatus> LongTerm { get; set; }

    [JsonPropertyName("medium_term")]
    public required List<ObjectiveStatus> MediumTerm { get; set; }

    [JsonPropertyName("short_term")]
    public required List<ObjectiveStatus> ShortTerm { get; set; }
}

public class ObjectiveStatus
{
    [JsonPropertyName("goal")]
    public required string Goal { get; set; }

    [JsonPropertyName("progress_percentage")]
    public int ProgressPercentage { get; set; }

    [JsonPropertyName("status")]
    public required string Status { get; set; } // "active", "completed", "failed"
}

public class PlotThread
{
    [JsonPropertyName("description")]
    public required string Description { get; set; }

    [JsonPropertyName("tension_type")]
    public required string TensionType { get; set; } // "external_conflict", "character_conflict", "mystery_element"
}

public class NewEntities
{
    [JsonPropertyName("locations")]
    public required List<LocationEntity> Locations { get; set; }

    [JsonPropertyName("lore")]
    public required List<LoreItem> Lore { get; set; }

    [JsonPropertyName("items")]
    public required List<GeneratedItem> Items { get; set; }

    [JsonPropertyName("characters")]
    public required List<CharacterEntity> Characters { get; set; }
}

public class LocationEntity
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("description")]
    public required string Description { get; set; }

    [JsonPropertyName("relationships")]
    public required List<EntityRelationship> Relationships { get; set; }
}

public class EntityRelationship
{
    [JsonPropertyName("type")]
    public required string Type { get; set; } // "connected_to", "part_of", "near"

    [JsonPropertyName("target_entity")]
    public required string TargetEntity { get; set; }
}

public class LoreItem
{
    [JsonPropertyName("title")]
    public required string Title { get; set; }

    [JsonPropertyName("content")]
    public required string Content { get; set; }

    [JsonPropertyName("category")]
    public required string Category { get; set; } // "history", "culture", "magic", "faction", "technology"
}

public class GeneratedItem
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("description")]
    public required string Description { get; set; }
}

public class CharacterEntity
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("personality_traits")]
    public required List<string> PersonalityTraits { get; set; }

    [JsonPropertyName("description")]
    public required string Description { get; set; }

    [JsonPropertyName("background")]
    public required string Background { get; set; }

    [JsonPropertyName("speech_patterns")]
    public required string SpeechPatterns { get; set; }

    [JsonPropertyName("goals")]
    public required CharacterGoals Goals { get; set; }

    [JsonPropertyName("faction_affiliation")]
    public required string FactionAffiliation { get; set; }

    [JsonPropertyName("knowledge")]
    public required CharacterKnowledge Knowledge { get; set; }

    [JsonPropertyName("relationships")]
    public required List<CharacterRelationship> Relationships { get; set; }

    [JsonPropertyName("memories")]
    public required List<CharacterMemory> Memories { get; set; }
}

public class CharacterGoals
{
    [JsonPropertyName("immediate")]
    public required List<string> Immediate { get; set; }

    [JsonPropertyName("long_term")]
    public required List<string> LongTerm { get; set; }
}

public class CharacterKnowledge
{
    [JsonPropertyName("knows")]
    public required List<string> Knows { get; set; }
}

public class CharacterRelationship
{
    [JsonPropertyName("type")]
    public required string Type { get; set; } // "ally", "enemy", "family", "mentor", "rival"

    [JsonPropertyName("target_character_id")]
    public required string TargetCharacterId { get; set; }

    [JsonPropertyName("description")]
    public required string Description { get; set; }
}

public class CharacterMemory
{
    [JsonPropertyName("event")]
    public required string Event { get; set; }

    [JsonPropertyName("emotional_impact")]
    public required string EmotionalImpact { get; set; }
}

public class CharacterUpdate
{
    [JsonPropertyName("character_id")]
    public required string CharacterId { get; set; }

    [JsonPropertyName("emotional_state_change")]
    public string? EmotionalStateChange { get; set; }

    [JsonPropertyName("relationship_changes")]
    public required List<RelationshipChange> RelationshipChanges { get; set; }

    [JsonPropertyName("new_memories")]
    public required List<CharacterMemory> NewMemories { get; set; }

    [JsonPropertyName("goal_progress")]
    public GoalProgress? GoalProgress { get; set; }
}

public class RelationshipChange
{
    [JsonPropertyName("with_character_id")]
    public required string WithCharacterId { get; set; }

    [JsonPropertyName("change_description")]
    public required string ChangeDescription { get; set; }
}

public class GoalProgress
{
    [JsonPropertyName("goal")]
    public required string Goal { get; set; }

    [JsonPropertyName("progress")]
    public required string Progress { get; set; } // "advanced", "stalled", "completed", "abandoned"
}

public class SceneMetadata
{
    [JsonPropertyName("beat_type")]
    public required string BeatType { get; set; } // "trigger", "choice", "complication", "reflection"

    [JsonPropertyName("pacing")]
    public required string Pacing { get; set; } // "fast-action", "slow-reflection", "moderate"

    [JsonPropertyName("emotional_tone")]
    public required string EmotionalTone { get; set; } // "triumph", "tension", "humor", "intimacy", "suspense", "melancholy"

    [JsonPropertyName("narrative_arc_position")]
    public required string NarrativeArcPosition { get; set; } // "exposition", "rising_action", "climax", "falling_action", "resolution"
}