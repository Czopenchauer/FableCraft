namespace FableCraft.Application.Model.Adventure;

public class AdventureListItemDto
{
    public Guid AdventureId { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? LastScenePreview { get; init; }

    public DateTimeOffset Created { get; init; }

    public DateTimeOffset? LastPlayed { get; init; }
}