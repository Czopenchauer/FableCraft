using System.Text.Json;

using FableCraft.Application.NarrativeEngine;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;

namespace FableCraft.Tests.Tracker;

public class TrackerStructureTests
{
    private readonly static JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static TrackerStructure DeserializeStructure(string json)
    {
        var structure = JsonSerializer.Deserialize<TrackerStructure>(json, JsonOptions);
        if (structure is null)
        {
            throw new InvalidOperationException("Failed to deserialize TrackerStructure");
        }
        return structure;
    }

    private async static Task AssertFieldIsValid(FieldDefinition field, string? because = null)
    {
        await Assert.That(field.Name).IsNotNull();
        await Assert.That(field.Prompt).IsNotNull();
    }

    private async static Task AssertStringField(FieldDefinition field, bool hasExampleValues = true)
    {
        await AssertFieldIsValid(field);
        await Assert.That(field.Type).IsEqualTo(FieldType.String);
        await Assert.That(field.DefaultValue).IsNotNull();
        await Assert.That(field.HasNestedFields).IsFalse().Because("String field should not have nested fields");

        if (hasExampleValues)
        {
            await Assert.That(field.ExampleValues).IsNotNull();
        }
    }

    #region Story Tests

    [Test]
    public async Task Story_ShouldDeserializeAllFields()
    {
        // Act
        var structure = DeserializeStructure(TestTracker.InputJson);

        // Assert
        await Assert.That(structure.Story).IsNotNull();
        await Assert.That(structure.Story.Length).IsEqualTo(6);

        var expectedFields = new[] { "Time", "Weather", "Location", "PrimaryTopic", "EmotionalTone", "InteractionTheme" };
        var actualFieldNames = structure.Story.Select(f => f.Name).ToArray();

        foreach (var expectedField in expectedFields)
        {
            await Assert.That(actualFieldNames).Contains(expectedField);
        }
    }

    [Test]
    public async Task Story_AllFieldsShouldBeValidStringTypes()
    {
        // Act
        var structure = DeserializeStructure(TestTracker.InputJson);

        // Assert
        foreach (FieldDefinition field in structure.Story)
        {
            await AssertStringField(field);
            await Assert.That(field.ExampleValues!.Count).IsEqualTo(3)
                .Because($"Field '{field.Name}' should have 3 example values");
        }
    }

    [Test]
    public async Task Story_TimeField_ShouldHaveCorrectDefaults()
    {
        // Act
        var structure = DeserializeStructure(TestTracker.InputJson);
        FieldDefinition timeField = structure.Story.First(f => f.Name == "Time");

        // Assert
        await Assert.That(timeField.DefaultValue?.ToString()).IsEqualTo("2024-10-16T09:15:30");
        await Assert.That(timeField.Prompt ?? string.Empty).Contains("Adjust time in small increments");
        await Assert.That(timeField.Prompt ?? string.Empty).Contains("ISO 8601");
    }

    [Test]
    public async Task Story_LocationField_ShouldHaveCorrectDefaults()
    {
        // Act
        var structure = DeserializeStructure(TestTracker.InputJson);
        FieldDefinition locationField = structure.Story.First(f => f.Name == "Location");

        // Assert
        await Assert.That(locationField.DefaultValue?.ToString() ?? string.Empty).Contains("Conference Room B");
        await Assert.That(locationField.Prompt).StartsWith("Provide a detailed and specific location");
    }

    #endregion

    #region MainCharacter Tests

    [Test]
    public async Task MainCharacter_ShouldDeserializeAllFields()
    {
        // Act
        var structure = DeserializeStructure(TestTrackerV2.Tracker);

        // Assert
        await Assert.That(structure.MainCharacter).IsNotNull();
        await Assert.That(structure.MainCharacter.Length).IsEqualTo(11);

        var expectedFields = new[]
        {
            "Name", "Gender", "Age", "Hair", "Makeup", "Outfit", "StateOfDress",
            "PostureAndInteraction", "Traits", "Children", "Inventory"
        };
        var actualFieldNames = structure.MainCharacter.Select(f => f.Name).ToArray();

        foreach (var expectedField in expectedFields)
        {
            await Assert.That(actualFieldNames).Contains(expectedField);
        }
    }

