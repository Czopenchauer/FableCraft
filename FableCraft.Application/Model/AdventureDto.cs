namespace FableCraft.Application.Model;

public class AdventureDto
{
    public Guid AdventureId { get; init; }
    
    public string Name { get; init; } = string.Empty;

    public string WorldDescription { get; init; } = string.Empty;

    public string FirstSceneDescription { get; init; } = string.Empty;

    public string AuthorNotes { get; init; } = string.Empty;

    public CharacterDto Character { get; init; } = null!;

    public List<LorebookEntryDto> Lorebook { get; init; } = new();
}

public class CharacterDto
{
    public string Name { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string Background { get; init; } = string.Empty;
}

public class LorebookEntryDto
{
    public string Description { get; init; } = string.Empty;

    public string Content { get; init; } = string.Empty;

    public string Category { get; init; } = string.Empty;
}