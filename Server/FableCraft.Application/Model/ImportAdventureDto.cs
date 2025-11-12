namespace FableCraft.Application.Model;

/// <summary>
/// Represents a message from an adventure file
/// </summary>
public class AdventureMessageDto
{
    public string Name { get; set; } = string.Empty;
    public bool IsUser { get; set; }
    public bool IsSystem { get; set; }
    public string SendDate { get; set; } = string.Empty;
    public string Mes { get; set; } = string.Empty;
    public AdventureMessageExtraDto? Extra { get; set; }
    public string? ForceAvatar { get; set; }
    public object? Tracker { get; set; }
}

/// <summary>
/// Extra metadata from adventure message
/// </summary>
public class AdventureMessageExtraDto
{
    public bool IsSmallSys { get; set; }
    public string? Reasoning { get; set; }
    public string? Api { get; set; }
    public string? Model { get; set; }
}

/// <summary>
/// Represents a lorebook entry from import file
/// </summary>
public class ImportLorebookEntryDto
{
    public string Comment { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int Order { get; set; }
    public bool Disable { get; set; }
}

/// <summary>
/// Root structure for imported lorebook file
/// </summary>
public class ImportLorebookDto
{
    public Dictionary<string, ImportLorebookEntryDto> Entries { get; set; } = new();
}

/// <summary>
/// Represents a character from import file
/// </summary>
public class ImportCharacterDto
{
    public string Description { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