    [Test]
    public async Task MainCharacter_AllFieldsShouldBeValid()
    {
        // Act
        var structure = DeserializeStructure(TestTracker.InputJson);

        // Assert
        foreach (FieldDefinition field in structure.MainCharacter)
        {
            await AssertFieldIsValid(field);
        }
    }

    [Test]
    public async Task MainCharacter_StringFieldsShouldHaveCorrectStructure()
    {
        // Act
        var structure = DeserializeStructure(TestTracker.InputJson);
        var stringFields = structure.MainCharacter.Where(f => f.Name != "Inventory").ToArray();

        // Assert
        await Assert.That(stringFields.Length).IsEqualTo(10);

        foreach (FieldDefinition field in stringFields)
        {
            await AssertStringField(field);
        }
    }

    [Test]
    public async Task MainCharacter_InventoryField_ShouldBeForEachObjectType()
    {
        // Act
        var structure = DeserializeStructure(TestTracker.InputJson);
        FieldDefinition inventoryField = structure.MainCharacter.First(f => f.Name == "Inventory");

        // Assert
        await Assert.That(inventoryField.Type).IsEqualTo(FieldType.ForEachObject);
        await Assert.That(inventoryField.DefaultValue).IsNull();
        await Assert.That(inventoryField.ExampleValues).IsNull();
        await Assert.That(inventoryField.HasNestedFields).IsTrue().Because("Inventory field should have nested fields");
    }

    [Test]
    public async Task MainCharacter_InventoryNestedFields_ShouldHaveCorrectStructure()
    {
        // Act
        var structure = DeserializeStructure(TestTracker.InputJson);
        FieldDefinition inventoryField = structure.MainCharacter.First(f => f.Name == "Inventory");

        // Assert
        await Assert.That(inventoryField.NestedFields).IsNotNull();
        await Assert.That(inventoryField.NestedFields!.Length).IsEqualTo(4);

        var nestedFieldNames = inventoryField.NestedFields.Select(f => f.Name).ToArray();
        await Assert.That(nestedFieldNames).Contains("ItemName");
        await Assert.That(nestedFieldNames).Contains("Description");
        await Assert.That(nestedFieldNames).Contains("Quantity");
        await Assert.That(nestedFieldNames).Contains("Location");

        foreach (FieldDefinition field in inventoryField.NestedFields)
        {
            await AssertStringField(field);
        }
    }

    [Test]
    public async Task MainCharacter_InventoryNestedFields_ShouldHaveCorrectDefaults()
    {
        // Act
        var structure = DeserializeStructure(TestTracker.InputJson);
        FieldDefinition inventoryField = structure.MainCharacter.First(f => f.Name == "Inventory");

        // Assert
        FieldDefinition itemNameField = inventoryField.NestedFields!.First(f => f.Name == "ItemName");
        await Assert.That(itemNameField.DefaultValue?.ToString()).IsEqualTo("Smartphone");
        await Assert.That(itemNameField.ExampleValues?.Count).IsEqualTo(4);

        FieldDefinition locationNestedField = inventoryField.NestedFields!.First(f => f.Name == "Location");
        await Assert.That(locationNestedField.DefaultValue?.ToString()).IsEqualTo("Right pocket");
    }

    [Test]
    public async Task MainCharacter_GenderField_ShouldHaveCorrectDefaults()
    {
        // Act
        var structure = DeserializeStructure(TestTracker.InputJson);
        FieldDefinition genderField = structure.MainCharacter.First(f => f.Name == "Gender");

        // Assert
        await Assert.That(genderField.DefaultValue?.ToString()).IsEqualTo("Female");
        await Assert.That(genderField.ExampleValues?.Count).IsEqualTo(2);
    }

