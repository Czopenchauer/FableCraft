using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace FableCraft.Infrastructure.Persistence.Entities;

public class TrackerDefinition : IEntity
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public string Name { get; set; }

    [Column(TypeName = "jsonb")]
    public TrackerStructure Structure { get; set; }
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
    // Time, Location, Weather is required in Story fields
    public FieldDefinition[] Story { get; set; }

    public string[]? CharactersPresent { get; set; }

    // Name is required
    public FieldDefinition[]? MainCharacter { get; set; }

    // Name is required
    public FieldDefinition[]? Characters { get; set; }
}

public sealed class FieldDefinition
{
    public string Name { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public FieldType Type { get; set; }

    public string Prompt { get; set; }

    public object? DefaultValue { get; set; }

    public List<object>? ExampleValues { get; set; }

    public FieldDefinition[]? NestedFields { get; set; }

    public bool IsValid()
    {
        return !string.IsNullOrEmpty(Name) && !string.IsNullOrEmpty(Prompt);
    }

    [MemberNotNullWhen(true, nameof(NestedFields))]
    public bool HasNestedFields => NestedFields is { Length: > 0 };
}