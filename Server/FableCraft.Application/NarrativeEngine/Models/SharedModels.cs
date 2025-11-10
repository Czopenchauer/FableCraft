namespace FableCraft.Application.NarrativeEngine.Models;

/// <summary>
/// Represents a scene in the narrative history
/// </summary>
public class SceneContext
{
    public required string SceneId { get; set; }
    public required string Summary { get; set; }
    public required List<string> KeyEvents { get; set; }
    public required Dictionary<string, string> CharacterDevelopments { get; set; }
    public required List<string> LocationChanges { get; set; }
    public DateTime Timestamp { get; set; }
    public required string NarrativeArcPosition { get; set; }
}

/// <summary>
/// Story beat with progress tracking
/// </summary>
public class StoryBeat
{
    public required string BeatId { get; set; }
    public required string Tier { get; set; } // "Long", "Medium", "Short"
    public required string Description { get; set; }
    public required List<string> Objectives { get; set; }
    public double Progress { get; set; }
    public bool IsCompleted { get; set; }
    public required List<string> Dependencies { get; set; }
}

/// <summary>
/// Narrative pacing history
/// </summary>
public class PacingHistory
{
    public required List<string> RecentBeatTypes { get; set; }
    public double CurrentTension { get; set; }
    public int ScenesSinceLastClimactic { get; set; }
}

/// <summary>
/// World entity base
/// </summary>
public class WorldEntity
{
    public required string EntityId { get; set; }
    public required string EntityType { get; set; } // "Location", "Item", "Lore", "Character"
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required Dictionary<string, string> Attributes { get; set; }
    public required List<string> RelatedEntities { get; set; }
}

/// <summary>
/// Location entity
/// </summary>
public class Location : WorldEntity
{
    public required string RegionType { get; set; }
    public required List<string> ConnectedLocations { get; set; }
    public required string AtmosphericDescription { get; set; }
}

/// <summary>
/// Lore entity
/// </summary>
public class LoreEntity : WorldEntity
{
    public required string Category { get; set; }
    public required List<string> HistoricalEvents { get; set; }
    public required List<string> AffectedFactions { get; set; }
}

/// <summary>
/// Item entity
/// </summary>
public class ItemEntity : WorldEntity
{
    public required string ItemType { get; set; }
    public required Dictionary<string, string> Properties { get; set; }
    public required string MagicTechLevel { get; set; }
}

/// <summary>
/// Character profile
/// </summary>
public class CharacterProfile : WorldEntity
{
    public required string Personality { get; set; }
    public required List<string> Goals { get; set; }
    public required string SpeechPattern { get; set; }
    public required string Background { get; set; }
    public required string Faction { get; set; }
    public required Dictionary<string, string> Relationships { get; set; }
    public required List<string> KnowledgeBoundaries { get; set; }
    public required List<string> Memories { get; set; }
    public required string EmotionalState { get; set; }
}

/// <summary>
/// Agent message in the group chat
/// </summary>
public class AgentMessage
{
    public required string AgentId { get; set; }
    public required string AgentRole { get; set; }
    public required string Content { get; set; }
    public required string MessageType { get; set; } // "Request", "Response", "Handoff", "Approval"
    public required Dictionary<string, object> Metadata { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Scene specification from Story Weaver
/// </summary>
public class SceneSpecification
{
    public required string BeatType { get; set; }
    public required string Pacing { get; set; }
    public required List<string> ObjectivesToAdvance { get; set; }
    public required List<string> RequiredLocations { get; set; }
    public required List<string> RequiredItems { get; set; }
    public required List<string> RequiredCharacters { get; set; }
    public required string SceneGoal { get; set; }
}

/// <summary>
/// Final scene output
/// </summary>
public class SceneOutput
{
    public required string SceneId { get; set; }
    public required string Prose { get; set; }
    public required List<PlayerChoice> PlayerChoices { get; set; }
    public required List<WorldEntity> NewEntities { get; set; }
    public required Dictionary<string, object> CharacterUpdates { get; set; }
    public required Dictionary<string, object> Metadata { get; set; }
    public required string NarrativeArcPosition { get; set; }
    public required List<string> ObjectivesAdvanced { get; set; }
    public required List<string> NewPlotThreads { get; set; }
}

/// <summary>
/// Player choice
/// </summary>
public class PlayerChoice
{
    public required string ChoiceId { get; set; }
    public required string ChoiceText { get; set; }
    public required List<string> PotentialConsequences { get; set; }
}

/// <summary>
/// QA validation result
/// </summary>
public class QAValidationResult
{
    public required bool IsApproved { get; set; }
    public required List<string> Issues { get; set; }
    public required List<string> Suggestions { get; set; }
    public required Dictionary<string, bool> FactCheckResults { get; set; }
    public required List<string> CriticalErrors { get; set; }
}

/// <summary>
/// Character roleplay response
/// </summary>
public class RoleplayResponse
{
    public required string CharacterId { get; set; }
    public required string NarrationText { get; set; }
    public required string Dialogue { get; set; }
    public required string PhysicalAction { get; set; }
    public required string UpdatedEmotionalState { get; set; }
    public required List<string> NewMemories { get; set; }
    public required Dictionary<string, string> RelationshipChanges { get; set; }
}
