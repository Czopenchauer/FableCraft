using System.Text.Json;

using FableCraft.Infrastructure.Persistence.Entities;

namespace FableCraft.Tests.Tracker;

public class TrackerStructureTests
{
    private readonly static JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public void Story_ShouldHaveCompleteStructure()
    {
        // Act
        var structure = JsonSerializer.Deserialize<TrackerStructure>(TestTracker.InputJson, JsonOptions);

        // Assert
        Assert.NotNull(structure);
        Assert.NotNull(structure.Story);
        Assert.Equal(6, structure.Story.Length);

        var expectedFields = new[] { "Time", "Weather", "Location", "PrimaryTopic", "EmotionalTone", "InteractionTheme" };
        var actualFieldNames = structure.Story.Select(f => f.Name).ToArray();

        foreach (var expectedField in expectedFields)
        {
            Assert.Contains(expectedField, actualFieldNames);
        }

        Assert.All(structure.Story,
            field =>
            {
                Assert.True(field.IsValid(), "Field should be valid");
                Assert.Equal(FieldType.String, field.Type);
                Assert.NotNull(field.Name);
                Assert.NotNull(field.Prompt);
                Assert.NotNull(field.DefaultValue);
                Assert.NotNull(field.ExampleValues);
                Assert.Equal(3, field.ExampleValues.Count);
                Assert.False(field.HasNestedFields, "Field should not have nested fields");
            });

        var timeField = structure.Story.First(f => f.Name == "Time");
        Assert.Equal("2024-10-16T09:15:30", timeField.DefaultValue?.ToString());
        Assert.Contains("Adjust time in small increments", timeField.Prompt ?? string.Empty);

        var locationField = structure.Story.First(f => f.Name == "Location");
        Assert.Contains("Conference Room B", locationField.DefaultValue?.ToString() ?? string.Empty);
        Assert.StartsWith("Provide a detailed and specific location", locationField.Prompt);
    }

    [Fact]
    public void CharactersPresent_ShouldHaveCompleteStructure()
    {
        // Act
        var structure = JsonSerializer.Deserialize<TrackerStructure>(TestTracker.InputJson, JsonOptions);

        // Assert
        Assert.NotNull(structure);
        Assert.NotNull(structure.CharactersPresent);
        // Assert.Equal(2, structure.CharactersPresent);
        // Assert.Contains("Emma Thompson", structure.CharactersPresent);
        // Assert.Contains("James Miller", structure.CharactersPresent);
        // Assert.All(structure.CharactersPresent, name => Assert.False(string.IsNullOrWhiteSpace(name)));
    }

