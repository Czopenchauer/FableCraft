using System.Text.Json.Serialization;

namespace FableCraft.Infrastructure.Persistence.Entities.Adventure;

public sealed class CharacterState
{
    public Guid Id { get; set; }

    public Guid CharacterId { get; set; }

    public Guid SceneId { get; set; }

    public Scene Scene { get; set; } = null!;

    public int SequenceNumber { get; set; }

    public required string Description { get; set; }

    public CharacterStats CharacterStats { get; init; } = null!;

    public CharacterTracker Tracker { get; init; } = null!;

    public SimulationMetadata? SimulationMetadata { get; init; }
}

/// <summary>
/// Tracks simulation-related data for a character.
/// </summary>
public class SimulationMetadata
{
    /// <summary>
    /// In-world timestamp of last simulation (e.g., "14:00 05-06-845").
    /// </summary>
    [JsonPropertyName("last_simulated")]
    public required string? LastSimulated { get; set; }

    /// <summary>
    /// Intended interactions with other characters from previous simulation.
    /// </summary>
    [JsonPropertyName("potential_interactions")]
    public required List<PotentialInteraction>? PotentialInteractions { get; set; }

    /// <summary>
    /// If the character has decided to seek out the MC.
    /// </summary>
    [JsonPropertyName("pending_mc_interaction")]
    public required PendingMcInteraction? PendingMcInteraction { get; set; }
}

/// <summary>
/// An intended interaction with another character, output from simulation.
/// </summary>
public class PotentialInteraction
{
    /// <summary>
    /// Name of the character to interact with.
    /// </summary>
    [JsonPropertyName("target_character")]
    public required string TargetCharacter { get; set; }

    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }
}

/// <summary>
/// Information about a character's intent to seek out the MC.
/// </summary>
public class PendingMcInteraction
{
    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }
}

public class CharacterStats
{
    [JsonPropertyName("character_identity")]
    public required CharacterIdentity CharacterIdentity { get; set; }

    [JsonPropertyName("goals_and_motivations")]
    public required object? Goals { get; set; }

    [JsonPropertyName("routine")]
    public required object? Routine { get; set; }

    [JsonExtensionData]
    public IDictionary<string, object>? ExtensionData { get; set; }
}

public class CharacterIdentity
{
    [JsonPropertyName("full_name")]
    public string? FullName { get; set; }

    [JsonExtensionData]
    public IDictionary<string, object>? ExtensionData { get; set; }
}