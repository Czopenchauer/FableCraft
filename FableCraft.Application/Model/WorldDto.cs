namespace FableCraft.Application.Validators;

public class WorldDto
{
    public string Name { get; set; } = string.Empty;

    public string Backstory { get; set; } = string.Empty;

    public string UniverseBackstory { get; set; } = string.Empty;

    public CharacterDto Character { get; set; } = null!;

    public List<LorebookEntryDto> Lorebook { get; set; } = new();
}

public class CharacterDto
{
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Background { get; set; } = string.Empty;
}

public class LorebookEntryDto
{
    public string Title { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;
}