using System.Text.Json.Serialization;

namespace FableCraft.Application.NarrativeEngine.Models;

public class GeneratedScene
{
    [JsonPropertyName("scene")]
    public string Scene { get; init; } = null!;

    [JsonPropertyName("choices")]
    public string[] Choices { get; init; } = null!;

    [JsonPropertyName("character_observations")]
    public CharacterObservations CharacterObservations { get; init; } = null!;
}

public class CharacterObservations
{
    [JsonPropertyName("potential_profiles")]
    public PotentialProfile[] PotentialProfiles { get; init; } = [];

    [JsonPropertyName("recurring_npcs")]
    public RecurringNpc[] RecurringNpcs { get; init; } = [];
}

public class PotentialProfile
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = null!;

    [JsonPropertyName("role")]
    public string Role { get; init; } = null!;

    [JsonPropertyName("reason")]
    public string Reason { get; init; } = null!;

    [JsonPropertyName("established_details")]
    public EstablishedDetails EstablishedDetails { get; init; } = null!;
}

public class EstablishedDetails
{
    [JsonPropertyName("appearance")]
    public string Appearance { get; init; } = null!;

    [JsonPropertyName("personality")]
    public string Personality { get; init; } = null!;

    [JsonPropertyName("background")]
    public string Background { get; init; } = null!;

    [JsonPropertyName("relationship_seed")]
    public string RelationshipSeed { get; init; } = null!;
}

public class RecurringNpc
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = null!;

    [JsonPropertyName("details_to_maintain")]
    public string DetailsToMaintain { get; init; } = null!;
}