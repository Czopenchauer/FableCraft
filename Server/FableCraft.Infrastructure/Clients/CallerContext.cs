namespace FableCraft.Infrastructure.Clients;

public record CallerContext(Type CallerType, Guid AdventureId, Guid? SceneId);