namespace FableCraft.Infrastructure;

public static class ProcessExecutionContext
{
    public readonly static AsyncLocal<Guid?> AdventureId = new();
}