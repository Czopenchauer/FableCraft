using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace FableCraft.Infrastructure.Persistence.Entities.Adventure;

public class TrackerDefinition : IEntity
{
    [Required]
    public required string Name { get; set; }

    public required TrackerStructure Structure { get; set; }

    [Key]
    public Guid Id { get; set; }
}

public enum FieldType
{
    String,
    Array,
    Object,
    ForEachObject
}

public sealed class TrackerStructure
{
    public required FieldDefinition[] Story { get; set; }

    public required FieldDefinition[] MainCharacter { get; set; }

    public required FieldDefinition[] Characters { get; set; }

    public FieldDefinition[]? CharacterDevelopment { get; set; }

    public FieldDefinition[]? MainCharacterDevelopment { get; set; }
}

public sealed class FieldDefinition
{
    public required string Name { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public FieldType Type { get; set; }

    public required string Prompt { get; set; }

    public object? DefaultValue { get; set; }

    public List<object>? ExampleValues { get; set; }

    public FieldDefinition[]? NestedFields { get; set; }

    [MemberNotNullWhen(true, nameof(NestedFields))]
    public bool HasNestedFields => NestedFields is { Length: > 0 };

    public bool IsValid()
    {
        return !string.IsNullOrEmpty(Name) && !string.IsNullOrEmpty(Prompt);
    }
}