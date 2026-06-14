namespace FableCraft.Application.NarrativeEngine.Models;

/// <summary>
///     Represents the agents that can be regenerated during enrichment.
/// </summary>
public enum EnrichmentAgent
{
    // Content generation agents
    CharacterCrafter,
    LoreCrafter,
    LocationCrafter,
    ItemCrafter,

    // Tracker agents
    SceneTracker,
    MainCharacterTracker,

    // Main character delta agents. Selecting these also forces MainCharacterTracker to re-derive,
    // so the regenerated delta can be merged onto a clean base instead of doubling onto the saved tracker.
    InventoryTracker,
    Progression,

    // All updates for side characters, state, tracker are treated atomically. They cannot be partially regenerated.
    CharacterTracker,

    // Simulation and story tracking - skipped during regeneration unless explicitly selected
    Simulation,
    Chronicler,
    NarrativeCatalyst,
    ContextGatherer,
    WorldInfoExtractor,
    CoLocation
}

internal enum LorebookCategory
{
    Location,
    Item,
    Lore,
    WorldEvent,
    BackgroundCharacter,
    Activity,
    ExtractedWorldFact
}