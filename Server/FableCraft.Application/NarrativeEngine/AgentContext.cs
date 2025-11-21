using System.Text.Json.Serialization;

using FableCraft.Infrastructure.Persistence.Entities;

namespace FableCraft.Application.NarrativeEngine;

internal sealed class NarrativeContext
{
    [JsonIgnore]
    public required string AdventureId { get; set; }

    public required string? StorySummary { get; set; }

    public required string PlayerAction { get; set; }

    public required string CommonContext { get; set; }

    public required TrackerStructure TrackerStructure { get; set; }

    public required SceneContext[] SceneContext { get; set; }

    public List<CharacterContext> Characters { get; set; }

    public SceneMetadata GetCurrentSceneMetadata()
    {
        return SceneContext[^1].SceneMetadata;
    }
}

internal sealed class CharacterContext
{
    public string Name { get; set; }

    public string Description { get; set; }

    public CharacterState CharacterState { get; set; }

    public CharacterTracker CharacterTracker { get; set; }
}

internal sealed class SceneContext
{
    public string SceneContent { get; set; }

    public string PlayerChoice { get; set; }

    public Character[] Characters { get; set; }

    public SceneMetadata SceneMetadata { get; set; }
}