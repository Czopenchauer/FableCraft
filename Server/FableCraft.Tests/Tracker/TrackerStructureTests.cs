using System.Text.Json;

using FableCraft.Infrastructure.Persistence.Entities;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;

namespace FableCraft.Tests.Tracker;

public class TrackerStructureTests
{
    private readonly static JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Test]
    public async Task Story_ShouldHaveCompleteStructure()
    {
        // Act
        var structure = JsonSerializer.Deserialize<TrackerStructure>(TestTracker.InputJson, JsonOptions);

        // Assert
        await Assert.That(structure).IsNotNull();
        await Assert.That(structure!.Story).IsNotNull();
        await Assert.That(structure.Story!.Length).IsEqualTo(6);

        var expectedFields = new[] { "Time", "Weather", "Location", "PrimaryTopic", "EmotionalTone", "InteractionTheme" };
        var actualFieldNames = structure.Story.Select(f => f.Name).ToArray();

        foreach (var expectedField in expectedFields)
        {
            await Assert.That(actualFieldNames).Contains(expectedField);
        }

        foreach (var field in structure.Story)
        {
            await Assert.That(field.IsValid()).IsTrue().Because("Field should be valid");
            await Assert.That(field.Type).IsEqualTo(FieldType.String);
            await Assert.That(field.Name).IsNotNull();
            await Assert.That(field.Prompt).IsNotNull();
            await Assert.That(field.DefaultValue).IsNotNull();
            await Assert.That(field.ExampleValues).IsNotNull();
            await Assert.That(field.ExampleValues!.Count).IsEqualTo(3);
            await Assert.That(field.HasNestedFields).IsFalse().Because("Field should not have nested fields");
        }

        var timeField = structure.Story.First(f => f.Name == "Time");
        await Assert.That(timeField.DefaultValue?.ToString()).IsEqualTo("2024-10-16T09:15:30");
        await Assert.That(timeField.Prompt ?? string.Empty).Contains("Adjust time in small increments");

        var locationField = structure.Story.First(f => f.Name == "Location");
        await Assert.That(locationField.DefaultValue?.ToString() ?? string.Empty).Contains("Conference Room B");
        await Assert.That(locationField.Prompt).StartsWith("Provide a detailed and specific location");
    }

    [Test]
    public async Task CharactersPresent_ShouldHaveCompleteStructure()
    {
        // Act
        var structure = JsonSerializer.Deserialize<TrackerStructure>(TestTracker.InputJson, JsonOptions);

        // Assert
        await Assert.That(structure).IsNotNull();
        await Assert.That(structure!.CharactersPresent).IsNotNull();
    }

    [Test]
    public async Task MainCharacter_ShouldHaveCompleteStructure()
    {
        // Act
        var structure = JsonSerializer.Deserialize<TrackerStructure>(TestTracker.InputJson, JsonOptions);

        // Assert
        await Assert.That(structure).IsNotNull();
        await Assert.That(structure!.MainCharacter).IsNotNull();
        await Assert.That(structure.MainCharacter!.Length).IsEqualTo(11);

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

        foreach (var field in structure.MainCharacter)
        {
            await Assert.That(field.IsValid()).IsTrue().Because("Field should be valid");
        }

        var stringFields = structure.MainCharacter.Where(f => f.Name != "Inventory").ToArray();
        await Assert.That(stringFields.Length).IsEqualTo(10);
        foreach (var field in stringFields)
        {
            await Assert.That(field.Type).IsEqualTo(FieldType.String);
            await Assert.That(field.DefaultValue).IsNotNull();
            await Assert.That(field.ExampleValues).IsNotNull();
            await Assert.That(field.HasNestedFields).IsFalse().Because("Field should not have nested fields");
        }

        var inventoryField = structure.MainCharacter.First(f => f.Name == "Inventory");
        await Assert.That(inventoryField.Type).IsEqualTo(FieldType.ForEachObject);
        await Assert.That(inventoryField.DefaultValue).IsNull();
        await Assert.That(inventoryField.ExampleValues).IsNull();
        await Assert.That(inventoryField.HasNestedFields).IsTrue().Because("Inventory field should have nested fields");
        await Assert.That(inventoryField.NestedFields).IsNotNull();
        await Assert.That(inventoryField.NestedFields!.Length).IsEqualTo(4);

        var nestedFieldNames = inventoryField.NestedFields.Select(f => f.Name).ToArray();
        await Assert.That(nestedFieldNames).Contains("ItemName");
        await Assert.That(nestedFieldNames).Contains("Description");
        await Assert.That(nestedFieldNames).Contains("Quantity");
        await Assert.That(nestedFieldNames).Contains("Location");

        foreach (var field in inventoryField.NestedFields)
        {
            await Assert.That(field.IsValid()).IsTrue().Because("Nested field should be valid");
            await Assert.That(field.Type).IsEqualTo(FieldType.String);
            await Assert.That(field.DefaultValue).IsNotNull();
            await Assert.That(field.ExampleValues).IsNotNull();
        }

        var itemNameField = inventoryField.NestedFields.First(f => f.Name == "ItemName");
        await Assert.That(itemNameField.DefaultValue?.ToString()).IsEqualTo("Smartphone");
        await Assert.That(itemNameField.ExampleValues?.Count).IsEqualTo(4);

        var locationNestedField = inventoryField.NestedFields.First(f => f.Name == "Location");
        await Assert.That(locationNestedField.DefaultValue?.ToString()).IsEqualTo("Right pocket");

        var genderField = structure.MainCharacter.First(f => f.Name == "Gender");
        await Assert.That(genderField.DefaultValue?.ToString()).IsEqualTo("Female");
        await Assert.That(genderField.ExampleValues?.Count).IsEqualTo(2);

        var outfitField = structure.MainCharacter.First(f => f.Name == "Outfit");
        var defaultOutfit = outfitField.DefaultValue?.ToString();
        await Assert.That(defaultOutfit ?? string.Empty).Contains("Navy blue blazer");
        await Assert.That(defaultOutfit ?? string.Empty).Contains("Black leather pumps");
    }

    [Test]
    public async Task Characters_ShouldHaveCompleteStructure()
    {
        // Act
        var structure = JsonSerializer.Deserialize<TrackerStructure>(TestTracker.InputJson, JsonOptions);

        // Assert
        await Assert.That(structure).IsNotNull();
        await Assert.That(structure!.Characters).IsNotNull();
        await Assert.That(structure.Characters!.Length).IsEqualTo(10);

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

        await Assert.That(actualFieldNames).DoesNotContain("Inventory");

        foreach (var field in structure.Characters)
        {
            await Assert.That(field.IsValid()).IsTrue();
            await Assert.That(field.Type).IsEqualTo(FieldType.String);
            await Assert.That(field.Name).IsNotNull();
            await Assert.That(field.Prompt).IsNotNull();
            await Assert.That(field.DefaultValue).IsNotNull();
            await Assert.That(field.ExampleValues).IsNotNull();
            await Assert.That(field.HasNestedFields).IsFalse();
        }

        var genderField = structure.Characters.First(f => f.Name == "Gender");
        await Assert.That(genderField.DefaultValue?.ToString()).IsEqualTo("Male");
        await Assert.That(genderField.ExampleValues).IsNotNull();
        await Assert.That(genderField.ExampleValues!.Count).IsEqualTo(2);

        var ageField = structure.Characters.First(f => f.Name == "Age");
        await Assert.That(ageField.DefaultValue?.ToString()).IsEqualTo("28");

        var hairField = structure.Characters.First(f => f.Name == "Hair");
        await Assert.That(hairField.DefaultValue?.ToString()).IsEqualTo("Short black hair, neatly combed");

        var makeupField = structure.Characters.First(f => f.Name == "Makeup");
        await Assert.That(makeupField.DefaultValue?.ToString()).IsEqualTo("None");

        var outfitField = structure.Characters.First(f => f.Name == "Outfit");
        var defaultOutfit = outfitField.DefaultValue?.ToString();
        await Assert.That(defaultOutfit ?? string.Empty).Contains("Dark gray suit");
        await Assert.That(defaultOutfit ?? string.Empty).Contains("Black dress shoes");
    }
}
