using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace FableCraft.Infrastructure.Persistence.Entities;

public class Scene : IKnowledgeGraphEntity
{
    [Required]
    public Guid AdventureId { get; init; }

    public Adventure? Adventure { get; init; }

    public required int SequenceNumber { get; init; }

    [Required]
    public required string NarrativeText { get; init; }

    [Column(TypeName = "jsonb")]
    public string? SceneStateJson { get; init; } = null!;

    public DateTime CreatedAt { get; init; }

    public List<CharacterAction> CharacterActions { get; init; } = new();

    [Key]
    public Guid Id { get; set; }

    public Content GetContent()
    {
        var selectedAction = CharacterActions.FirstOrDefault(x => x.Selected);
        var narrativeText = selectedAction != null
            ? $"{NarrativeText}\n{selectedAction.ActionDescription}".Trim()
            : NarrativeText;

        if (!string.IsNullOrEmpty(SceneStateJson))
        {
            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var sceneState = JsonSerializer.Deserialize<TrackerStructure>(SceneStateJson, options);

                if (sceneState != null)
                {
                    var timeField = sceneState.Story?.FirstOrDefault(f => f.Name == "Time");
                    var locationField = sceneState.Story?.FirstOrDefault(f => f.Name == "Location");

                    var metadata = new List<string>();

                    if (timeField?.DefaultValue != null)
                    {
                        metadata.Add($"Time: {timeField.DefaultValue}");
                    }

                    if (locationField?.DefaultValue != null)
                    {
                        metadata.Add($"Location: {locationField.DefaultValue}");
                    }

                    if (sceneState.CharactersPresent?.Length > 0)
                    {
                        metadata.Add($"Characters Present: {string.Join(", ", sceneState.CharactersPresent)}");
                    }

                    if (metadata.Any())
                    {
                        narrativeText = $"{string.Join("\n", metadata)}\n\n{narrativeText}";
                    }
                }
            }
            catch (JsonException)
            {
                // Ignore deserialization errors and proceed without metadata
            }
        }

        return new Content(narrativeText, $"Scene number {SequenceNumber}", ContentType.Text);
    }
}