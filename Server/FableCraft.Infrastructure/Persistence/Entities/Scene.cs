using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace FableCraft.Infrastructure.Persistence.Entities;

public class Scene : IKnowledgeGraphEntity
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid AdventureId { get; init; }

    public Adventure? Adventure { get; init; }

    public required int SequenceNumber { get; init; }

    public string? Summary { get; set; }

    [Required]
    public required string NarrativeText { get; init; }

    public required Metadata Metadata { get; init; }

    public required DateTime CreatedAt { get; init; }

    public List<CharacterState> CharacterStates { get; set; } = [];

    public List<MainCharacterAction> CharacterActions { get; init; } = new();

    public string GetSceneWithSelectedAction()
    {
        var selectedAction = CharacterActions.FirstOrDefault(x => x.Selected);
        return selectedAction != null
            ? $"{NarrativeText}\n{selectedAction.ActionDescription}".Trim()
            : NarrativeText;
    }

    public Content GetContent()
    {
        var narrativeText = GetSceneWithSelectedAction();

        var scene = $"""
                     Time: {Metadata.Tracker?.Story.Time}
                     Location: {Metadata.Tracker?.Story.Location}
                     Weather: {Metadata.Tracker?.Story.Weather}
                     Characters Present: {string.Join(", ", Metadata?.Tracker?.CharactersPresent ?? [])}

                     {narrativeText}
                     """;

        return new Content(scene, $"Scene number {SequenceNumber}", ContentType.Text);
    }
}

public sealed class Metadata
{
    public required NarrativeDirectorOutput NarrativeMetadata { get; set; }

    public required Tracker Tracker { get; set; }
}

public sealed class Tracker
{
    public required StoryTracker Story { get; init; }

    public string[] CharactersPresent { get; init; } = [];

    public CharacterTracker? MainCharacter { get; init; }

    public CharacterTracker[]? Characters { get; init; }
}

public sealed class StoryTracker
{
    public DateTime Time { get; init; }

    public required string Location { get; init; }

    public required string Weather { get; init; }

    [JsonExtensionData]
    public Dictionary<string, object> AdditionalProperties { get; init; } = null!;
}

public sealed class CharacterTracker
{
    public required string Name { get; init; }

    [JsonExtensionData]
    public Dictionary<string, object> AdditionalProperties { get; init; } = null!;
}