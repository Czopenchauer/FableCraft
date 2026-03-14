using System.Text.Json.Serialization;

using FableCraft.Infrastructure.Persistence.Entities;

namespace FableCraft.Application.Model.Worldbook;

public class CreateLorebookEntryDto
{
    public required string Title { get; init; }

    public required string Content { get; init; }

    public required string Category { get; init; }

    public string? Description { get; init; }

    public int Priority { get; init; } = 0;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ContentType ContentType { get; init; } = ContentType.txt;
}

public class UpdateLorebookEntryDto
{
    public required string Title { get; init; }

    public required string Content { get; init; }

    public required string Category { get; init; }

    public string? Description { get; init; }

    public int Priority { get; init; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ContentType ContentType { get; init; }
}

public class BulkCreateLorebookEntriesDto
{
    public required List<CreateLorebookEntryDto> Entries { get; init; }
}
