using System.Text.Json;
using FableCraft.Infrastructure.Persistence.Entities;

namespace FableCraft.Tests;

public class TrackerStructureTests
{
    private readonly static JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private const string InputJson = """
                                     {
                                       "story": [
                                         {
                                           "name": "Time",
                                           "type": "String",
                                           "prompt": "Adjust time in small increments for natural progression unless explicit directives indicate larger changes. Format: HH:MM:SS; MM/DD/YYYY (Day Name).",
                                           "defaultValue": "09:15:30; 10/16/2024 (Wednesday)",
                                           "exampleValues": [
                                             "09:15:30; 10/16/2024 (Wednesday)",
                                             "18:45:50; 10/16/2024 (Wednesday)",
                                             "15:10:20; 10/16/2024 (Wednesday)"
                                           ]
                                         },
                                         {
                                           "name": "Weather",
                                           "type": "String",
                                           "prompt": "Describe current weather concisely to set the scene.",
                                           "defaultValue": "Overcast, mild temperature",
                                           "exampleValues": [
                                             "Overcast, mild temperature",
                                             "Clear skies, warm evening",
                                             "Sunny, gentle sea breeze"
                                           ]
                                         },
                                         {
                                           "name": "Location",
                                           "type": "String",
                                           "prompt": "Provide a detailed and specific location, including exact places like rooms, landmarks, or stores, following this format: 'Specific Place, Building, City, State'.",
                                           "defaultValue": "Conference Room B, 12th Floor, Apex Corporation, New York, NY",
                                           "exampleValues": [
                                             "Conference Room B, 12th Floor, Apex Corporation, New York, NY",
                                             "Main Gym Hall, Maple Street Fitness Center, Denver, CO",
                                             "South Beach, Miami, FL"
                                           ]
                                         },
                                         {
                                           "name": "PrimaryTopic",
                                           "type": "String",
                                           "prompt": "One- or two-word topic describing main activity or focus of the scene.",
                                           "defaultValue": "Presentation",
                                           "exampleValues": ["Presentation", "Workout", "Relaxation"]
                                         },
                                         {
                                           "name": "EmotionalTone",
                                           "type": "String",
                                           "prompt": "One- or two-word topic describing dominant emotional atmosphere of the scene.",
                                           "defaultValue": "Tense",
                                           "exampleValues": ["Tense", "Focused", "Calm"]
                                         },
                                         {
                                           "name": "InteractionTheme",
                                           "type": "String",
                                           "prompt": "One- or two-word topic describing primary type of interactions or relationships in the scene.",
                                           "defaultValue": "Professional",
                                           "exampleValues": ["Professional", "Supportive", "Casual"]
                                         }
                                       ],
                                       "charactersPresent": [
                                         "Emma Thompson",
                                         "James Miller"
                                       ],
                                       "mainCharacterStats": [
                                         {
                                           "name": "Gender",
                                           "type": "String",
                                           "prompt": "A single word and an emoji for character gender.",
                                           "defaultValue": "Female",
                                           "exampleValues": ["Male", "Female"]
                                         },
                                         {
                                           "name": "Age",
                                           "type": "String",
                                           "prompt": "A single number displays character age based on narrative. Or 'Unknown' if unknown.",
                                           "defaultValue": "32",
                                           "exampleValues": ["Unknown", "18", "32"]
                                         },
                                         {
                                           "name": "Hair",
                                           "type": "String",
                                           "prompt": "Describe style only.",
                                           "defaultValue": "Shoulder-length blonde hair, styled straight",
                                           "exampleValues": [
                                             "Shoulder-length blonde hair, styled straight",
                                             "Short black hair, neatly combed",
                                             "Long curly brown hair, pulled back into a low bun"
                                           ]
                                         },
                                         {
                                           "name": "Makeup",
                                           "type": "String",
                                           "prompt": "Describe current makeup.",
                                           "defaultValue": "Natural look with light foundation and mascara",
                                           "exampleValues": [
                                             "Natural look with light foundation and mascara",
                                             "None",
                                             "Subtle eyeliner and nude lipstick"
                                           ]
                                         },
                                         {
                                           "name": "Outfit",
                                           "type": "String",
                                           "prompt": "List the complete outfit with color, fabric, and style details.",
                                           "defaultValue": "Navy blue blazer over a white silk blouse; Gray pencil skirt; Black leather belt; Sheer black stockings; Black leather pumps; Pearl necklace; Silver wristwatch",
                                           "exampleValues": [
                                             "Navy blue blazer over a white silk blouse; Gray pencil skirt; Black leather belt; Sheer black stockings; Black leather pumps; Pearl necklace; Silver wristwatch",
                                             "Dark gray suit; Light blue dress shirt; Navy tie with silver stripes; Black leather belt; Black dress shoes; Black socks",
                                             "Cream-colored blouse with ruffled collar; Black slacks; Brown leather belt; Brown ankle boots; Gold hoop earrings"
                                           ]
                                         },
                                         {
                                           "name": "StateOfDress",
                                           "type": "String",
                                           "prompt": "Describe how put-together or disheveled the character appears.",
                                           "defaultValue": "Professionally dressed, neat appearance",
                                           "exampleValues": [
                                             "Professionally dressed, neat appearance",
                                             "Workout attire, lightly perspiring",
                                             "Casual attire, relaxed"
                                           ]
                                         },
                                         {
                                           "name": "PostureAndInteraction",
                                           "type": "String",
                                           "prompt": "Describe physical posture, position relative to others or objects, and interactions.",
                                           "defaultValue": "Standing at the podium, presenting slides, holding a laser pointer",
                                           "exampleValues": [
                                             "Standing at the podium, presenting slides, holding a laser pointer",
                                             "Sitting at the conference table, taking notes on a laptop",
                                             "Lifting weights at the bench press, focused on form"
                                           ]
                                         },
                                         {
                                           "name": "Traits",
                                           "type": "String",
                                           "prompt": "Add or Remove trait based on Narrative. Format: '{trait}: {short description}'",
                                           "defaultValue": "No Traits",
                                           "exampleValues": [
                                             "No Traits",
                                             "Emotional Intelligence: deeply philosophical and sentimental",
                                             "Charismatic: naturally draws people in with charm and wit"
                                           ]
                                         },
                                         {
                                           "name": "Children",
                                           "type": "String",
                                           "prompt": "Add child after birth based on Narrative. Format: '{Birth Order}: {Name}, {Gender + Symbol}, child with {Other Parent}'",
                                           "defaultValue": "No Child",
                                           "exampleValues": [
                                             "No Child",
                                             "1st Born: Eve, Female, child with Harry"
                                           ]
                                         },
                                         {
                                           "name": "Inventory",
                                           "type": "ForEachObject",
                                           "prompt": "Track items the main character is carrying or has access to.",
                                           "defaultValue": null,
                                           "exampleValues": null,
                                           "nestedFields": [
                                             {
                                               "name": "ItemName",
                                               "type": "String",
                                               "prompt": "Name of the item.",
                                               "defaultValue": "Smartphone",
                                               "exampleValues": ["Smartphone", "Wallet", "Keys", "Notebook"]
                                             },
                                             {
                                               "name": "Description",
                                               "type": "String",
                                               "prompt": "Brief description of the item including notable details.",
                                               "defaultValue": "Black iPhone 14, cracked screen protector",
                                               "exampleValues": [
                                                 "Black iPhone 14, cracked screen protector",
                                                 "Brown leather wallet containing ID and credit cards",
                                                 "Silver car keys with blue keychain",
                                                 "Red leather-bound notebook, half-filled with notes"
                                               ]
                                             },
                                             {
                                               "name": "Quantity",
                                               "type": "String",
                                               "prompt": "Number of items.",
                                               "defaultValue": "1",
                                               "exampleValues": ["1", "3", "5", "10"]
                                             },
                                             {
                                               "name": "Location",
                                               "type": "String",
                                               "prompt": "Where the item is currently located.",
                                               "defaultValue": "Right pocket",
                                               "exampleValues": ["Right pocket", "Backpack", "Left hand", "Purse", "Briefcase"]
                                             }
                                           ]
                                         }
                                       ],
                                       "character": [
                                         {
                                           "name": "Gender",
                                           "type": "String",
                                           "prompt": "A single word and an emoji for character gender.",
                                           "defaultValue": "Male",
                                           "exampleValues": ["Male", "Female"]
                                         },
                                         {
                                           "name": "Age",
                                           "type": "String",
                                           "prompt": "A single number displays character age based on narrative. Or 'Unknown' if unknown.",
                                           "defaultValue": "28",
                                           "exampleValues": ["Unknown", "18", "32"]
                                         },
                                         {
                                           "name": "Hair",
                                           "type": "String",
                                           "prompt": "Describe style only.",
                                           "defaultValue": "Short black hair, neatly combed",
                                           "exampleValues": [
                                             "Shoulder-length blonde hair, styled straight",
                                             "Short black hair, neatly combed",
                                             "Long curly brown hair, pulled back into a low bun"
                                           ]
                                         },
                                         {
                                           "name": "Makeup",
                                           "type": "String",
                                           "prompt": "Describe current makeup.",
                                           "defaultValue": "None",
                                           "exampleValues": [
                                             "Natural look with light foundation and mascara",
                                             "None",
                                             "Subtle eyeliner and nude lipstick"
                                           ]
                                         },
                                         {
                                           "name": "Outfit",
                                           "type": "String",
                                           "prompt": "List the complete outfit with color, fabric, and style details.",
                                           "defaultValue": "Dark gray suit; Light blue dress shirt; Navy tie with silver stripes; Black leather belt; Black dress shoes; Black socks",
                                           "exampleValues": [
                                             "Navy blue blazer over a white silk blouse; Gray pencil skirt; Black leather belt; Sheer black stockings; Black leather pumps; Pearl necklace; Silver wristwatch",
                                             "Dark gray suit; Light blue dress shirt; Navy tie with silver stripes; Black leather belt; Black dress shoes; Black socks",
                                             "Cream-colored blouse with ruffled collar; Black slacks; Brown leather belt; Brown ankle boots; Gold hoop earrings"
                                           ]
                                         },
                                         {
                                           "name": "StateOfDress",
                                           "type": "String",
                                           "prompt": "Describe how put-together or disheveled the character appears.",
                                           "defaultValue": "Professionally dressed, attentive",
                                           "exampleValues": [
                                             "Professionally dressed, neat appearance",
                                             "Workout attire, lightly perspiring",
                                             "Casual attire, relaxed"
                                           ]
                                         },
                                         {
                                           "name": "PostureAndInteraction",
                                           "type": "String",
                                           "prompt": "Describe physical posture, position relative to others or objects, and interactions.",
                                           "defaultValue": "Sitting at the conference table, taking notes on a laptop",
                                           "exampleValues": [
                                             "Standing at the podium, presenting slides, holding a laser pointer",
                                             "Sitting at the conference table, taking notes on a laptop",
                                             "Lifting weights at the bench press, focused on form"
                                           ]
                                         },
                                         {
                                           "name": "Traits",
                                           "type": "String",
                                           "prompt": "Add or Remove trait based on Narrative. Format: '{trait}: {short description}'",
                                           "defaultValue": "No Traits",
                                           "exampleValues": [
                                             "No Traits",
                                             "Emotional Intelligence: deeply philosophical and sentimental",
                                             "Charismatic: naturally draws people in with charm and wit"
                                           ]
                                         },
                                         {
                                           "name": "Children",
                                           "type": "String",
                                           "prompt": "Add child after birth based on Narrative. Format: '{Birth Order}: {Name}, {Gender + Symbol}, child with {Other Parent}'",
                                           "defaultValue": "No Child",
                                           "exampleValues": [
                                             "No Child",
                                             "1st Born: Eve, Female, child with Harry"
                                           ]
                                         }
                                       ]
                                     }
                                     """;

