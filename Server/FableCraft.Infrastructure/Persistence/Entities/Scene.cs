using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
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

    [Required]
    public required string NarrativeText { get; init; }

    [Column(TypeName = "jsonb")]
    public required Tracker Tracker { get; init; }

    public required DateTime CreatedAt { get; init; }

    public List<CharacterAction> CharacterActions { get; init; } = new();

    public Content GetContent()
    {
        var selectedAction = CharacterActions.FirstOrDefault(x => x.Selected);
        var narrativeText = selectedAction != null
            ? $"{NarrativeText}\n{selectedAction.ActionDescription}".Trim()
            : NarrativeText;

        var scene = $"""
                     Time: {Tracker.Story.Time}
                     Location: {Tracker.Story.Location}
                     Weather: {Tracker.Story.Weather}
                     Characters Present: {string.Join(", ", Tracker.CharactersPresent)}
                     Main Character: {Tracker.MainCharacter.Name}

                     {narrativeText}
                     """;

        return new Content(scene, $"Scene number {SequenceNumber}", ContentType.Text);
    }
}

public sealed class Tracker
{
    public StoryTracker Story { get; init; }

    public string[] CharactersPresent { get; init; }

    public CharacterTracker MainCharacter { get; init; }

    public CharacterTracker[] Characters { get; init; }
}

public sealed class StoryTracker
{
    public DateTime Time { get; init; }

    public string Location { get; init; }

    public string Weather { get; init; }

    [JsonExtensionData]
    public Dictionary<string, object> AdditionalProperties { get; init; }
}

public sealed class CharacterTracker
{
    public string Name { get; init; }

    [JsonExtensionData]
    public Dictionary<string, object> AdditionalProperties { get; init; }
}