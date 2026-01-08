namespace FableCraft.Infrastructure;

public static class ProcessExecutionContext
{
    public readonly static AsyncLocal<Guid?> AdventureId = new();
    public readonly static AsyncLocal<Guid?> SceneId = new();
}