using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Text.Unicode;

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

    public static string ToJsonString<T>(this T obj) => JsonSerializer.Serialize(obj, JsonSerializerOptions);

    public static string ToJsonString<T>(this T obj, JsonSerializerOptions options) => JsonSerializer.Serialize(obj, options);

    /// <summary>
    ///     Patches an object with updates specified using dot-notation paths.
    ///     Each key is a path like "psychology.emotional_baseline" and the value
    ///     is the complete object to replace at that path.
    ///     Supports array item access by identifier e.g.: "Skills[Consciousness Analysis]" or
    ///     "MagicAndAbilities.InstinctiveAbilities[Fire Breath].Power"
    ///     Dots inside brackets are preserved (e.g., "in_development[Curiosity vs. Discipline]")
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
            var pathSegments = SplitPathRespectingBrackets(kvp.Key);
            SetValueAtPath(root, pathSegments, kvp.Value);
        }

        return root.Deserialize<T>(JsonSerializerOptions)!;
    }

    private static string[] SplitPathRespectingBrackets(string path)
    {
        var segments = new List<string>();
        var currentSegment = new StringBuilder();
        var bracketDepth = 0;

        foreach (var c in path)
        {
            if (c == '[')
            {
                bracketDepth++;
                currentSegment.Append(c);
            }
            else if (c == ']')
            {
                bracketDepth--;
                currentSegment.Append(c);
            }
            else if (c == '.' && bracketDepth == 0)
            {
                if (currentSegment.Length > 0)
                {
                    segments.Add(currentSegment.ToString());
                    currentSegment.Clear();
                }
            }
            else
            {
                currentSegment.Append(c);
            }
        }

        if (currentSegment.Length > 0)
        {
            segments.Add(currentSegment.ToString());
        }

        return segments.ToArray();
    }

    private static void SetValueAtPath(JsonNode root, string[] pathSegments, object? value)
    {
        var current = root;
        for (var i = 0; i < pathSegments.Length - 1; i++)
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

            if (TryFindArrayItemByIdentifier(arrayNode, identifier, out var itemIndex))
            {
                if (value is null)
                {
                    arrayNode.RemoveAt(itemIndex);
                }
                else
                {
                    arrayNode[itemIndex] = JsonSerializer.SerializeToNode(value, JsonSerializerOptions);
                }
            }
            else if (value is not null)
            {
                arrayNode.Add(JsonSerializer.SerializeToNode(value, JsonSerializerOptions));
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
        for (var i = 0; i < array.Count; i++)
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
        var normalizedIdentifier = NormalizeIdentifier(identifier);

        foreach (var property in obj)
        {
            if (property.Value is JsonValue jsonValue && jsonValue.TryGetValue<string>(out var stringValue))
            {
                if (stringValue == normalizedIdentifier)
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    ///     Normalizes an identifier by stripping surrounding quotes if present.
    ///     Handles cases where AI returns paths like: in_development["Curiosity about Lily's anomaly"]
    ///     instead of: in_development[Curiosity about Lily's anomaly]
    /// </summary>
    private static string NormalizeIdentifier(string identifier)
    {
        if (identifier.Length >= 2 && identifier.StartsWith('"') && identifier.EndsWith('"'))
        {
            return identifier[1..^1];
        }

        if (identifier.Length >= 2 && identifier.StartsWith('\'') && identifier.EndsWith('\''))
        {
            return identifier[1..^1];
        }

        return identifier;
    }
}