    [Fact]
    public void MainCharacter_ShouldHaveCompleteStructure()
    {
        // Act
        var structure = JsonSerializer.Deserialize<TrackerStructure>(TestTracker.InputJson, JsonOptions);

        // Assert
        Assert.NotNull(structure);
        Assert.NotNull(structure.MainCharacter);
        Assert.Equal(11, structure.MainCharacter.Length);

        var expectedFields = new[]
        {
            "Name", "Gender", "Age", "Hair", "Makeup", "Outfit", "StateOfDress",
            "PostureAndInteraction", "Traits", "Children", "Inventory"
        };
        var actualFieldNames = structure.MainCharacter.Select(f => f.Name).ToArray();

        foreach (var expectedField in expectedFields)
        {
            Assert.Contains(expectedField, (IEnumerable<string>)actualFieldNames);
        }

        Assert.All(structure.MainCharacter, field => Assert.True(field.IsValid(), "Field should be valid"));

        var stringFields = structure.MainCharacter.Where(f => f.Name != "Inventory").ToArray();
        Assert.Equal(10, stringFields.Length);
        Assert.All(stringFields,
            field =>
            {
                Assert.Equal(FieldType.String, field.Type);
                Assert.NotNull(field.DefaultValue);
                Assert.NotNull(field.ExampleValues);
                Assert.False(field.HasNestedFields, "Field should not have nested fields");
            });

        var inventoryField = structure.MainCharacter.First(f => f.Name == "Inventory");
        Assert.Equal(FieldType.ForEachObject, inventoryField.Type);
        Assert.Null(inventoryField.DefaultValue);
        Assert.Null(inventoryField.ExampleValues);
        Assert.True(inventoryField.HasNestedFields, "Inventory field should have nested fields");
        Assert.NotNull(inventoryField.NestedFields);
        Assert.Equal(4, inventoryField.NestedFields.Length);

        var nestedFieldNames = inventoryField.NestedFields.Select(f => f.Name).ToArray();
        Assert.Contains("ItemName", (IEnumerable<string>)nestedFieldNames);
        Assert.Contains("Description", (IEnumerable<string>)nestedFieldNames);
        Assert.Contains("Quantity", (IEnumerable<string>)nestedFieldNames);
        Assert.Contains("Location", (IEnumerable<string>)nestedFieldNames);

        Assert.All(inventoryField.NestedFields,
            field =>
            {
                Assert.True(field.IsValid(), "Nested field should be valid");
                Assert.Equal(FieldType.String, field.Type);
                Assert.NotNull(field.DefaultValue);
                Assert.NotNull(field.ExampleValues);
            });

        var itemNameField = inventoryField.NestedFields.First(f => f.Name == "ItemName");
        Assert.Equal("Smartphone", itemNameField.DefaultValue?.ToString());
        Assert.Equal(4, itemNameField.ExampleValues?.Count);

        var locationNestedField = inventoryField.NestedFields.First(f => f.Name == "Location");
        Assert.Equal("Right pocket", locationNestedField.DefaultValue?.ToString());

        var genderField = structure.MainCharacter.First(f => f.Name == "Gender");
        Assert.Equal("Female", genderField.DefaultValue?.ToString());
        Assert.Equal(2, genderField.ExampleValues?.Count);

        var outfitField = structure.MainCharacter.First(f => f.Name == "Outfit");
        var defaultOutfit = outfitField.DefaultValue?.ToString();
        Assert.Contains("Navy blue blazer", defaultOutfit ?? string.Empty);
        Assert.Contains("Black leather pumps", defaultOutfit ?? string.Empty);
    }

    [Fact]
    public void Characters_ShouldHaveCompleteStructure()
    {
        // Act
        var structure = JsonSerializer.Deserialize<TrackerStructure>(TestTracker.InputJson, JsonOptions);

        // Assert
        Assert.NotNull(structure);
        Assert.NotNull(structure.Characters);
        Assert.Equal(10, structure.Characters.Length);

        var expectedFields = new[]
        {
            "Name", "Gender", "Age", "Hair", "Makeup", "Outfit", "StateOfDress",
            "PostureAndInteraction", "Traits", "Children"
        };
        var actualFieldNames = structure.Characters.Select(f => f.Name).ToArray();

        foreach (var expectedField in expectedFields)
        {
            Assert.Contains(expectedField, (IEnumerable<string>)actualFieldNames);
        }

        Assert.DoesNotContain("Inventory", (IEnumerable<string>)actualFieldNames);

        Assert.All(structure.Characters,
            field =>
            {
                Assert.True(field.IsValid());
                Assert.Equal(FieldType.String, field.Type);
                Assert.NotNull(field.Name);
                Assert.NotNull(field.Prompt);
                Assert.NotNull(field.DefaultValue);
                Assert.NotNull(field.ExampleValues);
                Assert.False(field.HasNestedFields);
            });

        var genderField = structure.Characters.First(f => f.Name == "Gender");
        Assert.Equal("Male", genderField.DefaultValue?.ToString());
        Assert.NotNull(genderField.ExampleValues);
        Assert.Equal(2, genderField.ExampleValues.Count);

        var ageField = structure.Characters.First(f => f.Name == "Age");
        Assert.Equal("28", ageField.DefaultValue?.ToString());

        var hairField = structure.Characters.First(f => f.Name == "Hair");
        Assert.Equal("Short black hair, neatly combed", hairField.DefaultValue?.ToString());

        var makeupField = structure.Characters.First(f => f.Name == "Makeup");
        Assert.Equal("None", makeupField.DefaultValue?.ToString());

        var outfitField = structure.Characters.First(f => f.Name == "Outfit");
        var defaultOutfit = outfitField.DefaultValue?.ToString();
        Assert.Contains("Dark gray suit", defaultOutfit ?? string.Empty);
        Assert.Contains("Black dress shoes", defaultOutfit ?? string.Empty);
    }
}