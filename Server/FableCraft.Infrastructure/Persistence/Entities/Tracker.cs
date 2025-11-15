using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace FableCraft.Infrastructure.Persistence.Entities;

public class Tracker : IEntity
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
    [JsonPropertyName("time")]
    public FieldDefinition? Time { get; set; }

    public FieldDefinition? Weather { get; set; }

    [JsonPropertyName("location")]
    public FieldDefinition? Location { get; set; }

    [JsonPropertyName("characters_present")]
    public string[]? CharactersPresent { get; set; }

    [JsonPropertyName("main_character")]
    public TrackerStructureDefinition? MainCharacterStats { get; set; }

    [JsonPropertyName("characters")]
    public TrackerStructureDefinition[]? Characters { get; set; }
}

public sealed class TrackerStructureDefinition : Dictionary<string, FieldDefinition>;

public sealed class FieldDefinition
{
    public string Name { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public FieldType Type { get; set; }

    public string Prompt { get; set; }

    public object? DefaultValue { get; set; }

    public List<object>? ExampleValues { get; set; }

    public TrackerStructureDefinition? NestedFields { get; set; }

    public bool IsValid()
    {
        return !string.IsNullOrEmpty(Name) && !string.IsNullOrEmpty(Prompt);
    }

    [MemberNotNullWhen(true, nameof(NestedFields))]
    public bool HasNestedFields => NestedFields is { Count: > 0 };
}