    [Fact]
    public void Story_ShouldHaveCompleteStructure()
    {
        // Act
        var structure = JsonSerializer.Deserialize<TrackerStructure>(InputJson, JsonOptions);

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
        
        Assert.All(structure.Story, field =>
        {
            Assert.True(field.IsValid());
            Assert.Equal(FieldType.String, field.Type);
            Assert.NotNull(field.Name);
            Assert.NotNull(field.Prompt);
            Assert.NotNull(field.DefaultValue);
            Assert.NotNull(field.ExampleValues);
            Assert.Equal(3, field.ExampleValues.Count);
            Assert.False(field.HasNestedFields);
        });
        
        var timeField = structure.Story.First(f => f.Name == "Time");
        Assert.Equal("09:15:30; 10/16/2024 (Wednesday)", timeField.DefaultValue?.ToString());
        Assert.Contains("Adjust time in small increments", timeField.Prompt);
        
        var locationField = structure.Story.First(f => f.Name == "Location");
        Assert.Contains("Conference Room B", locationField.DefaultValue?.ToString());
        Assert.StartsWith("Provide a detailed and specific location", locationField.Prompt);
    }

    [Fact]
    public void CharactersPresent_ShouldHaveCompleteStructure()
    {
        // Act
        var structure = JsonSerializer.Deserialize<TrackerStructure>(InputJson, JsonOptions);

        // Assert
        Assert.NotNull(structure);
        Assert.NotNull(structure.CharactersPresent);
        Assert.Equal(2, structure.CharactersPresent.Length);
        Assert.Contains("Emma Thompson", structure.CharactersPresent);
        Assert.Contains("James Miller", structure.CharactersPresent);
        Assert.All(structure.CharactersPresent, name => Assert.False(string.IsNullOrWhiteSpace(name)));
    }

