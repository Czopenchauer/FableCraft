using System.Text.Json;

namespace FableCraft.Infrastructure.Persistence.Entities;

public enum GenerationProcessStep
{
    NotStarted,
    GeneratingScene,
    SceneGenerated
}

public class GenerationProcess : IEntity
{
    public Guid AdventureId { get; set; }

    public required GenerationProcessStep Step { get; set; }

    public required string Context { get; set; }

    public Guid Id { get; set; }

    public T GetContextAs<T>() => JsonSerializer.Deserialize<T>(Context, JsonExtensions.JsonSerializerOptions)!;
}