    [Test]
    public async Task MainCharacter_OutfitField_ShouldHaveDetailedDefault()
    {
        // Act
        var structure = DeserializeStructure(TestTracker.InputJson);
        FieldDefinition outfitField = structure.MainCharacter.First(f => f.Name == "Outfit");
        var defaultOutfit = outfitField.DefaultValue?.ToString();

        // Assert
        await Assert.That(defaultOutfit ?? string.Empty).Contains("Navy blue blazer");
        await Assert.That(defaultOutfit ?? string.Empty).Contains("Black leather pumps");
    }

    #endregion

    #region Characters Tests

    [Test]
    public async Task Characters_ShouldDeserializeAllFields()
    {
        // Act
        var structure = DeserializeStructure(TestTracker.InputJson);

        // Assert
        await Assert.That(structure.Characters).IsNotNull();
        await Assert.That(structure.Characters.Length).IsEqualTo(10);

        var expectedFields = new[]
        {
            "Name", "Gender", "Age", "Hair", "Makeup", "Outfit", "StateOfDress",
            "PostureAndInteraction", "Traits", "Children"
        };
        var actualFieldNames = structure.Characters.Select(f => f.Name).ToArray();

        foreach (var expectedField in expectedFields)
        {
            await Assert.That(actualFieldNames).Contains(expectedField);
        }
    }

    [Test]
    public async Task Characters_ShouldNotHaveInventoryField()
    {
        // Act
        var structure = DeserializeStructure(TestTracker.InputJson);
        var actualFieldNames = structure.Characters.Select(f => f.Name).ToArray();

        // Assert - Characters should not have Inventory (only MainCharacter has it)
        await Assert.That(actualFieldNames).DoesNotContain("Inventory");
    }

    [Test]
    public async Task Characters_AllFieldsShouldBeValidStringTypes()
    {
        // Act
        var structure = DeserializeStructure(TestTracker.InputJson);

        // Assert
        foreach (FieldDefinition field in structure.Characters)
        {
            await AssertStringField(field);
        }
    }

    [Test]
    public async Task Characters_GenderField_ShouldHaveCorrectDefaults()
    {
        // Act
        var structure = DeserializeStructure(TestTracker.InputJson);
        FieldDefinition genderField = structure.Characters.First(f => f.Name == "Gender");

        // Assert - Characters default to Male (different from MainCharacter which defaults to Female)
        await Assert.That(genderField.DefaultValue?.ToString()).IsEqualTo("Male");
        await Assert.That(genderField.ExampleValues).IsNotNull();
        await Assert.That(genderField.ExampleValues!.Count).IsEqualTo(2);
    }

    [Test]
    public async Task Characters_ShouldHaveCorrectDefaultValues()
    {
        // Act
        var structure = DeserializeStructure(TestTracker.InputJson);

        // Assert
        FieldDefinition ageField = structure.Characters.First(f => f.Name == "Age");
        await Assert.That(ageField.DefaultValue?.ToString()).IsEqualTo("28");

        FieldDefinition hairField = structure.Characters.First(f => f.Name == "Hair");
        await Assert.That(hairField.DefaultValue?.ToString()).IsEqualTo("Short black hair, neatly combed");

        FieldDefinition makeupField = structure.Characters.First(f => f.Name == "Makeup");
        await Assert.That(makeupField.DefaultValue?.ToString()).IsEqualTo("None");
    }

    [Test]
    public async Task Characters_OutfitField_ShouldHaveDetailedDefault()
    {
        // Act
        var structure = DeserializeStructure(TestTracker.InputJson);
        FieldDefinition outfitField = structure.Characters.First(f => f.Name == "Outfit");
        var defaultOutfit = outfitField.DefaultValue?.ToString();

        // Assert
        await Assert.That(defaultOutfit ?? string.Empty).Contains("Dark gray suit");
        await Assert.That(defaultOutfit ?? string.Empty).Contains("Black dress shoes");
    }

