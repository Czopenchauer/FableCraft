using System.Text.Json;

using FableCraft.Application.NarrativeEngine;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;

namespace FableCraft.Tests.Tracker;

public class TrackerMergerTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    #region Scalar Field Tests

    [Test]
    public async Task Merge_ScalarField_ReplacesValue()
    {
        // Arrange
        var previous = new Dictionary<string, object>
        {
            ["Location"] = "Old Location",
            ["Health"] = "Healthy"
        };

        var updates = JsonDocument.Parse("""
        {
            "Location": "New Location"
        }
        """).RootElement;

        // Act
        var result = TrackerMerger.Merge(previous, updates);

        // Assert
        await Assert.That(result["Location"].ToString()).IsEqualTo("New Location");
        await Assert.That(result["Health"].ToString()).IsEqualTo("Healthy");
    }

    [Test]
    public async Task Merge_MultipleScalarFields_ReplacesAllSpecified()
    {
        // Arrange
        var previous = new Dictionary<string, object>
        {
            ["Location"] = "Old Location",
            ["Health"] = "Healthy",
            ["Mental"] = "Calm"
        };

        var updates = JsonDocument.Parse("""
        {
            "Location": "New Location",
            "Mental": "Anxious"
        }
        """).RootElement;

        // Act
        var result = TrackerMerger.Merge(previous, updates);

        // Assert
        await Assert.That(result["Location"].ToString()).IsEqualTo("New Location");
        await Assert.That(result["Mental"].ToString()).IsEqualTo("Anxious");
        await Assert.That(result["Health"].ToString()).IsEqualTo("Healthy");
    }

    [Test]
    public async Task Merge_NumericScalarField_ReplacesValue()
    {
        // Arrange
        var previous = new Dictionary<string, object>
        {
            ["Mana"] = 50,
            ["Health"] = 100
        };

        var updates = JsonDocument.Parse("""
        {
            "Mana": 30
        }
        """).RootElement;

        // Act
        var result = TrackerMerger.Merge(previous, updates);

        // Assert
        await Assert.That(GetLongValue(result["Mana"])).IsEqualTo(30);
        await Assert.That(GetLongValue(result["Health"])).IsEqualTo(100);
    }

    private static long GetLongValue(object value)
    {
        if (value is long l) return l;
        if (value is int i) return i;
        if (value is JsonElement je && je.ValueKind == JsonValueKind.Number) return je.GetInt64();
        return Convert.ToInt64(value);
    }

    #endregion

    #region Nested Object Tests

    [Test]
    public async Task Merge_NestedObject_MergesAtSubFieldLevel()
    {
        // Arrange
        var previous = new Dictionary<string, object>
        {
            ["Body"] = new Dictionary<string, object>
            {
                ["Head"] = "Original head description",
                ["Torso"] = "Original torso description",
                ["Arms"] = "Original arms description"
            }
        };

        var updates = JsonDocument.Parse("""
        {
            "Body": {
                "Head": "Updated head description"
            }
        }
        """).RootElement;

        // Act
        var result = TrackerMerger.Merge(previous, updates);

        // Assert
        var body = result["Body"] as Dictionary<string, object>;
        await Assert.That(body).IsNotNull();
        await Assert.That(body!["Head"].ToString()).IsEqualTo("Updated head description");
        await Assert.That(body["Torso"].ToString()).IsEqualTo("Original torso description");
        await Assert.That(body["Arms"].ToString()).IsEqualTo("Original arms description");
    }

    [Test]
    public async Task Merge_NestedObject_MultipleSubFields()
    {
        // Arrange
        var previous = new Dictionary<string, object>
        {
            ["Body"] = new Dictionary<string, object>
            {
                ["Head"] = "Original head",
                ["Torso"] = "Original torso",
                ["Arms"] = "Original arms"
            }
        };

        var updates = JsonDocument.Parse("""
        {
            "Body": {
                "Head": "New head",
                "Arms": "New arms"
            }
        }
        """).RootElement;

        // Act
        var result = TrackerMerger.Merge(previous, updates);

        // Assert
        var body = result["Body"] as Dictionary<string, object>;
        await Assert.That(body!["Head"].ToString()).IsEqualTo("New head");
        await Assert.That(body["Arms"].ToString()).IsEqualTo("New arms");
        await Assert.That(body["Torso"].ToString()).IsEqualTo("Original torso");
    }

    [Test]
    public async Task Merge_DeepNestedObject_MergesRecursively()
    {
        // Arrange
        var previous = new Dictionary<string, object>
        {
            ["Progress"] = new Dictionary<string, object>
            {
                ["Status"] = "Not started",
                ["Details"] = new Dictionary<string, object>
                {
                    ["Stage"] = "N/A",
                    ["Deadline"] = "N/A"
                }
            }
        };

        var updates = JsonDocument.Parse("""
        {
            "Progress": {
                "Status": "In progress",
                "Details": {
                    "Stage": "First"
                }
            }
        }
        """).RootElement;

        // Act
        var result = TrackerMerger.Merge(previous, updates);

        // Assert
        var progress = result["Progress"] as Dictionary<string, object>;
        await Assert.That(progress!["Status"].ToString()).IsEqualTo("In progress");

        var details = progress["Details"] as Dictionary<string, object>;
        await Assert.That(details!["Stage"].ToString()).IsEqualTo("First");
        await Assert.That(details["Deadline"].ToString()).IsEqualTo("N/A");
    }

    [Test]
    public async Task Merge_NestedObject_CreatesNewIfNotExists()
    {
        // Arrange
        var previous = new Dictionary<string, object>
        {
            ["Location"] = "Some place"
        };

        var updates = JsonDocument.Parse("""
        {
            "Body": {
                "Head": "New head description"
            }
        }
        """).RootElement;

        // Act
        var result = TrackerMerger.Merge(previous, updates);

        // Assert
        var body = result["Body"] as Dictionary<string, object>;
        await Assert.That(body).IsNotNull();
        await Assert.That(body!["Head"].ToString()).IsEqualTo("New head description");
    }

    #endregion

    #region Simple Array Tests

    [Test]
    public async Task Merge_SimpleArray_FullReplacement()
    {
        // Arrange
        var previous = new Dictionary<string, object>
        {
            ["ActiveEffects"] = new List<object> { "Effect1", "Effect2", "Effect3" }
        };

        var updates = JsonDocument.Parse("""
        {
            "ActiveEffects": ["NewEffect1", "NewEffect2"]
        }
        """).RootElement;

        // Act
        var result = TrackerMerger.Merge(previous, updates);

        // Assert
        var effects = result["ActiveEffects"] as List<object>;
        await Assert.That(effects).IsNotNull();
        await Assert.That(effects!.Count).IsEqualTo(2);
        await Assert.That(effects[0].ToString()).IsEqualTo("NewEffect1");
        await Assert.That(effects[1].ToString()).IsEqualTo("NewEffect2");
    }

    [Test]
    public async Task Merge_SimpleArray_EmptyArrayReplacement()
    {
        // Arrange
        var previous = new Dictionary<string, object>
        {
            ["Weapons"] = new List<object> { "Sword", "Dagger" }
        };

        var updates = JsonDocument.Parse("""
        {
            "Weapons": []
        }
        """).RootElement;

        // Act
        var result = TrackerMerger.Merge(previous, updates);

        // Assert
        var weapons = result["Weapons"] as List<object>;
        await Assert.That(weapons).IsNotNull();
        await Assert.That(weapons!.Count).IsEqualTo(0);
    }

    #endregion

    #region Complex Array - $modify Tests

    [Test]
    public async Task Merge_ComplexArray_ModifyBySkillName()
    {
        // Arrange
        var previous = new Dictionary<string, object>
        {
            ["Skills"] = new List<object>
            {
                new Dictionary<string, object>
                {
                    ["SkillName"] = "Swordsmanship",
                    ["Rank"] = "Novice",
                    ["XP"] = "25/50"
                },
                new Dictionary<string, object>
                {
                    ["SkillName"] = "Magic",
                    ["Rank"] = "Apprentice",
                    ["XP"] = "10/100"
                }
            }
        };

        var updates = JsonDocument.Parse("""
        {
            "Skills": {
                "$modify": [
                    {
                        "$match": "Swordsmanship",
                        "$set": {
                            "XP": "45/50"
                        }
                    }
                ]
            }
        }
        """).RootElement;

        // Act
        var result = TrackerMerger.Merge(previous, updates);

        // Assert
        var skills = result["Skills"] as List<object>;
        await Assert.That(skills).IsNotNull();
        await Assert.That(skills!.Count).IsEqualTo(2);

        var sword = skills[0] as Dictionary<string, object>;
        await Assert.That(sword!["SkillName"].ToString()).IsEqualTo("Swordsmanship");
        await Assert.That(sword["XP"].ToString()).IsEqualTo("45/50");
        await Assert.That(sword["Rank"].ToString()).IsEqualTo("Novice");

        var magic = skills[1] as Dictionary<string, object>;
        await Assert.That(magic!["XP"].ToString()).IsEqualTo("10/100");
    }

    [Test]
    public async Task Merge_ComplexArray_ModifyByAbilityName()
    {
        // Arrange
        var previous = new Dictionary<string, object>
        {
            ["Abilities"] = new List<object>
            {
                new Dictionary<string, object>
                {
                    ["AbilityName"] = "Fireball",
                    ["ManaCost"] = "20",
                    ["Description"] = "Launches a ball of fire"
                }
            }
        };

        var updates = JsonDocument.Parse("""
        {
            "Abilities": {
                "$modify": [
                    {
                        "$match": "Fireball",
                        "$set": {
                            "ManaCost": "15",
                            "Description": "Improved fireball with reduced cost"
                        }
                    }
                ]
            }
        }
        """).RootElement;

        // Act
        var result = TrackerMerger.Merge(previous, updates);

        // Assert
        var abilities = result["Abilities"] as List<object>;
        var fireball = abilities![0] as Dictionary<string, object>;
        await Assert.That(fireball!["ManaCost"].ToString()).IsEqualTo("15");
        await Assert.That(fireball["Description"].ToString()).IsEqualTo("Improved fireball with reduced cost");
        await Assert.That(fireball["AbilityName"].ToString()).IsEqualTo("Fireball");
    }

    [Test]
    public async Task Merge_ComplexArray_ModifyMultipleEntries()
    {
        // Arrange
        var previous = new Dictionary<string, object>
        {
            ["Skills"] = new List<object>
            {
                new Dictionary<string, object> { ["SkillName"] = "Skill1", ["Level"] = "1" },
                new Dictionary<string, object> { ["SkillName"] = "Skill2", ["Level"] = "2" },
                new Dictionary<string, object> { ["SkillName"] = "Skill3", ["Level"] = "3" }
            }
        };

        var updates = JsonDocument.Parse("""
        {
            "Skills": {
                "$modify": [
                    { "$match": "Skill1", "$set": { "Level": "5" } },
                    { "$match": "Skill3", "$set": { "Level": "10" } }
                ]
            }
        }
        """).RootElement;

        // Act
        var result = TrackerMerger.Merge(previous, updates);

        // Assert
        var skills = result["Skills"] as List<object>;
        var skill1 = skills![0] as Dictionary<string, object>;
        var skill2 = skills[1] as Dictionary<string, object>;
        var skill3 = skills[2] as Dictionary<string, object>;

        await Assert.That(skill1!["Level"].ToString()).IsEqualTo("5");
        await Assert.That(skill2!["Level"].ToString()).IsEqualTo("2");
        await Assert.That(skill3!["Level"].ToString()).IsEqualTo("10");
    }

    [Test]
    public async Task Merge_ComplexArray_ModifyNonExistentEntry_NoChange()
    {
        // Arrange
        var previous = new Dictionary<string, object>
        {
            ["Skills"] = new List<object>
            {
                new Dictionary<string, object> { ["SkillName"] = "Existing", ["Level"] = "1" }
            }
        };

        var updates = JsonDocument.Parse("""
        {
            "Skills": {
                "$modify": [
                    { "$match": "NonExistent", "$set": { "Level": "99" } }
                ]
            }
        }
        """).RootElement;

        // Act
        var result = TrackerMerger.Merge(previous, updates);

        // Assert
        var skills = result["Skills"] as List<object>;
        await Assert.That(skills!.Count).IsEqualTo(1);
        var skill = skills[0] as Dictionary<string, object>;
        await Assert.That(skill!["Level"].ToString()).IsEqualTo("1");
    }

    #endregion

    #region Complex Array - $add Tests

    [Test]
    public async Task Merge_ComplexArray_AddNewEntry()
    {
        // Arrange
        var previous = new Dictionary<string, object>
        {
            ["Skills"] = new List<object>
            {
                new Dictionary<string, object> { ["SkillName"] = "Existing", ["Level"] = "1" }
            }
        };

        var updates = JsonDocument.Parse("""
        {
            "Skills": {
                "$add": [
                    { "SkillName": "NewSkill", "Level": "0", "Description": "Just learned" }
                ]
            }
        }
        """).RootElement;

        // Act
        var result = TrackerMerger.Merge(previous, updates);

        // Assert
        var skills = result["Skills"] as List<object>;
        await Assert.That(skills!.Count).IsEqualTo(2);

        var newSkill = skills[1] as Dictionary<string, object>;
        await Assert.That(newSkill!["SkillName"].ToString()).IsEqualTo("NewSkill");
        await Assert.That(newSkill["Level"].ToString()).IsEqualTo("0");
        await Assert.That(newSkill["Description"].ToString()).IsEqualTo("Just learned");
    }

    [Test]
    public async Task Merge_ComplexArray_AddMultipleEntries()
    {
        // Arrange
        var previous = new Dictionary<string, object>
        {
            ["Abilities"] = new List<object>()
        };

        var updates = JsonDocument.Parse("""
        {
            "Abilities": {
                "$add": [
                    { "AbilityName": "Ability1", "ManaCost": "10" },
                    { "AbilityName": "Ability2", "ManaCost": "20" }
                ]
            }
        }
        """).RootElement;

        // Act
        var result = TrackerMerger.Merge(previous, updates);

        // Assert
        var abilities = result["Abilities"] as List<object>;
        await Assert.That(abilities!.Count).IsEqualTo(2);
    }

    [Test]
    public async Task Merge_ComplexArray_AddToEmptyArray()
    {
        // Arrange
        var previous = new Dictionary<string, object>
        {
            ["Parasites"] = new List<object>()
        };

        var updates = JsonDocument.Parse("""
        {
            "Parasites": {
                "$add": [
                    { "Type": "Symbiotic Worm", "Effects": "Enhanced regeneration" }
                ]
            }
        }
        """).RootElement;

        // Act
        var result = TrackerMerger.Merge(previous, updates);

        // Assert
        var parasites = result["Parasites"] as List<object>;
        await Assert.That(parasites!.Count).IsEqualTo(1);
    }

    #endregion

    #region Complex Array - $remove Tests

    [Test]
    public async Task Merge_ComplexArray_RemoveEntry()
    {
        // Arrange
        var previous = new Dictionary<string, object>
        {
            ["Skills"] = new List<object>
            {
                new Dictionary<string, object> { ["SkillName"] = "ToRemove", ["Level"] = "1" },
                new Dictionary<string, object> { ["SkillName"] = "ToKeep", ["Level"] = "2" }
            }
        };

        var updates = JsonDocument.Parse("""
        {
            "Skills": {
                "$remove": ["ToRemove"]
            }
        }
        """).RootElement;

        // Act
        var result = TrackerMerger.Merge(previous, updates);

        // Assert
        var skills = result["Skills"] as List<object>;
        await Assert.That(skills!.Count).IsEqualTo(1);
        var remaining = skills[0] as Dictionary<string, object>;
        await Assert.That(remaining!["SkillName"].ToString()).IsEqualTo("ToKeep");
    }

    [Test]
    public async Task Merge_ComplexArray_RemoveMultipleEntries()
    {
        // Arrange
        var previous = new Dictionary<string, object>
        {
            ["Abilities"] = new List<object>
            {
                new Dictionary<string, object> { ["AbilityName"] = "A" },
                new Dictionary<string, object> { ["AbilityName"] = "B" },
                new Dictionary<string, object> { ["AbilityName"] = "C" },
                new Dictionary<string, object> { ["AbilityName"] = "D" }
            }
        };

        var updates = JsonDocument.Parse("""
        {
            "Abilities": {
                "$remove": ["A", "C"]
            }
        }
        """).RootElement;

        // Act
        var result = TrackerMerger.Merge(previous, updates);

        // Assert
        var abilities = result["Abilities"] as List<object>;
        await Assert.That(abilities!.Count).IsEqualTo(2);
        var names = abilities.Select(a => ((Dictionary<string, object>)a)["AbilityName"].ToString()).ToList();
        await Assert.That(names).Contains("B");
        await Assert.That(names).Contains("D");
        await Assert.That(names).DoesNotContain("A");
        await Assert.That(names).DoesNotContain("C");
    }

    [Test]
    public async Task Merge_ComplexArray_RemoveNonExistent_NoError()
    {
        // Arrange
        var previous = new Dictionary<string, object>
        {
            ["Skills"] = new List<object>
            {
                new Dictionary<string, object> { ["SkillName"] = "Existing", ["Level"] = "1" }
            }
        };

        var updates = JsonDocument.Parse("""
        {
            "Skills": {
                "$remove": ["NonExistent"]
            }
        }
        """).RootElement;

        // Act
        var result = TrackerMerger.Merge(previous, updates);

        // Assert
        var skills = result["Skills"] as List<object>;
        await Assert.That(skills!.Count).IsEqualTo(1);
    }

    #endregion

    #region Complex Array - Combined Operations Tests

    [Test]
    public async Task Merge_ComplexArray_CombinedModifyAddRemove()
    {
        // Arrange
        var previous = new Dictionary<string, object>
        {
            ["Skills"] = new List<object>
            {
                new Dictionary<string, object> { ["SkillName"] = "ToModify", ["Level"] = "1" },
                new Dictionary<string, object> { ["SkillName"] = "ToRemove", ["Level"] = "2" },
                new Dictionary<string, object> { ["SkillName"] = "Untouched", ["Level"] = "3" }
            }
        };

        var updates = JsonDocument.Parse("""
        {
            "Skills": {
                "$modify": [
                    { "$match": "ToModify", "$set": { "Level": "10" } }
                ],
                "$add": [
                    { "SkillName": "NewSkill", "Level": "0" }
                ],
                "$remove": ["ToRemove"]
            }
        }
        """).RootElement;

        // Act
        var result = TrackerMerger.Merge(previous, updates);

        // Assert
        var skills = result["Skills"] as List<object>;
        await Assert.That(skills!.Count).IsEqualTo(3);

        var names = skills.Select(s => ((Dictionary<string, object>)s)["SkillName"].ToString()).ToList();
        await Assert.That(names).Contains("ToModify");
        await Assert.That(names).Contains("Untouched");
        await Assert.That(names).Contains("NewSkill");
        await Assert.That(names).DoesNotContain("ToRemove");

        var modified = skills.First(s => ((Dictionary<string, object>)s)["SkillName"].ToString() == "ToModify");
        await Assert.That(((Dictionary<string, object>)modified)["Level"].ToString()).IsEqualTo("10");
    }

    #endregion

    #region Custom Field Name Detection Tests

    [Test]
    public async Task Merge_ComplexArray_MatchByTypeField()
    {
        // Arrange
        var previous = new Dictionary<string, object>
        {
            ["Parasites"] = new List<object>
            {
                new Dictionary<string, object>
                {
                    ["Type"] = "Duct Worms",
                    ["Effects"] = "Minor effects"
                }
            }
        };

        var updates = JsonDocument.Parse("""
        {
            "Parasites": {
                "$modify": [
                    { "$match": "Duct Worms", "$set": { "Effects": "Enhanced effects" } }
                ]
            }
        }
        """).RootElement;

        // Act
        var result = TrackerMerger.Merge(previous, updates);

        // Assert
        var parasites = result["Parasites"] as List<object>;
        var parasite = parasites![0] as Dictionary<string, object>;
        await Assert.That(parasite!["Effects"].ToString()).IsEqualTo("Enhanced effects");
    }

    [Test]
    public async Task Merge_ComplexArray_MatchByItemField()
    {
        // Arrange
        var previous = new Dictionary<string, object>
        {
            ["CursedItems"] = new List<object>
            {
                new Dictionary<string, object>
                {
                    ["Item"] = "Ring of Binding",
                    ["Curse"] = "Cannot be removed"
                }
            }
        };

        var updates = JsonDocument.Parse("""
        {
            "CursedItems": {
                "$modify": [
                    { "$match": "Ring of Binding", "$set": { "Curse": "Curse weakened" } }
                ]
            }
        }
        """).RootElement;

        // Act
        var result = TrackerMerger.Merge(previous, updates);

        // Assert
        var items = result["CursedItems"] as List<object>;
        var ring = items![0] as Dictionary<string, object>;
        await Assert.That(ring!["Curse"].ToString()).IsEqualTo("Curse weakened");
    }

    [Test]
    public async Task Merge_ComplexArray_MatchByCustomNameField()
    {
        // Arrange
        var previous = new Dictionary<string, object>
        {
            ["CustomArray"] = new List<object>
            {
                new Dictionary<string, object>
                {
                    ["CustomName"] = "ItemOne",
                    ["Value"] = "100"
                }
            }
        };

        var updates = JsonDocument.Parse("""
        {
            "CustomArray": {
                "$modify": [
                    { "$match": "ItemOne", "$set": { "Value": "200" } }
                ]
            }
        }
        """).RootElement;

        // Act
        var result = TrackerMerger.Merge(previous, updates);

        // Assert
        var items = result["CustomArray"] as List<object>;
        var item = items![0] as Dictionary<string, object>;
        await Assert.That(item!["Value"].ToString()).IsEqualTo("200");
    }

    #endregion

    #region MainCharacterTracker Integration Tests

    [Test]
    public async Task Merge_MainCharacterTracker_UpdatesTypedProperties()
    {
        // Arrange
        var previous = new MainCharacterTracker
        {
            Name = "Original Name",
            Appearance = "Original appearance",
            GeneralBuild = "Athletic",
            AdditionalProperties = new Dictionary<string, object>
            {
                ["Location"] = "Old location"
            }
        };

        var updates = JsonDocument.Parse("""
        {
            "Name": "New Name",
            "Appearance": "Updated appearance"
        }
        """).RootElement;

        // Act
        var result = TrackerMerger.Merge(previous, updates);

        // Assert
        await Assert.That(result.Name).IsEqualTo("New Name");
        await Assert.That(result.Appearance).IsEqualTo("Updated appearance");
        await Assert.That(result.GeneralBuild).IsEqualTo("Athletic");
        await Assert.That(result.AdditionalProperties["Location"].ToString()).IsEqualTo("Old location");
    }

    [Test]
    public async Task Merge_MainCharacterTracker_UpdatesAdditionalProperties()
    {
        // Arrange
        var previous = new MainCharacterTracker
        {
            Name = "Test",
            AdditionalProperties = new Dictionary<string, object>
            {
                ["Health"] = "100%",
                ["Mana"] = "50/100"
            }
        };

        var updates = JsonDocument.Parse("""
        {
            "Mana": "30/100",
            "NewProperty": "New Value"
        }
        """).RootElement;

        // Act
        var result = TrackerMerger.Merge(previous, updates);

        // Assert
        await Assert.That(result.AdditionalProperties["Mana"].ToString()).IsEqualTo("30/100");
        await Assert.That(result.AdditionalProperties["Health"].ToString()).IsEqualTo("100%");
        await Assert.That(result.AdditionalProperties["NewProperty"].ToString()).IsEqualTo("New Value");
    }

    [Test]
    public async Task Merge_MainCharacterTracker_FullScenario()
    {
        // Arrange - Simulating a real tracker state
        var previous = new MainCharacterTracker
        {
            Name = "Elena",
            Appearance = "Wearing casual clothes",
            GeneralBuild = "Slender",
            AdditionalProperties = new Dictionary<string, object>
            {
                ["Location"] = "Village Square",
                ["Health"] = "Healthy",
                ["Body"] = new Dictionary<string, object>
                {
                    ["Head"] = "Dark hair, green eyes",
                    ["Torso"] = "Slim build"
                },
                ["Skills"] = new List<object>
                {
                    new Dictionary<string, object>
                    {
                        ["SkillName"] = "Herbalism",
                        ["Rank"] = "Novice",
                        ["XP"] = "20/50"
                    },
                    new Dictionary<string, object>
                    {
                        ["SkillName"] = "Combat",
                        ["Rank"] = "Untrained",
                        ["XP"] = "0/50"
                    }
                },
                ["ActiveEffects"] = new List<object> { "Well-rested" }
            }
        };

        var updates = JsonDocument.Parse("""
        {
            "Location": "Forest Path",
            "Appearance": "Clothes torn from branches",
            "Body": {
                "Head": "Hair disheveled, scratches on face"
            },
            "Skills": {
                "$modify": [
                    { "$match": "Herbalism", "$set": { "XP": "35/50" } }
                ],
                "$add": [
                    { "SkillName": "Survival", "Rank": "Untrained", "XP": "5/50" }
                ]
            },
            "ActiveEffects": ["Well-rested", "Minor scratches"]
        }
        """).RootElement;

        // Act
        var result = TrackerMerger.Merge(previous, updates);

        // Assert - Typed properties
        await Assert.That(result.Name).IsEqualTo("Elena");
        await Assert.That(result.Appearance).IsEqualTo("Clothes torn from branches");
        await Assert.That(result.GeneralBuild).IsEqualTo("Slender");

        // Assert - Additional properties
        await Assert.That(result.AdditionalProperties["Location"].ToString()).IsEqualTo("Forest Path");
        await Assert.That(result.AdditionalProperties["Health"].ToString()).IsEqualTo("Healthy");

        // Assert - Nested object merge
        var body = result.AdditionalProperties["Body"] as Dictionary<string, object>;
        await Assert.That(body!["Head"].ToString()).IsEqualTo("Hair disheveled, scratches on face");
        await Assert.That(body["Torso"].ToString()).IsEqualTo("Slim build");

        // Assert - Complex array operations
        var skills = result.AdditionalProperties["Skills"] as List<object>;
        await Assert.That(skills!.Count).IsEqualTo(3);

        var herbalism = skills.First(s => ((Dictionary<string, object>)s)["SkillName"].ToString() == "Herbalism");
        await Assert.That(((Dictionary<string, object>)herbalism)["XP"].ToString()).IsEqualTo("35/50");

        var survival = skills.First(s => ((Dictionary<string, object>)s)["SkillName"].ToString() == "Survival");
        await Assert.That(((Dictionary<string, object>)survival)["XP"].ToString()).IsEqualTo("5/50");

        // Assert - Simple array replacement
        var effects = result.AdditionalProperties["ActiveEffects"] as List<object>;
        await Assert.That(effects!.Count).IsEqualTo(2);
        await Assert.That(effects[0].ToString()).IsEqualTo("Well-rested");
        await Assert.That(effects[1].ToString()).IsEqualTo("Minor scratches");
    }

    #endregion

    #region Edge Cases

    [Test]
    public async Task Merge_EmptyUpdates_ReturnsUnchangedState()
    {
        // Arrange
        var previous = new Dictionary<string, object>
        {
            ["Field1"] = "Value1",
            ["Field2"] = "Value2"
        };

        var updates = JsonDocument.Parse("{}").RootElement;

        // Act
        var result = TrackerMerger.Merge(previous, updates);

        // Assert
        await Assert.That(result["Field1"].ToString()).IsEqualTo("Value1");
        await Assert.That(result["Field2"].ToString()).IsEqualTo("Value2");
    }

    [Test]
    public async Task Merge_NullValueInUpdate_SetsNull()
    {
        // Arrange
        var previous = new Dictionary<string, object>
        {
            ["Field"] = "Value"
        };

        var updates = JsonDocument.Parse("""
        {
            "Field": null
        }
        """).RootElement;

        // Act
        var result = TrackerMerger.Merge(previous, updates);

        // Assert
        await Assert.That(result["Field"]).IsNull();
    }

    [Test]
    public async Task Merge_BooleanValues_HandlesCorrectly()
    {
        // Arrange
        var previous = new Dictionary<string, object>
        {
            ["IsActive"] = false,
            ["HasItem"] = true
        };

        var updates = JsonDocument.Parse("""
        {
            "IsActive": true
        }
        """).RootElement;

        // Act
        var result = TrackerMerger.Merge(previous, updates);

        // Assert
        await Assert.That(GetBoolValue(result["IsActive"])).IsTrue();
        await Assert.That(GetBoolValue(result["HasItem"])).IsTrue();
    }

    private static bool GetBoolValue(object value)
    {
        if (value is bool b) return b;
        if (value is JsonElement je) return je.GetBoolean();
        return Convert.ToBoolean(value);
    }

    [Test]
    public async Task Merge_PreservesPreviousState_DoesNotModifyOriginal()
    {
        // Arrange
        var previous = new Dictionary<string, object>
        {
            ["Field"] = "Original"
        };

        var updates = JsonDocument.Parse("""
        {
            "Field": "Modified"
        }
        """).RootElement;

        // Act
        var result = TrackerMerger.Merge(previous, updates);

        // Assert
        await Assert.That(previous["Field"].ToString()).IsEqualTo("Original");
        await Assert.That(result["Field"].ToString()).IsEqualTo("Modified");
    }

    [Test]
    public async Task Merge_CaseInsensitiveMatch_FindsEntry()
    {
        // Arrange
        var previous = new Dictionary<string, object>
        {
            ["Skills"] = new List<object>
            {
                new Dictionary<string, object> { ["SkillName"] = "Swordsmanship", ["Level"] = "1" }
            }
        };

        var updates = JsonDocument.Parse("""
        {
            "Skills": {
                "$modify": [
                    { "$match": "swordsmanship", "$set": { "Level": "5" } }
                ]
            }
        }
        """).RootElement;

        // Act
        var result = TrackerMerger.Merge(previous, updates);

        // Assert
        var skills = result["Skills"] as List<object>;
        var skill = skills![0] as Dictionary<string, object>;
        await Assert.That(skill!["Level"].ToString()).IsEqualTo("5");
    }

    #endregion
}
