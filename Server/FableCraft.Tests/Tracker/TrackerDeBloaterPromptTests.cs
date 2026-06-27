using System.Text.Json;

using FableCraft.Application.NarrativeEngine;
using FableCraft.Application.NarrativeEngine.Agents.Builders;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;

namespace FableCraft.Tests.Tracker;

public class TrackerDeBloaterPromptTests
{
    private static readonly JsonSerializerOptions JsonOptions = PromptSections.GetJsonOptions();

    [Test]
    public async Task BuildInstruction_ShouldIncludeFullMainCharacterTrackerDefinition_NotJustName()
    {
        // Arrange
        var trackerJson = """
                          {
                            "Story": [
                              {
                                "Name": "Time",
                                "Type": "String",
                                "Prompt": "Time.",
                                "DefaultValue": "12:00",
                                "ExampleValues": ["12:00"]
                              }
                            ],
                            "MainCharacter": [
                              {
                                "Name": "Name",
                                "Type": "String",
                                "Prompt": "Character full name.",
                                "DefaultValue": "Ariel",
                                "ExampleValues": ["Ariel", "Kael", "Valerius"]
                              },
                              {
                                "Name": "Appearance",
                                "Type": "String",
                                "Prompt": "Current visual state.",
                                "DefaultValue": "Unremarkable",
                                "ExampleValues": ["Clean", "Dirty"]
                              }
                            ],
                            "Characters": [
                              {
                                "Name": "Name",
                                "Type": "String",
                                "Prompt": "NPC name.",
                                "DefaultValue": "Unknown",
                                "ExampleValues": ["Bob"]
                              }
                            ]
                          }
                          """;

        var structure = JsonSerializer.Deserialize<TrackerStructure>(trackerJson, JsonOptions);
        var definition = TrackerExtensions.ConvertToSystemJson(structure!.MainCharacter);
        var serialized = JsonSerializer.Serialize(definition, JsonOptions);

        // Act + Assert
        await Assert.That(serialized).Contains("\"Name\"");
        await Assert.That(serialized).Contains("\"Appearance\"");
        await Assert.That(serialized).DoesNotContain("Characters");

        using var doc = JsonDocument.Parse(serialized);
        await Assert.That(doc.RootElement.EnumerateObject().Count()).IsEqualTo(2);
    }

    [Test]
    public async Task ConvertToSystemJson_ShouldReturnAllMainCharacterFields_WithPromptDefaultExample()
    {
        // Arrange
        var structure = JsonSerializer.Deserialize<TrackerStructure>(TestTracker.InputJson, JsonOptions)!;

        // Act
        var definition = TrackerExtensions.ConvertToSystemJson(structure.MainCharacter);
        var serialized = JsonSerializer.Serialize(definition, JsonOptions);

        // Assert
        await Assert.That(serialized).Contains("\"Name\"");
        await Assert.That(serialized).Contains("\"Gender\"");
        await Assert.That(serialized).Contains("\"Age\"");
        await Assert.That(serialized).Contains("\"Hair\"");
        await Assert.That(serialized).Contains("\"Makeup\"");
        await Assert.That(serialized).Contains("\"Outfit\"");
        await Assert.That(serialized).Contains("\"StateOfDress\"");
        await Assert.That(serialized).Contains("\"PostureAndInteraction\"");
        await Assert.That(serialized).Contains("\"Traits\"");
        await Assert.That(serialized).Contains("\"Children\"");
        await Assert.That(serialized).Contains("\"Inventory\"");

        using var doc = JsonDocument.Parse(serialized);
        await Assert.That(doc.RootElement.EnumerateObject().Count()).IsEqualTo(11);

        var nameField = doc.RootElement.GetProperty("Name");
        await Assert.That(nameField.GetProperty("Prompt").GetString()).IsEqualTo("Character's full name.");
        await Assert.That(nameField.GetProperty("DefaultValue").GetString()).IsEqualTo("James Miller");
    }

    [Test]
    public async Task MainCharacterTrackerAgent_BuildInstruction_ShouldSerializeFullDefinition()
    {
        // Arrange
        var structure = JsonSerializer.Deserialize<TrackerStructure>(TestTracker.InputJson, JsonOptions)!;
        var trackerPrompt = TrackerExtensions.ConvertToSystemJson(structure.MainCharacter);
        var serialized = JsonSerializer.Serialize(trackerPrompt, JsonOptions);

        // Assert
        await Assert.That(serialized).Contains("\"Name\"");
        await Assert.That(serialized).Contains("\"Inventory\"");

        using var doc = JsonDocument.Parse(serialized);
        var inventoryProp = doc.RootElement.GetProperty("Inventory");
        await Assert.That(inventoryProp.ValueKind).IsEqualTo(JsonValueKind.Array);

        var firstInventoryTemplate = inventoryProp[0];
        await Assert.That(firstInventoryTemplate.GetProperty("ItemName").GetProperty("Prompt").GetString()).IsEqualTo("Name of the item.");
    }

    [Test]
    public async Task TrackerDeBloaterPrompt_Placeholder_ShouldBeReplacedWithFullDefinitionJson()
    {
        // Arrange
        var trackerJson = """
                          {
                            "Story": [],
                            "MainCharacter": [
                              { "Name": "Name", "Type": "String", "Prompt": "P1", "DefaultValue": "A", "ExampleValues": ["A"] },
                              { "Name": "Age", "Type": "String", "Prompt": "P2", "DefaultValue": "20", "ExampleValues": ["20"] },
                              { "Name": "Race", "Type": "String", "Prompt": "P3", "DefaultValue": "Human", "ExampleValues": ["Human"] }
                            ],
                            "Characters": []
                          }
                          """;

        var structure = JsonSerializer.Deserialize<TrackerStructure>(trackerJson, JsonOptions)!;
        var definition = TrackerExtensions.ConvertToSystemJson(structure.MainCharacter);
        var definitionJson = JsonSerializer.Serialize(definition, JsonOptions);

        var promptTemplate = """
                             ## INPUT FORMAT

                             ### 1. Tracker Definition
                             {{tracker_definition}}
                             """;

        // Act
        var rendered = PromptBuilder.ReplacePlaceholders(
            promptTemplate,
            ("{{tracker_definition}}", definitionJson));

        // Assert
        await Assert.That(rendered).Contains("\"Name\"");
        await Assert.That(rendered).Contains("\"Age\"");
        await Assert.That(rendered).Contains("\"Race\"");
        await Assert.That(rendered).DoesNotContain("{{tracker_definition}}");
    }
}
