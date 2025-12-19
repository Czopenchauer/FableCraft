using System.Text.Json;

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

    public static T PatchWith<T>(this T original, IDictionary<string, object> updates)
    {
        if (updates.Count == 0)
        {
            return original;
        }

        var json = original.ToJsonString();
        var jsonObject = JsonSerializer.SerializeToNode(json)!;

        foreach (var kvp in updates)
        {
            var jsonPath = kvp.Key.Split(".");
            foreach (var path in jsonPath)
            {
                jsonObject = jsonObject![path];
            }

            jsonObject = JsonSerializer.SerializeToNode(kvp.Value);
            jsonObject = jsonObject!.Root;
        }

        return jsonObject.Deserialize<T>(JsonSerializerOptions)!;
    }
}