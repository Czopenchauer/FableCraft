using System.Text.Json;

using FableCraft.Infrastructure.Persistence.Entities.Adventure;

namespace FableCraft.Application.NarrativeEngine;

/// <summary>
/// Merges delta updates into the previous tracker state.
/// Implements the merge contract where omitted fields retain previous values.
///
/// Merge rules are detected dynamically based on the update structure:
/// - Objects with $modify/$add/$remove keys → Complex array operations
/// - Objects without operation keys → Nested object merge (sub-field level)
/// - Arrays → Full replacement
/// - Scalars → Direct replacement
/// </summary>
public static class TrackerMerger
{
    private static readonly string[] OperationKeys = ["$modify", "$add", "$remove"];

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    /// <summary>
    /// Merges delta updates into the previous tracker state to produce the new state.
    /// </summary>
    /// <param name="previousState">The previous full tracker state.</param>
    /// <param name="updates">The delta updates containing only changed fields.</param>
    /// <returns>The merged tracker state.</returns>
    public static MainCharacterTracker Merge(MainCharacterTracker previousState, JsonElement updates)
    {
        // Start with a deep copy of the previous state
        var newState = DeepCopy(previousState);

        if (updates.ValueKind != JsonValueKind.Object)
            return newState;

        foreach (var property in updates.EnumerateObject())
        {
            var key = property.Name;
            var value = property.Value;

            // Handle known typed properties on MainCharacterTracker
            switch (key)
            {
                case "Name":
                    newState.Name = value.GetString() ?? newState.Name;
                    break;
                case "Appearance":
                    newState.Appearance = value.GetString();
                    break;
                case "GeneralBuild":
                    newState.GeneralBuild = value.GetString();
                    break;
                default:
                    // Handle dynamic properties in AdditionalProperties
                    MergeProperty(newState.AdditionalProperties, key, value);
                    break;
            }
        }

        return newState;
    }

    /// <summary>
    /// Merges delta updates into a dictionary (generic version for any tracker type).
    /// </summary>
    /// <param name="previousState">The previous full tracker state as a dictionary.</param>
    /// <param name="updates">The delta updates containing only changed fields.</param>
    /// <returns>The merged tracker state.</returns>
    public static Dictionary<string, object> Merge(Dictionary<string, object> previousState, JsonElement updates)
    {
        var newState = DeepCopyDictionary(previousState);

        if (updates.ValueKind != JsonValueKind.Object)
            return newState;

        foreach (var property in updates.EnumerateObject())
        {
            MergeProperty(newState, property.Name, property.Value);
        }

        return newState;
    }

    /// <summary>
    /// Merges delta updates into a CharacterTracker state to produce the new state.
    /// Handles typed properties (Name, Location, Appearance, GeneralBuild) explicitly
    /// and routes everything else to AdditionalProperties via MergeProperty.
    /// </summary>
    /// <param name="previousState">The previous full CharacterTracker state.</param>
    /// <param name="updates">The delta updates containing only changed fields.</param>
    /// <returns>The merged CharacterTracker state.</returns>
    public static CharacterTracker Merge(CharacterTracker previousState, JsonElement updates)
    {
        var newState = DeepCopy(previousState);

        if (updates.ValueKind != JsonValueKind.Object)
            return newState;

        foreach (var property in updates.EnumerateObject())
        {
            var key = property.Name;
            var value = property.Value;

            switch (key)
            {
                case "Name":
                    newState.Name = value.GetString() ?? newState.Name;
                    break;
                case "Location":
                    newState.Location = value.GetString() ?? newState.Location;
                    break;
                case "Appearance":
                    newState.Appearance = value.GetString();
                    break;
                case "GeneralBuild":
                    newState.GeneralBuild = value.GetString();
                    break;
                default:
                    MergeProperty(newState.AdditionalProperties, key, value);
                    break;
            }
        }

        return newState;
    }

    /// <summary>
    /// Merges a single property value into the target dictionary.
    /// Automatically detects the merge strategy based on the value structure.
    /// </summary>
    internal static void MergeProperty(Dictionary<string, object> target, string key, JsonElement value)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.Object when HasOperationKeys(value):
                // Complex array with $modify/$add/$remove operations
                MergeComplexArray(target, key, value);
                break;

            case JsonValueKind.Object:
                // Nested object: merge at sub-field level
                MergeNestedObject(target, key, value);
                break;

            case JsonValueKind.Array:
                // Simple array: full replacement
                target[key] = JsonElementToObject(value);
                break;

