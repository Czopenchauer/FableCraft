using FableCraft.Infrastructure.Persistence.Entities;

namespace FableCraft.Application.Model;

public class AdventureDto
{
    public required string Name { get; init; } = string.Empty;

    public required string FirstSceneDescription { get; init; } = string.Empty;

    public required DateTime ReferenceTime { get; init; }

    public required string AuthorNotes { get; init; } = string.Empty;

    public required CharacterDto Character { get; init; } = null!;

    public List<LorebookEntryDto> Lorebook { get; init; } = new();

    public required TrackerStructure TrackerStructure { get; init; }
}