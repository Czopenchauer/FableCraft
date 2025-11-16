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

        var textContent = new
        {
            NarrativeText = narrativeText,
            Tracker = new Dictionary<string, object>()
        };

        if (!string.IsNullOrEmpty(SceneStateJson))
        {
            try
            {
                using var doc = JsonDocument.Parse(SceneStateJson);
                if (doc.RootElement.TryGetProperty("tracker", out var tracker))
                {
                    foreach (var property in tracker.EnumerateObject())
                    {
                        if (property.Value.ValueKind == JsonValueKind.Array)
                        {
                            var arrayItems = property.Value.EnumerateArray()
                                .Select(item => item.ToString())
                                .ToList();
                            textContent.Tracker[property.Name] = arrayItems;
                        }
                        else if (property.Value.ValueKind == JsonValueKind.Object)
                        {
                            var nestedObj = new Dictionary<string, object>();
                            foreach (var nestedProp in property.Value.EnumerateObject())
                            {
                                nestedObj[nestedProp.Name] = nestedProp.Value.ValueKind == JsonValueKind.Array
                                    ? nestedProp.Value.EnumerateArray().Select(i => i.ToString()).ToList()
                                    : nestedProp.Value.ToString();
                            }

                            textContent.Tracker[property.Name] = nestedObj;
                        }
                        else
                        {
                            textContent.Tracker[property.Name] = property.Value.ToString();
                        }
                    }
                }
            }
            catch(JsonException)
            {
                // Return content with empty tracker if JSON parsing fails
            }
        }
        var text = JsonSerializer.Serialize(textContent, new JsonSerializerOptions { WriteIndented = true });
        return new Content(text, $"Scene number {SequenceNumber}", ContentType.Json);
    }
}