            default:
                // Scalar: direct replacement
                target[key] = JsonElementToObject(value);
                break;
        }
    }

    /// <summary>
    /// Checks if a JSON object contains any operation keys ($modify, $add, $remove).
    /// </summary>
    private static bool HasOperationKeys(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Object)
            return false;

        foreach (var opKey in OperationKeys)
        {
            if (value.TryGetProperty(opKey, out _))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Merges a nested object at the sub-field level.
    /// Existing sub-fields not in the update are preserved.
    /// </summary>
    private static void MergeNestedObject(Dictionary<string, object> target, string key, JsonElement value)
    {
        // Get or create the nested object
        var nested = GetOrCreateNestedDictionary(target, key);

        // Recursively merge each sub-field
        foreach (var subProperty in value.EnumerateObject())
        {
            MergeProperty(nested, subProperty.Name, subProperty.Value);
        }

        target[key] = nested;
    }

    /// <summary>
    /// Merges a complex array using $modify, $add, and $remove operations.
    /// </summary>
    private static void MergeComplexArray(Dictionary<string, object> target, string key, JsonElement value)
    {
        // Get existing array
        var existingArray = GetExistingArray(target, key);

        // Process $modify operations
        if (value.TryGetProperty("$modify", out var modifyArray) && modifyArray.ValueKind == JsonValueKind.Array)
        {
            foreach (var modification in modifyArray.EnumerateArray())
            {
                ProcessModifyOperation(existingArray, modification);
            }
        }

        // Process $add operations
        if (value.TryGetProperty("$add", out var addArray) && addArray.ValueKind == JsonValueKind.Array)
        {
            foreach (var newEntry in addArray.EnumerateArray())
            {
                existingArray.Add(JsonElementToObject(newEntry));
            }
        }

        // Process $remove operations
        if (value.TryGetProperty("$remove", out var removeArray) && removeArray.ValueKind == JsonValueKind.Array)
        {
            ProcessRemoveOperations(existingArray, removeArray);
        }

        target[key] = existingArray;
    }

    /// <summary>
    /// Processes a single $modify operation on an array.
    /// Finds the matching entry and applies $set updates.
    /// </summary>
    private static void ProcessModifyOperation(List<object> array, JsonElement modification)
    {
        if (!modification.TryGetProperty("$match", out var matchValue))
            return;

        var matchString = matchValue.GetString();
        if (string.IsNullOrEmpty(matchString))
            return;

        // Find entry by scanning all fields for a match
        var entryIndex = FindEntryByValue(array, matchString);
        if (entryIndex < 0)
            return;

        if (!modification.TryGetProperty("$set", out var setValues) || setValues.ValueKind != JsonValueKind.Object)
            return;

        // Get or convert the entry to a dictionary
        var entry = EnsureDictionary(array[entryIndex]);

        // Apply all $set updates
        foreach (var setProp in setValues.EnumerateObject())
        {
            entry[setProp.Name] = JsonElementToObject(setProp.Value);
        }

        array[entryIndex] = entry;
    }

    /// <summary>
    /// Finds an entry in the array where any field matches the given value.
    /// Prioritizes fields ending with Name, Id, Type, Item (common identifier patterns).
    /// </summary>
    private static int FindEntryByValue(List<object> array, string matchValue)
    {
        for (var i = 0; i < array.Count; i++)
        {
            if (EntryMatchesValue(array[i], matchValue))
                return i;
        }

        return -1;
    }

    /// <summary>
    /// Checks if any identifier-like field in the entry matches the given value.
    /// </summary>
    private static bool EntryMatchesValue(object entry, string matchValue)
    {
        var fields = GetEntryFields(entry);
        if (fields == null)
            return false;

        // First pass: check common identifier fields (Name, *Name, *Id, Type, Item)
        foreach (var (fieldName, fieldValue) in fields)
        {
            if (!IsIdentifierField(fieldName))
                continue;

            if (string.Equals(fieldValue?.ToString(), matchValue, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        // Second pass: check all string fields
        foreach (var (_, fieldValue) in fields)
        {
            if (fieldValue is string strValue &&
                string.Equals(strValue, matchValue, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Determines if a field name looks like an identifier field.
    /// </summary>
    private static bool IsIdentifierField(string fieldName)
    {
        return fieldName.Equals("Name", StringComparison.OrdinalIgnoreCase) ||
               fieldName.EndsWith("Name", StringComparison.OrdinalIgnoreCase) ||
               fieldName.EndsWith("Id", StringComparison.OrdinalIgnoreCase) ||
               fieldName.Equals("Type", StringComparison.OrdinalIgnoreCase) ||
               fieldName.Equals("Item", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets all fields from an entry as key-value pairs.
    /// </summary>
    private static IEnumerable<(string Name, object? Value)>? GetEntryFields(object entry)
    {
        if (entry is Dictionary<string, object> dict)
        {
            return dict.Select(kv => (kv.Key, (object?)kv.Value));
        }

        if (entry is JsonElement element && element.ValueKind == JsonValueKind.Object)
        {
            return element.EnumerateObject()
                .Select(p => (p.Name, (object?)JsonElementToObject(p.Value)));
        }

        return null;
    }

    /// <summary>
    /// Processes $remove operations, removing entries that match any of the specified values.
    /// </summary>
    private static void ProcessRemoveOperations(List<object> array, JsonElement removeArray)
    {
        var toRemove = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var removeItem in removeArray.EnumerateArray())
        {
            var removeValue = removeItem.GetString();
            if (!string.IsNullOrEmpty(removeValue))
                toRemove.Add(removeValue);
        }

        array.RemoveAll(entry => EntryMatchesAny(entry, toRemove));
    }

    /// <summary>
    /// Checks if any identifier field in the entry matches any value in the set.
    /// </summary>
    private static bool EntryMatchesAny(object entry, HashSet<string> values)
    {
        var fields = GetEntryFields(entry);
        if (fields == null)
            return false;

        foreach (var (fieldName, fieldValue) in fields)
        {
            if (!IsIdentifierField(fieldName))
                continue;

            var strValue = fieldValue?.ToString();
            if (strValue != null && values.Contains(strValue))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Gets or creates a nested dictionary from the target.
    /// </summary>
    private static Dictionary<string, object> GetOrCreateNestedDictionary(Dictionary<string, object> target, string key)
    {
        if (!target.TryGetValue(key, out var existing))
            return new Dictionary<string, object>();

        if (existing is JsonElement existingElement)
            return JsonElementToDictionary(existingElement);

        if (existing is Dictionary<string, object> existingDict)
            return new Dictionary<string, object>(existingDict);

        return new Dictionary<string, object>();
    }

    /// <summary>
    /// Gets an existing array from the target, converting if necessary.
    /// </summary>
    private static List<object> GetExistingArray(Dictionary<string, object> target, string key)
    {
        if (!target.TryGetValue(key, out var existing))
            return [];

        if (existing is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Array)
        {
            return jsonElement.EnumerateArray().Select(JsonElementToObject).ToList();
        }

        if (existing is List<object> existingList)
            return new List<object>(existingList);

        if (existing is object[] existingArr)
            return [.. existingArr];

        return [];
    }

    /// <summary>
    /// Ensures an object is a dictionary, converting if necessary.
    /// </summary>
    private static Dictionary<string, object> EnsureDictionary(object entry)
    {
        if (entry is Dictionary<string, object> dict)
            return dict;

        if (entry is JsonElement element && element.ValueKind == JsonValueKind.Object)
            return JsonElementToDictionary(element);

        return new Dictionary<string, object>();
    }

    private static MainCharacterTracker DeepCopy(MainCharacterTracker source)
    {
        var json = JsonSerializer.Serialize(source, JsonOptions);
        return JsonSerializer.Deserialize<MainCharacterTracker>(json, JsonOptions)!;
    }

    private static CharacterTracker DeepCopy(CharacterTracker source)
    {
        var json = JsonSerializer.Serialize(source, JsonOptions);
        return JsonSerializer.Deserialize<CharacterTracker>(json, JsonOptions)!;
    }

    private static Dictionary<string, object> DeepCopyDictionary(Dictionary<string, object> source)
    {
        var json = JsonSerializer.Serialize(source, JsonOptions);
        return JsonSerializer.Deserialize<Dictionary<string, object>>(json, JsonOptions)!;
    }

    private static Dictionary<string, object> JsonElementToDictionary(JsonElement element)
    {
        var dict = new Dictionary<string, object>();
        if (element.ValueKind != JsonValueKind.Object)
            return dict;

        foreach (var property in element.EnumerateObject())
        {
            dict[property.Name] = JsonElementToObject(property.Value);
        }

        return dict;
    }

    internal static object JsonElementToObject(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString()!,
            JsonValueKind.Number => element.TryGetInt64(out var l) ? l : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null!,
            JsonValueKind.Array => element.EnumerateArray().Select(JsonElementToObject).ToList(),
            JsonValueKind.Object => JsonElementToDictionary(element),
            _ => element.GetRawText()
        };
    }
}
