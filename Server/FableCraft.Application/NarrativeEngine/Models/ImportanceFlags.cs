using System.Text.Json.Serialization;

namespace FableCraft.Application.NarrativeEngine.Models;

public class ImportanceFlags
{
    [JsonPropertyName("upgrade_requests")]
    public List<ImportanceChangeRequest> UpgradeRequests { get; set; } = new();

    [JsonPropertyName("downgrade_requests")]
    public List<ImportanceChangeRequest> DowngradeRequests { get; set; } = new();
}

public class ImportanceChangeRequest
{
    [JsonPropertyName("character")]
    public string Character { get; set; } = null!;

    [JsonPropertyName("from")]
    public string From { get; set; } = null!;

    [JsonPropertyName("to")]
    public string To { get; set; } = null!;

    [JsonPropertyName("reason")]
    public string Reason { get; set; } = null!;
}
