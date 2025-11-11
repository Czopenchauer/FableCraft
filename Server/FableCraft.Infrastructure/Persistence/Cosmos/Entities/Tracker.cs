using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace FableCraft.Infrastructure.Persistence.Cosmos.Entities;

public sealed class Tracker
{
    public string? SystemPrompt { get; set; }

    public TrackerField Fields { get; set; } = null!;
}

public sealed class TrackerField
{
    public string Name { get; set; } = null!;

    public int Order { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public Type Type { get; set; }

    public string Prompt { get; set; } = null!;

    public string[] ExampleValues { get; set; } = null!;

    public string? Value { get; set; } = null!;

    public TrackerField[]? NestedFields { get; set; }

    [MemberNotNullWhen(true, nameof(NestedFields))]
    public bool IsObjectType => Type == Type.Object;
}

public enum Type
{
    Text,
    Array,
    Object
}