    [Fact]
    public void MainCharacterStats_ShouldHaveCompleteStructure()
    {
        // Act
        var structure = JsonSerializer.Deserialize<TrackerStructure>(InputJson, JsonOptions);

        // Assert
        Assert.NotNull(structure);
        Assert.NotNull(structure.MainCharacterStats);
        Assert.Equal(10, structure.MainCharacterStats.Length);
        
        var expectedFields = new[] { "Gender", "Age", "Hair", "Makeup", "Outfit", "StateOfDress", 
            "PostureAndInteraction", "Traits", "Children", "Inventory" };
        var actualFieldNames = structure.MainCharacterStats.Select(f => f.Name).ToArray();
        
        foreach (var expectedField in expectedFields)
        {
            Assert.Contains(expectedField, actualFieldNames);
        }
        
        Assert.All(structure.MainCharacterStats, field => Assert.True(field.IsValid()));
        
        var stringFields = structure.MainCharacterStats.Where(f => f.Name != "Inventory").ToArray();
        Assert.Equal(9, stringFields.Length);
        Assert.All(stringFields, field =>
        {
            Assert.Equal(FieldType.String, field.Type);
            Assert.NotNull(field.DefaultValue);
            Assert.NotNull(field.ExampleValues);
            Assert.False(field.HasNestedFields);
        });
        
        var inventoryField = structure.MainCharacterStats.First(f => f.Name == "Inventory");
        Assert.Equal(FieldType.ForEachObject, inventoryField.Type);
        Assert.Null(inventoryField.DefaultValue);
        Assert.Null(inventoryField.ExampleValues);
        Assert.True(inventoryField.HasNestedFields);
        Assert.NotNull(inventoryField.NestedFields);
        Assert.Equal(4, inventoryField.NestedFields.Length);
        
        var nestedFieldNames = inventoryField.NestedFields.Select(f => f.Name).ToArray();
        Assert.Contains("ItemName", nestedFieldNames);
        Assert.Contains("Description", nestedFieldNames);
        Assert.Contains("Quantity", nestedFieldNames);
        Assert.Contains("Location", nestedFieldNames);
        
        Assert.All(inventoryField.NestedFields, field =>
        {
            Assert.True(field.IsValid());
            Assert.Equal(FieldType.String, field.Type);
            Assert.NotNull(field.DefaultValue);
            Assert.NotNull(field.ExampleValues);
        });
        
        var itemNameField = inventoryField.NestedFields.First(f => f.Name == "ItemName");
        Assert.Equal("Smartphone", itemNameField.DefaultValue?.ToString());
        Assert.Equal(4, itemNameField.ExampleValues?.Count);
        
        var locationNestedField = inventoryField.NestedFields.First(f => f.Name == "Location");
        Assert.Equal("Right pocket", locationNestedField.DefaultValue?.ToString());
        
        var genderField = structure.MainCharacterStats.First(f => f.Name == "Gender");
        Assert.Equal("Female", genderField.DefaultValue?.ToString());
        Assert.Equal(2, genderField.ExampleValues?.Count);
        
        var outfitField = structure.MainCharacterStats.First(f => f.Name == "Outfit");
        var defaultOutfit = outfitField.DefaultValue?.ToString();
        Assert.Contains("Navy blue blazer", defaultOutfit);
        Assert.Contains("Black leather pumps", defaultOutfit);
    }

