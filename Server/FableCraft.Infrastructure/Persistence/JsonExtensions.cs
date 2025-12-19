using System.Text.Json;
using System.Text.Json.Nodes;

namespace FableCraft.Infrastructure.Persistence;

public static class JsonExtensions
{
    public readonly static JsonSerializerOptions JsonSerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true
    };

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
            current = current[pathSegments[i]]
                ?? throw new InvalidOperationException($"Path segment '{pathSegments[i]}' not found");
        }

        var finalProperty = pathSegments[^1];
        if (current is JsonObject jsonObject)
        {
            var valueNode = value is null ? null : JsonSerializer.SerializeToNode(value, JsonSerializerOptions);
            jsonObject[finalProperty] = valueNode;
        }
        else
        {
            throw new InvalidOperationException(
                $"Cannot set property '{finalProperty}' on non-object node at path '{string.Join(".", pathSegments[..^1])}'");
        }
    }
}