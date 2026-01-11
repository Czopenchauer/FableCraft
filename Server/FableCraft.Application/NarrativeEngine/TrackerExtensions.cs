using FableCraft.Infrastructure.Persistence.Entities.Adventure;

namespace FableCraft.Application.NarrativeEngine;

public static class TrackerExtensions
{
    public static Dictionary<string, object> ConvertToSystemJson(params FieldDefinition[] fields)
    {
        var dict = new Dictionary<string, object>();

        foreach (var field in fields)
        {
            if (field is { Type: FieldType.ForEachObject, HasNestedFields: true })
            {
                dict[field.Name] = new object[] { ConvertToSystemJson(field.NestedFields) };
            }
            else if (field is { Type: FieldType.Object, HasNestedFields: true })
            {
                dict[field.Name] = ConvertToSystemJson(field.NestedFields);
            }
            else if (field.DefaultValue != null)
            {
                dict[field.Name] = new
                {
                    field.Prompt,
                    field.DefaultValue,
                    field.ExampleValues
                };
            }
        }

        return dict;
    }

    public static Dictionary<string, object> ConvertToOutputJson(params FieldDefinition[] fields)
    {
        var dict = new Dictionary<string, object>();

        foreach (var field in fields)
        {
            if (field is { Type: FieldType.ForEachObject, HasNestedFields: true })
            {
                dict[field.Name] = new object[] { ConvertToOutputJson(field.NestedFields) };
            }
            else if (field is { Type: FieldType.Object, HasNestedFields: true })
            {
                dict[field.Name] = ConvertToOutputJson(field.NestedFields);
            }
            else if (field.DefaultValue != null)
            {
                dict[field.Name] = GetDefaultValue(field);
            }
        }

        return dict;

        object GetDefaultValue(FieldDefinition field)
        {
            return field.Type switch
                   {
                       FieldType.Array => new object[1],
                       FieldType.Object => new { },
                       FieldType.String => "",
                       _ => throw new NotSupportedException($"Field type {field.Type} is not supported.")
                   };
        }
    }
}