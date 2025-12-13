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
}