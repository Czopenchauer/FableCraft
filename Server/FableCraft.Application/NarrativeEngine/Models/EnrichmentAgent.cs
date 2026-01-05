namespace FableCraft.Application.NarrativeEngine.Models;

/// <summary>
/// Represents the agents that can be regenerated during enrichment.
/// </summary>
public enum EnrichmentAgent
{
    // Content generation agents
    CharacterCrafter,
    LoreCrafter,
    LocationCrafter,
    ItemCrafter,

    // Tracker agents
    StoryTracker,
    MainCharacterTracker,

    // All updates for side characters, state, tracker are treated atomically. They cannot be partially regenerated.
    CharacterTracker
}

internal enum LorebookCategory
{
    Location,
    Item,
    Lore,
    WorldEvent
}