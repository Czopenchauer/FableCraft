using System.Text.Json.Serialization;

using FableCraft.Infrastructure.Persistence.Entities;

namespace FableCraft.Application.Model.Worldbook;

public class LorebookEntryResponseDto
{
    public Guid Id { get; init; }

    public Guid AdventureId { get; init; }

    public Guid? SceneId { get; init; }

    public string? Title { get; init; }

    public string Description { get; init; } = string.Empty;

    public int Priority { get; init; }

    public string Content { get; init; } = string.Empty;

    public string Category { get; init; } = string.Empty;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ContentType ContentType { get; init; }
}
