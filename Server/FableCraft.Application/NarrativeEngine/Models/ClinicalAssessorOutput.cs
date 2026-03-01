using System.Text.Json.Serialization;

using FableCraft.Infrastructure.Persistence.Entities.Adventure;

namespace FableCraft.Application.NarrativeEngine.Models;

/// <summary>
///     Output from the Clinical Assessor Agent (Pass 2 of character reflection).
///     Produces identity updates and relationship updates in clinical third-person language.
///     Does NOT produce scene_rewrite (that comes from Pass 1: ExperientialNarratorAgent).
/// </summary>
public sealed class ClinicalAssessorOutput
{
    /// <summary>
    ///     Complete current-state identity snapshot, or null if no identity changes.
    /// </summary>
    [JsonPropertyName("identity")]
    public CharacterStats? Identity { get; set; }

    /// <summary>
    ///     Complete current-state relationship snapshots for relationships that changed.
    ///     Empty array if no relationship changes.
    /// </summary>
    [JsonPropertyName("relationships")]
    public ClinicalRelationshipOutput[] Relationships { get; set; } = [];
}

/// <summary>
///     Complete relationship snapshot from Clinical Assessor.
///     Written in neutral third-person language using the character's name.
///     Uses JsonExtensionData to capture the complete relationship object dynamically.
/// </summary>
public sealed class ClinicalRelationshipOutput
{
    /// <summary>
    ///     The name of the character this relationship is toward.
    /// </summary>
    [JsonPropertyName("toward")]
    public required string Toward { get; set; }

    /// <summary>
    ///     How they actually interact - used for the Dynamic field in CharacterRelationshipContext.
    /// </summary>
    [JsonPropertyName("dynamic")]
    public string? Dynamic { get; set; }

    /// <summary>
    ///     All other relationship fields (foundation, stance, trust, desire, intimacy, power, unspoken, developing, etc.)
    ///     captured as extension data to preserve the complete relationship snapshot.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, object>? Data { get; set; }
}
