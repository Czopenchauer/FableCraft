using System.Text.Json.Serialization;

using FableCraft.Infrastructure.Persistence.Entities;

namespace FableCraft.Application.Model;

public class LorebookEntryDto
{
    public string Description { get; init; } = string.Empty;

    public string Content { get; init; } = string.Empty;

    public string Category { get; init; } = string.Empty;

    public int Order { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ContentType ContentType { get; set; }
}