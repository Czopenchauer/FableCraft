using System.Text.Json;

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
    string? Category = null,
    string? Description = null);

/// <summary>
///     Summary of what was created, returned to the client.
/// </summary>
public sealed record ManualCreateContentResult(
    string Kind,
    Guid? Id,
    string Name,
    string Summary);

/// <summary>
///     Draft result returned before persistence. Contains the raw crafted JSON for the user
///     to review and edit before confirming.
/// </summary>
public sealed record ManualContentDraftResult(
    string Kind,
    string Name,
    string Summary,
    JsonElement RawJson);

/// <summary>
///     Confirmation request sent after the user reviews/edits the draft.
///     Carries the (potentially edited) raw JSON to be deserialized and persisted.
/// </summary>
public sealed record ManualContentConfirmRequest(
    ManualContentType Type,
    JsonElement RawJson);
