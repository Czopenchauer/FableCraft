namespace FableCraft.Infrastructure.Persistence.Entities;

public sealed class LlmLog : IEntity
{
    public Guid? AdventureId { get; set; }

    public Guid? SceneId { get; set; }

    public string? CallerName { get; set; }

    public required string RequestContent { get; set; }

    public required string ResponseContent { get; set; }

    public DateTimeOffset ReceivedAt { get; set; }

    public required int? InputToken { get; set; }

    public required int? OutputToken { get; set; }

    public required int? TotalToken { get; set; }

    public required long Duration { get; set; }

    public Guid Id { get; set; }
}