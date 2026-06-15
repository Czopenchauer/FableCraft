namespace FableCraft.Application.NarrativeEngine.Models;

internal sealed class QaReviewOutput
{
    public required string ReviewText { get; init; }

    public bool IsPass => ReviewText.Contains("VERDICT: PASS", StringComparison.OrdinalIgnoreCase);
}