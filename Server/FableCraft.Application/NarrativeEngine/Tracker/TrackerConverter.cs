using FableCraft.Infrastructure.Persistence.Entities;

namespace FableCraft.Application.NarrativeEngine.Tracker;

using System.Text.Json;
using System.Text.Json.Nodes;

internal static class TrackerDataConverter
{
    public static JsonObject ConvertToAIFormat(TrackerStructure structure, Dictionary<string, object> actualData)
    {
        var result = new JsonObject();
        
        if (structure.Time != null && actualData.TryGetValue("Time", out var timeValue))
        {
            result["Time"] = ConvertFieldValue(structure.Time, timeValue);
        }
        
        if (structure.Weather != null && actualData.TryGetValue("Weather", out var weatherValue))
        {
            result["Weather"] = ConvertFieldValue(structure.Weather, weatherValue);
        }
        
        if (structure.Location != null && actualData.TryGetValue("Location", out var locationValue))
        {
            result["Location"] = ConvertFieldValue(structure.Location, locationValue);
        }
        
        if (structure.CharactersPresent != null && actualData.TryGetValue("CharactersPresent", out var charactersPresent))
        {
            result["CharactersPresent"] = JsonNode.Parse(JsonSerializer.Serialize(charactersPresent));
        }
        
        if (structure.MainCharacterStats != null && actualData.TryGetValue("MainCharacter", out var mainCharData))
        {
            result["MainCharacter"] = ConvertNestedStructure(structure.MainCharacterStats, mainCharData as Dictionary<string, object>);
        }
        
        if (structure.Characters != null && actualData.TryGetValue("Characters", out var charactersData))
        {
            var charactersObject = new JsonObject();
            if (charactersData is Dictionary<string, object> charDict)
            {
                foreach (var (charName, charData) in charDict)
                {
                    if (charData is Dictionary<string, object> charDetails)
                    {
                        var charStructure = structure.Characters.FirstOrDefault();
                        if (charStructure != null)
                        {
                            charactersObject[charName] = ConvertNestedStructure(charStructure, charDetails);
                        }
                    }
                }
            }
            result["Characters"] = charactersObject;
        }
        
        return result;
    }
    
    private static JsonNode? ConvertFieldValue(FieldDefinition field, object value)
    {
        return field.Type switch
        {
            FieldType.String => JsonValue.Create(value?.ToString() ?? field.DefaultValue?.ToString() ?? ""),
            FieldType.Array => JsonNode.Parse(JsonSerializer.Serialize(value)),
            FieldType.Object => ConvertObjectField(field, value),
            FieldType.ForEachObject => ConvertForEachObjectField(field, value),
            _ => JsonValue.Create(value?.ToString() ?? "")
        };
    }
    
    private static JsonObject ConvertObjectField(FieldDefinition field, object value)
    {
        var result = new JsonObject();
        if (field.HasNestedFields && value is Dictionary<string, object> dict)
        {
            result = ConvertNestedStructure(field.NestedFields, dict);
        }
        return result;
    }
    
    private static JsonObject ConvertForEachObjectField(FieldDefinition field, object value)
    {
        var result = new JsonObject();
        if (field.HasNestedFields && value is Dictionary<string, object> dict)
        {
            foreach (var (key, val) in dict)
            {
                if (val is Dictionary<string, object> nestedDict)
                {
                    result[key] = ConvertNestedStructure(field.NestedFields, nestedDict);
                }
            }
        }
        return result;
    }
    
    private static JsonObject ConvertNestedStructure(TrackerStructureDefinition structure, Dictionary<string, object>? data)
    {
        var result = new JsonObject();
        
        foreach (var (_, fieldDef) in structure)
        {
            if (data.TryGetValue(fieldDef.Name, out var value))
            {
                result[fieldDef.Name] = ConvertFieldValue(fieldDef, value);
            }
            else if (fieldDef.DefaultValue != null)
            {
                result[fieldDef.Name] = ConvertFieldValue(fieldDef, fieldDef.DefaultValue);
            }
        }
        
        return result;
    }
}
