using FableCraft.Infrastructure.Persistence.Entities;

namespace FableCraft.Application.NarrativeEngine.Tracker;

internal sealed class TrackerExampleModel : Dictionary<string, object>
{
    public TrackerExampleModel(TrackerStructure structure)
    {
        var story = ConvertFieldsToDict(structure.Story);
        Add(nameof(Infrastructure.Persistence.Entities.Tracker.MainCharacter), story);

        if (structure.CharactersPresent != null)
        {
            Add(nameof(Infrastructure.Persistence.Entities.Tracker.CharactersPresent), structure.CharactersPresent);
        }

        if (structure.MainCharacter != null)
        {
            var mainCharStats = ConvertFieldsToDict(structure.MainCharacter);
            Add(nameof(Infrastructure.Persistence.Entities.Tracker.MainCharacter), mainCharStats);
        }

        if (structure.Characters != null)
        {
            var charDict = ConvertFieldsToDict(structure.Characters);

            Add(nameof(Infrastructure.Persistence.Entities.Tracker.Characters), new object[] { charDict } );
        }

        return;

        Dictionary<string, object> ConvertFieldsToDict(FieldDefinition[] fields)
        {
            var dict = new Dictionary<string, object>();

            foreach (var field in fields)
            {
                if (field is { Type: FieldType.ForEachObject, HasNestedFields: true })
                {
                    dict[field.Name] = new List<Dictionary<string, object>>();
                }
                else if (field.DefaultValue != null)
                {
                    dict[field.Name] = field.DefaultValue.ToString()!;
                }
            }

            return dict;
        }
    }
}