    [Fact]
    public void Character_ShouldHaveCompleteStructure()
    {
        // Act
        var structure = JsonSerializer.Deserialize<TrackerStructure>(InputJson, JsonOptions);

        // Assert
        Assert.NotNull(structure);
        Assert.NotNull(structure.Character);
        Assert.Equal(9, structure.Character.Length);
        
        var expectedFields = new[] { "Gender", "Age", "Hair", "Makeup", "Outfit", "StateOfDress", 
            "PostureAndInteraction", "Traits", "Children" };
        var actualFieldNames = structure.Character.Select(f => f.Name).ToArray();
        
        foreach (var expectedField in expectedFields)
        {
            Assert.Contains(expectedField, actualFieldNames);
        }
        
        Assert.DoesNotContain("Inventory", actualFieldNames);
        
        Assert.All(structure.Character, field =>
        {
            Assert.True(field.IsValid());
            Assert.Equal(FieldType.String, field.Type);
            Assert.NotNull(field.Name);
            Assert.NotNull(field.Prompt);
            Assert.NotNull(field.DefaultValue);
            Assert.NotNull(field.ExampleValues);
            Assert.False(field.HasNestedFields);
        });
        
        var genderField = structure.Character.First(f => f.Name == "Gender");
        Assert.Equal("Male", genderField.DefaultValue?.ToString());
        Assert.NotNull(genderField.ExampleValues);
        Assert.Equal(2, genderField.ExampleValues.Count);
        
        var ageField = structure.Character.First(f => f.Name == "Age");
        Assert.Equal("28", ageField.DefaultValue?.ToString());
        
        var hairField = structure.Character.First(f => f.Name == "Hair");
        Assert.Equal("Short black hair, neatly combed", hairField.DefaultValue?.ToString());
        
        var makeupField = structure.Character.First(f => f.Name == "Makeup");
        Assert.Equal("None", makeupField.DefaultValue?.ToString());
        
        var outfitField = structure.Character.First(f => f.Name == "Outfit");
        var defaultOutfit = outfitField.DefaultValue?.ToString();
        Assert.Contains("Dark gray suit", defaultOutfit);
        Assert.Contains("Black dress shoes", defaultOutfit);
    }
}