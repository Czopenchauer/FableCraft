using System.Text.Json.Serialization;

namespace FableCraft.Application.NarrativeEngine.Models;

/// <summary>
/// Writer output for character importance tier changes.
/// Only arc_important &lt;-&gt; significant transitions are valid.
/// Background characters cannot be upgraded/downgraded.
/// </summary>
public sealed class ImportanceFlags
{
    [JsonPropertyName("upgrade_requests")]
    public List<ImportanceChangeRequest> UpgradeRequests { get; init; } = [];

    [JsonPropertyName("downgrade_requests")]
    public List<ImportanceChangeRequest> DowngradeRequests { get; init; } = [];
}

/// <summary>
/// Request to change a character's importance tier.
/// Valid transitions: significant -&gt; arc_important (upgrade) or arc_important -&gt; significant (downgrade).
/// </summary>
public sealed class ImportanceChangeRequest
{
    [JsonPropertyName("character")]
    public string Character { get; init; } = null!;

    [JsonPropertyName("from")]
    public string From { get; init; } = null!;

    [JsonPropertyName("to")]
    public string To { get; init; } = null!;

    [JsonPropertyName("reason")]
    public string Reason { get; init; } = null!;
}
