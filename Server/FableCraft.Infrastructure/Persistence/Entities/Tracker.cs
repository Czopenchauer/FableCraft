using System.Text.Json.Serialization;

namespace FableCraft.Infrastructure.Persistence.Entities;

public enum FieldType
{
    String,
    Array,
    Object,
    ForEachObject
}

public sealed class Tracker
{
    public string TrackerName { get; set; }
    
    [JsonPropertyName("time")]
    public FieldDefsetion Time { get; set; }

    public FieldDefsetion Weather { get; set; }

    [JsonPropertyName("location")]
    public FieldDefsetion Location { get; set; }

    [JsonPropertyName("characters_present")]
    public string[] CharactersPresent { get; set; }

    [JsonPropertyName("main_character")]
    public TrackerDefsetion MainCharacterStats { get; set; }

    [JsonPropertyName("characters")]
    public TrackerDefsetion[] Characters { get; set; }
}

public sealed class TrackerDefsetion : Dictionary<string, FieldDefsetion>;

public sealed class FieldDefsetion
{
    public string Name { get; set; }

    public FieldType Type { get; set; }

    public string Prompt { get; set; }

    public object[] DefaultValue { get; set; }

    public List<object> ExampleValues { get; set; } = new List<object>();

    public TrackerDefsetion NestedFields { get; set; } = new TrackerDefsetion();

    public bool IsValid()
    {
        return !string.IsNullOrEmpty(Name) && 
               !string.IsNullOrEmpty(Prompt);
    }

    public bool HasNestedFields => NestedFields != null && NestedFields.Count > 0;
}