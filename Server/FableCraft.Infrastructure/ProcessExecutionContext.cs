namespace FableCraft.Infrastructure;

public static class ProcessExecutionContext
{
    public readonly static AsyncLocal<Guid?> AdventureId = new();
    public readonly static AsyncLocal<Guid?> SceneId = new();
    public readonly static AsyncLocal<string?> OperationName = new();
}