using System.Text.Json.Serialization;

using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Infrastructure.Persistence.Entities;

using Microsoft.SemanticKernel;

namespace FableCraft.Application.NarrativeEngine;

internal sealed class NarrativeContext
{
    [JsonIgnore]
    public required Guid AdventureId { get; set; }

    public required Kernel KernelKg { get; set; }

    public required string? StorySummary { get; set; }

    public required string PlayerAction { get; set; }

    public required string CommonContext { get; set; }

    public required SceneContext[] SceneContext { get; set; }

    public required List<CharacterContext> Characters { get; set; } = new();

    public required LocationGenerationResult[] NewLocations { get; set; }

    public required GeneratedLore[] NewLore { get; set; }

    public Metadata? GetCurrentSceneMetadata()
    {
        return SceneContext.LastOrDefault()?.Metadata;
    }
}

internal sealed class CharacterContext
{
    public required Guid CharacterId { get; set; }

    public string Name { get; set; } = null!;

    public string Description { get; set; } = null!;

    public CharacterStats CharacterState { get; set; } = null!;

    public CharacterTracker? CharacterTracker { get; set; }
}

internal sealed class SceneContext
{
    public string SceneContent { get; set; } = null!;

    public string PlayerChoice { get; set; } = null!;

    public required IEnumerable<CharacterContext> Characters { get; set; } = [];

    public Metadata Metadata { get; set; } = null!;
}