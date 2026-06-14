namespace FableCraft.Application.NarrativeEngine.Models;

internal sealed class NarrativeCatalystOutput
{
    public string StoryAssessment { get; init; } = null!;

    public string CatalystGoals { get; init; } = null!;

    public string? RandomEvent { get; init; }
}