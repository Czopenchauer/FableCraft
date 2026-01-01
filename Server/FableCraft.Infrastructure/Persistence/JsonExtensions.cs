using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace FableCraft.Infrastructure.Persistence;

public static partial class JsonExtensions
{
    public readonly static JsonSerializerOptions JsonSerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true
    };

    // Matches patterns like "Skills[Consciousness Analysis]" or "Items[Sword]"
    [GeneratedRegex(@"^(?<property>\w+)\[(?<identifier>.+)\]$")]
    private static partial Regex ArrayAccessPattern();

    public static string ToJsonString<T>(this T obj)
    {
        return JsonSerializer.Serialize(obj, JsonSerializerOptions);
    }

    public static string ToJsonString<T>(this T obj, JsonSerializerOptions options)
    {
        return JsonSerializer.Serialize(obj, options);
    }

    /// <summary>
    /// Patches an object with updates specified using dot-notation paths.
    /// Each key is a path like "emotional_landscape.current_state" and the value
    /// is the complete object to replace at that path.
    /// Supports array item access by identifier e.g.: "Skills[Consciousness Analysis]" or
    /// "MagicAndAbilities.InstinctiveAbilities[Fire Breath].Power"
    /// </summary>
    public static T PatchWith<T>(this T original, IDictionary<string, object> updates)
    {
        if (updates.Count == 0)
        {
            return original;
        }

        var json = original.ToJsonString();
        var root = JsonNode.Parse(json)!;

        foreach (var kvp in updates)
        {
            var pathSegments = kvp.Key.Split('.');
            SetValueAtPath(root, pathSegments, kvp.Value);
        }

        return root.Deserialize<T>(JsonSerializerOptions)!;
    }

    private static void SetValueAtPath(JsonNode root, string[] pathSegments, object? value)
    {
        JsonNode current = root;
        for (int i = 0; i < pathSegments.Length - 1; i++)
        {
            current = NavigateToSegment(current, pathSegments[i]);
        }

        var finalSegment = pathSegments[^1];
        var arrayMatch = ArrayAccessPattern().Match(finalSegment);

        if (arrayMatch.Success)
        {
            var arrayProperty = arrayMatch.Groups["property"].Value;
            var identifier = arrayMatch.Groups["identifier"].Value;

            var arrayNode = current[arrayProperty] as JsonArray
                ?? throw new InvalidOperationException($"Property '{arrayProperty}' is not an array");

            var valueNode = value is null ? null : JsonSerializer.SerializeToNode(value, JsonSerializerOptions);

            if (TryFindArrayItemByIdentifier(arrayNode, identifier, out var itemIndex))
            {
                arrayNode[itemIndex] = valueNode;
            }
            else
            {
                arrayNode.Add(valueNode);
            }
        }
        else if (current is JsonObject jsonObject)
        {
            var valueNode = value is null ? null : JsonSerializer.SerializeToNode(value, JsonSerializerOptions);
            jsonObject[finalSegment] = valueNode;
        }
        else
        {
            throw new InvalidOperationException(
                $"Cannot set property '{finalSegment}' on non-object node at path '{string.Join(".", pathSegments[..^1])}'");
        }
    }

    private static JsonNode NavigateToSegment(JsonNode current, string segment)
    {
        var arrayMatch = ArrayAccessPattern().Match(segment);

        if (arrayMatch.Success)
        {
            var arrayProperty = arrayMatch.Groups["property"].Value;
            var identifier = arrayMatch.Groups["identifier"].Value;

            var arrayNode = current[arrayProperty] as JsonArray
                ?? throw new InvalidOperationException($"Property '{arrayProperty}' is not an array or not found");

            var item = FindArrayItemByIdentifier(arrayNode, identifier, arrayProperty);
            return item;
        }

        return current[segment]
            ?? throw new InvalidOperationException($"Path segment '{segment}' not found");
    }

    private static bool TryFindArrayItemByIdentifier(JsonArray array, string identifier, out int index)
    {
        for (int i = 0; i < array.Count; i++)
        {
            var item = array[i];
            if (item is JsonObject obj && HasMatchingStringProperty(obj, identifier))
            {
                index = i;
                return true;
            }
        }

        index = -1;
        return false;
    }

    private static JsonNode FindArrayItemByIdentifier(JsonArray array, string identifier, string arrayProperty)
    {
        if (TryFindArrayItemByIdentifier(array, identifier, out var index))
        {
            return array[index]!;
        }

        throw new InvalidOperationException(
            $"No item with identifier '{identifier}' found in array '{arrayProperty}'");
    }

    private static bool HasMatchingStringProperty(JsonObject obj, string identifier)
    {
        foreach (var property in obj)
        {
            if (property.Value is JsonValue jsonValue &&
                jsonValue.TryGetValue<string>(out var stringValue) &&
                stringValue == identifier)
            {
                return true;
            }
        }
        return false;
    }
}