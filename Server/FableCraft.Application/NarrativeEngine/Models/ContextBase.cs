namespace FableCraft.Application.NarrativeEngine.Models;

internal class ContextBase
{
    public required AnalysisSummary AnalysisSummary { get; set; }

    public ContextItem[] WorldContext { get; set; } = [];

    public ContextItem[] NarrativeContext { get; set; } = [];

    public DroppedContext[] DroppedContext { get; set; } = [];
}