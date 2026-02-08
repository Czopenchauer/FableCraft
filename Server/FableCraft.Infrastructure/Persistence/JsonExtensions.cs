using System.Text.Json;

namespace FableCraft.Infrastructure.Persistence;

public static class JsonExtensions
{
    public static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true
    };

    public static string ToJsonString<T>(this T obj) => JsonSerializer.Serialize(obj, JsonSerializerOptions);

    public static string ToJsonString<T>(this T obj, JsonSerializerOptions options) => JsonSerializer.Serialize(obj, options);
}