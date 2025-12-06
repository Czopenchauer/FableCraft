namespace FableCraft.Application.Exceptions;

public sealed class SceneNotFoundException(Guid guid) : Exception($"Scene with ID {guid} was not found.");