namespace FableCraft.Application.Model;

public class GeneratedLorebookDto
{
    public string Content { get; init; } = string.Empty;
}

public class AvailableLorebookDto
{
    public string Category { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public int Priority { get; init; }
}

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

public class GenerateLorebookDto
{
    public LorebookEntryDto[] Lorebooks { get; init; } = [];

    public string Category { get; init; } = string.Empty;

    public string? AdditionalInstruction { get; init; }
}

public class AdventureListItemDto
{
    public Guid AdventureId { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? LastScenePreview { get; init; }
}