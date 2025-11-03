namespace FableCraft.Application.Model;

public class AdventureDto
{
    public Guid AdventureId { get; init; }

    public string Name { get; init; } = string.Empty;

    public string FirstSceneDescription { get; init; } = string.Empty;

    public string AuthorNotes { get; init; } = string.Empty;

    public CharacterDto Character { get; init; } = null!;

    public List<LorebookEntryDto> Lorebook { get; init; } = new();
}