    #endregion

    #region Conversion Tests

    [Test]
    public async Task ConvertToSystemJson_ShouldProduceValidJsonForStory()
    {
        // Act
        var structure = DeserializeStructure(TestTracker.InputJson);
        var systemJson = TrackerExtensions.ConvertToSystemJson(structure.Story);

        // Assert
        await Assert.That(systemJson).IsNotNull();
        await Assert.That(systemJson.Count).IsEqualTo(6);
        await Assert.That(systemJson.ContainsKey("Time")).IsTrue();
        await Assert.That(systemJson.ContainsKey("Location")).IsTrue();
    }

    [Test]
    public async Task ConvertToOutputJson_ShouldProduceValidJsonForStory()
    {
        // Act
        var structure = DeserializeStructure(TestTracker.InputJson);
        var outputJson = TrackerExtensions.ConvertToOutputJson(structure.Story);

        // Assert
        await Assert.That(outputJson).IsNotNull();
        await Assert.That(outputJson.Count).IsEqualTo(6);
    }

    [Test]
    public async Task ConvertToSystemJson_ShouldProduceValidJsonForMainCharacter()
    {
        // Act
        var structure = DeserializeStructure(TestTracker.InputJson);
        var systemJson = TrackerExtensions.ConvertToSystemJson(structure.MainCharacter);

        // Assert
        await Assert.That(systemJson).IsNotNull();
        await Assert.That(systemJson.Count).IsEqualTo(11);
        await Assert.That(systemJson.ContainsKey("Name")).IsTrue();
        await Assert.That(systemJson.ContainsKey("Inventory")).IsTrue();
    }

    [Test]
    public async Task ConvertToOutputJson_ShouldProduceValidJsonForMainCharacter()
    {
        // Act
        var structure = DeserializeStructure(TestTracker.InputJson);
        var outputJson = TrackerExtensions.ConvertToOutputJson(structure.MainCharacter);

        // Assert
        await Assert.That(outputJson).IsNotNull();
        await Assert.That(outputJson.Count).IsEqualTo(11);
    }

    [Test]
    public async Task ConvertToSystemJson_ShouldProduceSerializableJson()
    {
        // Arrange
        var structure = DeserializeStructure(TestTracker.InputJson);
        var systemJson = TrackerExtensions.ConvertToSystemJson(structure.Story);

        // Act
        var serialized = JsonSerializer.Serialize(systemJson, JsonOptions);

        // Assert
        await Assert.That(serialized).IsNotNull();
        await Assert.That(string.IsNullOrEmpty(serialized)).IsFalse();
    }

    #endregion

    #region Edge Cases

    [Test]
    public async Task FieldDefinition_HasNestedFields_ShouldReturnFalseForNull()
    {
        // Arrange
        var field = new FieldDefinition
        {
            Name = "Test",
            Prompt = "Test prompt",
            NestedFields = null
        };

        // Assert
        await Assert.That(field.HasNestedFields).IsFalse();
    }

    [Test]
    public async Task FieldDefinition_HasNestedFields_ShouldReturnFalseForEmptyArray()
    {
        // Arrange
        var field = new FieldDefinition
        {
            Name = "Test",
            Prompt = "Test prompt",
            NestedFields = []
        };

        // Assert
        await Assert.That(field.HasNestedFields).IsFalse();
    }

    [Test]
    public async Task FieldDefinition_HasNestedFields_ShouldReturnTrueForPopulatedArray()
    {
        // Arrange
        var field = new FieldDefinition
        {
            Name = "Test",
            Prompt = "Test prompt",
            NestedFields =
            [
                new FieldDefinition { Name = "Nested", Prompt = "Nested prompt" }
            ]
        };

        // Assert
        await Assert.That(field.HasNestedFields).IsTrue();
        await Assert.That(field.NestedFields).IsNotNull();
    }

    #endregion
}
