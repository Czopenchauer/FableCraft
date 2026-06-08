namespace FableCraft.Server.Models;

public enum ManualContentType
{
    Character,
    Location,
    Item,
    Lore
}

/// <summary>
///     Player-supplied request to manually create a piece of canon (character, location, item,
///     or lore) after a scene has been generated.
/// </summary>
public sealed record ManualCreateContentRequest(
    ManualContentType Type,
    string Name,
    string Details,
    string? Importance = null,
    string? PowerLevel = null,
    string? Category = null);

/// <summary>
///     Summary of what was created, returned to the client.
/// </summary>
public sealed record ManualCreateContentResult(
    string Kind,
    Guid? Id,
    string Name,
    string Summary);
