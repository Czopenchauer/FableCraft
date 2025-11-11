namespace FableCraft.Application.NarrativeEngine.Agents;

internal sealed class TrackerAgent : AgentBase
{
    public override string Name { get; }

    public override string Description { get; }

    protected override string BuildInstruction(NarrativeContext context)
    {
        throw new NotImplementedException();
    }
}