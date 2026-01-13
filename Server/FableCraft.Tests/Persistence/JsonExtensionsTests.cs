using System.Text.Json;
using System.Text.Json.Serialization;

using FableCraft.Infrastructure.Persistence;

namespace FableCraft.Tests.Persistence;

public class JsonExtensionsTests
{
    #region Array Value Tests

    [Test]
    public async Task PatchWith_ArrayValue_ReplacesArray()
    {
        // Arrange
        var original = CreateTestCharacter();
        var newPlan = new Routine
        {
            DailyPattern = "escape",
            Activities = ["disable alarm", "exit through back", "meet contact", "leave city"]
        };
        var updates = new Dictionary<string, object>
        {
            ["behavioral_defaults.routine"] = newPlan
        };

        // Act
        var result = original.PatchWith(updates);

        // Assert
        await Assert.That(result.BehavioralDefaults!.Routine!.Activities!.Count).IsEqualTo(4);
        await Assert.That(result.BehavioralDefaults.Routine.Activities).Contains("leave city");
    }

    #endregion

    #region ADR Char.md Example Tests

    [Test]
    public async Task PatchWith_CharacterReflectionAgentOutput_AppliesCorrectly()
    {
        // Arrange
        var original = CreateTestCharacter();

        // Simulates CharacterReflectionAgent output from ADR
        var updates = new Dictionary<string, object>
        {
            ["psychology.triggers"] = new Triggers
            {
                PrimaryTrigger = "anxious",
                SecondaryTriggers = ["calculating", "defensive"],
                Intensity = 0.7,
                Response = "protagonist getting too close"
            },
            ["motivations.goals_current"] = "gather intelligence before next confrontation",
            ["behavioral_defaults.routine"] = new Routine
            {
                DailyPattern = "assess damage",
                Activities = ["talk to dockmaster", "check records", "prepare alibi"]
            }
        };

        // Act
        var result = original.PatchWith(updates);

        // Assert - Volatile state updated as per ADR
        await Assert.That(result.Psychology!.Triggers!.PrimaryTrigger).IsEqualTo("anxious");
        await Assert.That(result.Psychology.Triggers.SecondaryTriggers).Contains("defensive");
        await Assert.That(result.Psychology.Triggers.Intensity).IsEqualTo(0.7);
        await Assert.That(result.Psychology.Triggers.Response).IsEqualTo("protagonist getting too close");

        await Assert.That(result.Motivations!.GoalsCurrent)
            .IsEqualTo("gather intelligence before next confrontation");

        await Assert.That(result.BehavioralDefaults!.Routine!.DailyPattern).IsEqualTo("assess damage");
        await Assert.That(result.BehavioralDefaults.Routine.Activities).Contains("prepare alibi");

        // Assert - Core profile (stable) unchanged as per ADR
        await Assert.That(result.CharacterIdentity.Name).IsEqualTo("Viktor Volkov");
        await Assert.That(result.CharacterIdentity.Role).IsEqualTo("Antagonist");

        // Assert - Sibling properties preserved
        await Assert.That(result.Psychology.EmotionalBaseline).IsEqualTo("stoic");
        await Assert.That(result.Motivations.Needs).IsEqualTo("Expand territory");
        await Assert.That(result.BehavioralDefaults.ConflictStyle).IsEqualTo("methodical");
    }

    #endregion

    #region Test Models

    private sealed class CharacterStats
    {
        [JsonPropertyName("character_identity")]
        public required CharacterIdentity CharacterIdentity { get; set; }

        [JsonPropertyName("psychology")]
        public Psychology? Psychology { get; set; }

        [JsonPropertyName("motivations")]
        public Motivations? Motivations { get; set; }

        [JsonPropertyName("behavioral_defaults")]
        public BehavioralDefaults? BehavioralDefaults { get; set; }

        [JsonPropertyName("in_development")]
        public InDevelopment? InDevelopment { get; set; }

        [JsonExtensionData]
        public IDictionary<string, object>? ExtensionData { get; set; }
    }

    private sealed class CharacterIdentity
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("role")]
        public string? Role { get; set; }
    }

    private sealed class Psychology
    {
        [JsonPropertyName("emotional_baseline")]
        public string? EmotionalBaseline { get; set; }

        [JsonPropertyName("triggers")]
        public Triggers? Triggers { get; set; }
    }

    private sealed class Triggers
    {
        [JsonPropertyName("primary_trigger")]
        public string? PrimaryTrigger { get; set; }

        [JsonPropertyName("secondary_triggers")]
        public List<string>? SecondaryTriggers { get; set; }

        [JsonPropertyName("intensity")]
        public double Intensity { get; set; }

        [JsonPropertyName("response")]
        public string? Response { get; set; }
    }

    private sealed class Motivations
    {
        [JsonPropertyName("needs")]
        public string? Needs { get; set; }

        [JsonPropertyName("goals_current")]
        public string? GoalsCurrent { get; set; }
    }

    private sealed class BehavioralDefaults
    {
        [JsonPropertyName("conflict_style")]
        public string? ConflictStyle { get; set; }

        [JsonPropertyName("routine")]
        public Routine? Routine { get; set; }
    }

    private sealed class Routine
    {
        [JsonPropertyName("daily_pattern")]
        public string? DailyPattern { get; set; }

        [JsonPropertyName("activities")]
        public List<string>? Activities { get; set; }
    }

    private sealed class InDevelopment
    {
        [JsonPropertyName("aspect")]
        public string? Aspect { get; set; }

        [JsonPropertyName("progress")]
        public int Progress { get; set; }
    }

    private static CharacterStats CreateTestCharacter() =>
        new()
        {
            CharacterIdentity = new CharacterIdentity
            {
                Name = "Viktor Volkov",
                Role = "Antagonist"
            },
            Psychology = new Psychology
            {
                Triggers = new Triggers
                {
                    PrimaryTrigger = "calm",
                    SecondaryTriggers = ["calculating"],
                    Intensity = 0.3,
                    Response = "routine business"
                },
                EmotionalBaseline = "stoic"
            },
            Motivations = new Motivations
            {
                Needs = "Expand territory",
                GoalsCurrent = "Monitor competitors"
            },
            BehavioralDefaults = new BehavioralDefaults
            {
                ConflictStyle = "methodical",
                Routine = new Routine
                {
                    DailyPattern = "gather information",
                    Activities = ["observe", "report", "plan"]
                }
            },
            InDevelopment = new InDevelopment
            {
                Aspect = "rising_action",
                Progress = 25
            }
        };

    #endregion

    #region Basic Functionality Tests

    [Test]
    public async Task PatchWith_EmptyUpdates_ReturnsOriginalUnchanged()
    {
        // Arrange
        var original = CreateTestCharacter();
        var updates = new Dictionary<string, object>();

        // Act
        var result = original.PatchWith(updates);

        // Assert
        await Assert.That(result.CharacterIdentity.Name).IsEqualTo("Viktor Volkov");
        await Assert.That(result.Psychology!.Triggers!.PrimaryTrigger).IsEqualTo("calm");
    }

    [Test]
    public async Task PatchWith_TopLevelProperty_ReplacesEntireObject()
    {
        // Arrange
        var original = CreateTestCharacter();
        var newIdentity = new CharacterIdentity
        {
            Name = "Ivan Petrov",
            Role = "Informant"
        };
        var updates = new Dictionary<string, object>
        {
            ["character_identity"] = newIdentity
        };

        // Act
        var result = original.PatchWith(updates);

        // Assert
        await Assert.That(result.CharacterIdentity.Name).IsEqualTo("Ivan Petrov");
        await Assert.That(result.CharacterIdentity.Role).IsEqualTo("Informant");
        // Other properties should remain unchanged
        await Assert.That(result.Psychology!.EmotionalBaseline).IsEqualTo("stoic");
    }

    [Test]
    public async Task PatchWith_NestedPath_ReplacesAtPath()
    {
        // Arrange
        var original = CreateTestCharacter();
        var newCurrentState = new Triggers
        {
            PrimaryTrigger = "anxious",
            SecondaryTriggers = ["calculating", "defensive"],
            Intensity = 0.7,
            Response = "protagonist getting too close"
        };
        var updates = new Dictionary<string, object>
        {
            ["psychology.triggers"] = newCurrentState
        };

        // Act
        var result = original.PatchWith(updates);

        // Assert
        await Assert.That(result.Psychology!.Triggers!.PrimaryTrigger).IsEqualTo("anxious");
        await Assert.That(result.Psychology.Triggers.Intensity).IsEqualTo(0.7);
        await Assert.That(result.Psychology.Triggers.SecondaryTriggers).Contains("defensive");
        // Sibling properties should remain unchanged
        await Assert.That(result.Psychology.EmotionalBaseline).IsEqualTo("stoic");
    }

    [Test]
    public async Task PatchWith_DeepNestedPath_ReplacesAtPath()
    {
        // Arrange
        var original = CreateTestCharacter();
        var newPlan = new Routine
        {
            DailyPattern = "assess damage",
            Activities = ["talk to dockmaster", "check records", "prepare alibi"]
        };
        var updates = new Dictionary<string, object>
        {
            ["behavioral_defaults.routine"] = newPlan
        };

        // Act
        var result = original.PatchWith(updates);

        // Assert
        await Assert.That(result.BehavioralDefaults!.Routine!.DailyPattern).IsEqualTo("assess damage");
        await Assert.That(result.BehavioralDefaults.Routine.Activities).Contains("prepare alibi");
        // Sibling property should remain unchanged
        await Assert.That(result.BehavioralDefaults.ConflictStyle).IsEqualTo("methodical");
    }

    #endregion

    #region Multiple Updates Tests

    [Test]
    public async Task PatchWith_MultipleUpdates_AppliesAll()
    {
        // Arrange
        var original = CreateTestCharacter();
        var updates = new Dictionary<string, object>
        {
            ["psychology.triggers"] = new Triggers
            {
                PrimaryTrigger = "anxious",
                SecondaryTriggers = ["calculating", "defensive"],
                Intensity = 0.7,
                Response = "protagonist getting too close"
            },
            ["motivations.goals_current"] = "gather intelligence before next confrontation",
            ["behavioral_defaults.routine"] = new Routine
            {
                DailyPattern = "assess damage",
                Activities = ["talk to dockmaster", "check records", "prepare alibi"]
            }
        };

        // Act
        var result = original.PatchWith(updates);

        // Assert - All updates applied
        await Assert.That(result.Psychology!.Triggers!.PrimaryTrigger).IsEqualTo("anxious");
        await Assert.That(result.Motivations!.GoalsCurrent).IsEqualTo("gather intelligence before next confrontation");
        await Assert.That(result.BehavioralDefaults!.Routine!.DailyPattern).IsEqualTo("assess damage");

        // Assert - Unchanged properties preserved
        await Assert.That(result.CharacterIdentity.Name).IsEqualTo("Viktor Volkov");
        await Assert.That(result.Psychology.EmotionalBaseline).IsEqualTo("stoic");
        await Assert.That(result.Motivations.Needs).IsEqualTo("Expand territory");
        await Assert.That(result.BehavioralDefaults.ConflictStyle).IsEqualTo("methodical");
    }

    [Test]
    public async Task PatchWith_UpdateSiblingPaths_BothApplied()
    {
        // Arrange
        var original = CreateTestCharacter();
        var updates = new Dictionary<string, object>
        {
            ["psychology.triggers"] = new Triggers
            {
                PrimaryTrigger = "angry",
                SecondaryTriggers = ["frustrated"],
                Intensity = 0.8,
                Response = "betrayal discovered"
            },
            ["psychology.emotional_baseline"] = "volatile"
        };

        // Act
        var result = original.PatchWith(updates);

        // Assert - Both emotional_landscape children updated
        await Assert.That(result.Psychology!.Triggers!.PrimaryTrigger).IsEqualTo("angry");
        await Assert.That(result.Psychology.EmotionalBaseline).IsEqualTo("volatile");
    }

    #endregion

    #region Primitive Value Tests

    [Test]
    public async Task PatchWith_StringValue_ReplacesString()
    {
        // Arrange
        var original = CreateTestCharacter();
        var updates = new Dictionary<string, object>
        {
            ["motivations.goals_current"] = "flee the scene"
        };

        // Act
        var result = original.PatchWith(updates);

        // Assert
        await Assert.That(result.Motivations!.GoalsCurrent).IsEqualTo("flee the scene");
        await Assert.That(result.Motivations.Needs).IsEqualTo("Expand territory");
    }

    [Test]
    public async Task PatchWith_IntegerValue_ReplacesInteger()
    {
        // Arrange
        var original = CreateTestCharacter();
        var updates = new Dictionary<string, object>
        {
            ["in_development.progress"] = 75
        };

        // Act
        var result = original.PatchWith(updates);

        // Assert
        await Assert.That(result.InDevelopment!.Progress).IsEqualTo(75);
        await Assert.That(result.InDevelopment.Aspect).IsEqualTo("rising_action");
    }

    [Test]
    public async Task PatchWith_DoubleValue_ReplacesDouble()
    {
        // Arrange
        var original = CreateTestCharacter();
        var newState = new Triggers
        {
            PrimaryTrigger = "calm",
            SecondaryTriggers = [],
            Intensity = 0.95,
            Response = "meditation"
        };
        var updates = new Dictionary<string, object>
        {
            ["psychology.triggers"] = newState
        };

        // Act
        var result = original.PatchWith(updates);

        // Assert
        await Assert.That(result.Psychology!.Triggers!.Intensity).IsEqualTo(0.95);
    }

    #endregion

    #region Edge Cases

    [Test]
    public async Task PatchWith_NullValue_SetsToNull()
    {
        // Arrange
        var original = CreateTestCharacter();
        var updates = new Dictionary<string, object>
        {
            ["behavioral_defaults.routine"] = null!
        };

        // Act
        var result = original.PatchWith(updates);

        // Assert
        await Assert.That(result.BehavioralDefaults!.Routine).IsNull();
        await Assert.That(result.BehavioralDefaults.ConflictStyle).IsEqualTo("methodical");
    }

    [Test]
    public async Task PatchWith_PreservesUnrelatedNestedStructure()
    {
        // Arrange
        var original = CreateTestCharacter();
        var updates = new Dictionary<string, object>
        {
            ["in_development.aspect"] = "climax"
        };

        // Act
        var result = original.PatchWith(updates);

        // Assert - Updated property changed
        await Assert.That(result.InDevelopment!.Aspect).IsEqualTo("climax");

        // Assert - All other nested structures preserved completely
        await Assert.That(result.CharacterIdentity.Name).IsEqualTo("Viktor Volkov");
        await Assert.That(result.Psychology!.Triggers!.PrimaryTrigger).IsEqualTo("calm");
        await Assert.That(result.Psychology.Triggers.SecondaryTriggers).Contains("calculating");
        await Assert.That(result.Motivations!.Needs).IsEqualTo("Expand territory");
        await Assert.That(result.BehavioralDefaults!.Routine!.Activities).Contains("observe");
    }

    [Test]
    public async Task PatchWith_JsonElementValue_WorksCorrectly()
    {
        // Arrange - Simulates receiving updates from JSON deserialization
        var original = CreateTestCharacter();
        var updateJson = """
                         {
                             "primary_trigger": "terrified",
                             "secondary_triggers": ["panicked"],
                             "intensity": 0.9,
                             "response": "ambush"
                         }
                         """;
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(updateJson);
        var updates = new Dictionary<string, object>
        {
            ["psychology.triggers"] = jsonElement
        };

        // Act
        var result = original.PatchWith(updates);

        // Assert
        await Assert.That(result.Psychology!.Triggers!.PrimaryTrigger).IsEqualTo("terrified");
        await Assert.That(result.Psychology.Triggers.Intensity).IsEqualTo(0.9);
    }

    #endregion

    #region Array Item Replacement Tests

    private sealed class CharacterWithSkills
    {
        [JsonPropertyName("Name")]
        public string? Name { get; set; }

        [JsonPropertyName("Skills")]
        public List<Skill>? Skills { get; set; }

        [JsonExtensionData]
        public IDictionary<string, object>? ExtensionData { get; set; }
    }

    private sealed class Skill
    {
        [JsonPropertyName("SkillName")]
        public string? SkillName { get; set; }

        [JsonPropertyName("Category")]
        public string? Category { get; set; }

        [JsonPropertyName("Proficiency")]
        public string? Proficiency { get; set; }

        [JsonPropertyName("XP")]
        public SkillXP? XP { get; set; }

        [JsonPropertyName("ChallengeFloor")]
        public string? ChallengeFloor { get; set; }

        [JsonPropertyName("Development")]
        public string? Development { get; set; }

        [JsonPropertyName("RecentGains")]
        public string? RecentGains { get; set; }
    }

    private sealed class SkillXP
    {
        [JsonPropertyName("Current")]
        public string? Current { get; set; }

        [JsonPropertyName("NextThreshold")]
        public string? NextThreshold { get; set; }

        [JsonPropertyName("ToNext")]
        public string? ToNext { get; set; }
    }

    private static CharacterWithSkills CreateCharacterWithSkills() =>
        new()
        {
            Name = "Test Character",
            Skills =
            [
                new Skill
                {
                    SkillName = "Consciousness Analysis",
                    Category = "Transcendent",
                    Proficiency = "Transcendent",
                    XP = new SkillXP
                    {
                        Current = "Beyond measurement",
                        NextThreshold = "MAX",
                        ToNext = "N/A"
                    },
                    ChallengeFloor = "Transcendent",
                    Development = "Always existed",
                    RecentGains = "Constant"
                },
                new Skill
                {
                    SkillName = "Reality Manipulation",
                    Category = "Transcendent",
                    Proficiency = "Transcendent",
                    XP = new SkillXP
                    {
                        Current = "Beyond measurement",
                        NextThreshold = "MAX",
                        ToNext = "N/A"
                    },
                    ChallengeFloor = "Transcendent",
                    Development = "Always existed",
                    RecentGains = "Constant"
                }
            ]
        };

    [Test]
    public async Task PatchWith_ArrayItemByIdentifier_ReplacesSingleItem()
    {
        // Arrange
        var original = CreateCharacterWithSkills();
        var updatedSkill = new Skill
        {
            SkillName = "Consciousness Analysis",
            Category = "Transcendent",
            Proficiency = "Transcendent",
            XP = new SkillXP
            {
                Current = "Beyond measurement",
                NextThreshold = "MAX",
                ToNext = "N/A"
            },
            ChallengeFloor = "Transcendent",
            Development = "Always existed, constantly evolving. Current focus: Analyzing Lua's 'surrender as completion' paradigm.",
            RecentGains = "05:05: +25 XP - Initial observation | 05:07: +50 XP - Analysis of surrender philosophy"
        };
        var updates = new Dictionary<string, object>
        {
            ["Skills[Consciousness Analysis]"] = updatedSkill
        };

        // Act
        var result = original.PatchWith(updates);

        // Assert - The updated skill should have new values
        var consciousnessSkill = result.Skills!.Single(s => s.SkillName == "Consciousness Analysis");
        await Assert.That(consciousnessSkill.Development).Contains("constantly evolving");
        await Assert.That(consciousnessSkill.RecentGains).Contains("+25 XP");
        await Assert.That(consciousnessSkill.RecentGains).Contains("+50 XP");

        // Assert - Other skills should remain unchanged
        var realitySkill = result.Skills!.Single(s => s.SkillName == "Reality Manipulation");
        await Assert.That(realitySkill.Development).IsEqualTo("Always existed");
        await Assert.That(realitySkill.RecentGains).IsEqualTo("Constant");

        // Assert - Array length should remain the same
        await Assert.That(result.Skills!.Count).IsEqualTo(2);
    }

    [Test]
    public async Task PatchWith_ArrayItemByIdentifier_PreservesOtherArrayItems()
    {
        // Arrange
        var original = CreateCharacterWithSkills();
        var updatedSkill = new Skill
        {
            SkillName = "Reality Manipulation",
            Category = "Transcendent",
            Proficiency = "Master",
            XP = new SkillXP
            {
                Current = "10000",
                NextThreshold = "15000",
                ToNext = "5000"
            },
            ChallengeFloor = "Expert",
            Development = "Rapid growth phase",
            RecentGains = "10:00: +100 XP - Combat practice"
        };
        var updates = new Dictionary<string, object>
        {
            ["Skills[Reality Manipulation]"] = updatedSkill
        };

        // Act
        var result = original.PatchWith(updates);

        // Assert - Updated skill changed
        var realitySkill = result.Skills!.Single(s => s.SkillName == "Reality Manipulation");
        await Assert.That(realitySkill.Proficiency).IsEqualTo("Master");
        await Assert.That(realitySkill.Development).IsEqualTo("Rapid growth phase");

        // Assert - Other skill unchanged
        var consciousnessSkill = result.Skills!.Single(s => s.SkillName == "Consciousness Analysis");
        await Assert.That(consciousnessSkill.Development).IsEqualTo("Always existed");
    }

    [Test]
    public async Task PatchWith_ArrayItemByIdentifier_WithNestedPath_UpdatesNestedProperty()
    {
        // Arrange
        var original = CreateCharacterWithSkills();
        var updates = new Dictionary<string, object>
        {
            ["Skills[Consciousness Analysis].Development"] = "Updated development text"
        };

        // Act
        var result = original.PatchWith(updates);

        // Assert - Only the Development property should be updated
        var consciousnessSkill = result.Skills!.Single(s => s.SkillName == "Consciousness Analysis");
        await Assert.That(consciousnessSkill.Development).IsEqualTo("Updated development text");
        await Assert.That(consciousnessSkill.RecentGains).IsEqualTo("Constant");
        await Assert.That(consciousnessSkill.Category).IsEqualTo("Transcendent");
    }

    [Test]
    public async Task PatchWith_ArrayItemByIdentifier_WithDeepNestedPath_UpdatesDeepNestedProperty()
    {
        // Arrange
        var original = CreateCharacterWithSkills();
        var updates = new Dictionary<string, object>
        {
            ["Skills[Reality Manipulation].XP.Current"] = "15000"
        };

        // Act
        var result = original.PatchWith(updates);

        // Assert - Only the XP.Current should be updated
        var realitySkill = result.Skills!.Single(s => s.SkillName == "Reality Manipulation");
        await Assert.That(realitySkill.XP!.Current).IsEqualTo("15000");
        await Assert.That(realitySkill.XP!.NextThreshold).IsEqualTo("MAX");
        await Assert.That(realitySkill.XP!.ToNext).IsEqualTo("N/A");
    }

    [Test]
    public async Task PatchWith_MultipleArrayItemUpdates_AppliesAll()
    {
        // Arrange
        var original = CreateCharacterWithSkills();
        var updates = new Dictionary<string, object>
        {
            ["Skills[Consciousness Analysis].Development"] = "New consciousness development",
            ["Skills[Reality Manipulation].Development"] = "New reality development"
        };

        // Act
        var result = original.PatchWith(updates);

        // Assert - Both skills updated
        var consciousnessSkill = result.Skills!.Single(s => s.SkillName == "Consciousness Analysis");
        await Assert.That(consciousnessSkill.Development).IsEqualTo("New consciousness development");

        var realitySkill = result.Skills!.Single(s => s.SkillName == "Reality Manipulation");
        await Assert.That(realitySkill.Development).IsEqualTo("New reality development");
    }

    [Test]
    public async Task PatchWith_ArrayItemNotFound_AddsNewItem()
    {
        // Arrange
        var original = CreateCharacterWithSkills();
        var newSkill = new Skill
        {
            SkillName = "Teleportation",
            Category = "Spatial",
            Proficiency = "Novice",
            XP = new SkillXP
            {
                Current = "100",
                NextThreshold = "500",
                ToNext = "400"
            },
            ChallengeFloor = "Beginner",
            Development = "Just learned",
            RecentGains = "10:00: +100 XP - First attempt"
        };
        var updates = new Dictionary<string, object>
        {
            ["Skills[Teleportation]"] = newSkill
        };

        // Act
        var result = original.PatchWith(updates);

        // Assert - New skill should be added
        await Assert.That(result.Skills!.Count).IsEqualTo(3);
        var addedSkill = result.Skills!.SingleOrDefault(s => s.SkillName == "Teleportation");
        await Assert.That(addedSkill).IsNotNull();
        await Assert.That(addedSkill!.Category).IsEqualTo("Spatial");
        await Assert.That(addedSkill.Proficiency).IsEqualTo("Novice");

        // Assert - Existing skills unchanged
        var consciousnessSkill = result.Skills!.Single(s => s.SkillName == "Consciousness Analysis");
        await Assert.That(consciousnessSkill.Development).IsEqualTo("Always existed");
    }

    [Test]
    public async Task PatchWith_ArrayItemByIdentifier_PreservesCharacterName()
    {
        // Arrange
        var original = CreateCharacterWithSkills();
        var updatedSkill = new Skill
        {
            SkillName = "Consciousness Analysis",
            Category = "Transcendent",
            Proficiency = "Transcendent",
            XP = new SkillXP
            {
                Current = "Beyond measurement",
                NextThreshold = "MAX",
                ToNext = "N/A"
            },
            ChallengeFloor = "Transcendent",
            Development = "Updated development",
            RecentGains = "Updated gains"
        };
        var updates = new Dictionary<string, object>
        {
            ["Skills[Consciousness Analysis]"] = updatedSkill
        };

        // Act
        var result = original.PatchWith(updates);

        // Assert - Character name preserved
        await Assert.That(result.Name).IsEqualTo("Test Character");
    }

    [Test]
    public async Task PatchWith_ArrayItemByIdentifier_NullValue_RemovesItem()
    {
        // Arrange
        var original = CreateCharacterWithSkills();
        var updates = new Dictionary<string, object>
        {
            ["Skills[Reality Manipulation]"] = null!
        };

        // Act
        var result = original.PatchWith(updates);

        // Assert - Item should be removed
        await Assert.That(result.Skills!.Count).IsEqualTo(1);
        await Assert.That(result.Skills!.Any(s => s.SkillName == "Reality Manipulation")).IsFalse();

        // Assert - Other items preserved
        await Assert.That(result.Skills!.Any(s => s.SkillName == "Consciousness Analysis")).IsTrue();
    }

    #endregion

    #region Nested Path with Array Access Tests

    private sealed class CharacterWithMagic
    {
        [JsonPropertyName("Name")]
        public string? Name { get; set; }

        [JsonPropertyName("MagicAndAbilities")]
        public MagicAndAbilities? MagicAndAbilities { get; set; }

        [JsonExtensionData]
        public IDictionary<string, object>? ExtensionData { get; set; }
    }

    private sealed class MagicAndAbilities
    {
        [JsonPropertyName("ManaPool")]
        public string? ManaPool { get; set; }

        [JsonPropertyName("InstinctiveAbilities")]
        public List<Ability>? InstinctiveAbilities { get; set; }

        [JsonPropertyName("LearnedAbilities")]
        public List<Ability>? LearnedAbilities { get; set; }
    }

    private sealed class Ability
    {
        [JsonPropertyName("AbilityName")]
        public string? AbilityName { get; set; }

        [JsonPropertyName("Type")]
        public string? Type { get; set; }

        [JsonPropertyName("Power")]
        public int Power { get; set; }

        [JsonPropertyName("CooldownSeconds")]
        public int CooldownSeconds { get; set; }

        [JsonPropertyName("Description")]
        public string? Description { get; set; }
    }

    private static CharacterWithMagic CreateCharacterWithMagic() =>
        new()
        {
            Name = "Dragon Character",
            MagicAndAbilities = new MagicAndAbilities
            {
                ManaPool = "1000/1000",
                InstinctiveAbilities =
                [
                    new Ability
                    {
                        AbilityName = "Fire Breath",
                        Type = "Fire",
                        Power = 150,
                        CooldownSeconds = 30,
                        Description = "Breathes a cone of fire"
                    },
                    new Ability
                    {
                        AbilityName = "Tail Swipe",
                        Type = "Physical",
                        Power = 80,
                        CooldownSeconds = 5,
                        Description = "Sweeps tail in an arc"
                    }
                ],
                LearnedAbilities =
                [
                    new Ability
                    {
                        AbilityName = "Ice Storm",
                        Type = "Ice",
                        Power = 200,
                        CooldownSeconds = 60,
                        Description = "Creates a devastating ice storm"
                    }
                ]
            }
        };

    [Test]
    public async Task PatchWith_NestedPathWithArrayAccess_ReplacesItem()
    {
        // Arrange
        var original = CreateCharacterWithMagic();
        var updatedAbility = new Ability
        {
            AbilityName = "Fire Breath",
            Type = "Fire",
            Power = 200,
            CooldownSeconds = 25,
            Description = "Breathes an enhanced cone of scorching fire"
        };
        var updates = new Dictionary<string, object>
        {
            ["MagicAndAbilities.InstinctiveAbilities[Fire Breath]"] = updatedAbility
        };

        // Act
        var result = original.PatchWith(updates);

        // Assert - Fire Breath should be updated
        var fireBreath = result.MagicAndAbilities!.InstinctiveAbilities!.Single(a => a.AbilityName == "Fire Breath");
        await Assert.That(fireBreath.Power).IsEqualTo(200);
        await Assert.That(fireBreath.CooldownSeconds).IsEqualTo(25);
        await Assert.That(fireBreath.Description).Contains("enhanced");

        // Assert - Other abilities unchanged
        var tailSwipe = result.MagicAndAbilities!.InstinctiveAbilities!.Single(a => a.AbilityName == "Tail Swipe");
        await Assert.That(tailSwipe.Power).IsEqualTo(80);
    }

    [Test]
    public async Task PatchWith_NestedPathWithArrayAccess_AndPropertyPath_UpdatesProperty()
    {
        // Arrange
        var original = CreateCharacterWithMagic();
        var updates = new Dictionary<string, object>
        {
            ["MagicAndAbilities.InstinctiveAbilities[Fire Breath].Power"] = 250
        };

        // Act
        var result = original.PatchWith(updates);

        // Assert - Only Power should be updated
        var fireBreath = result.MagicAndAbilities!.InstinctiveAbilities!.Single(a => a.AbilityName == "Fire Breath");
        await Assert.That(fireBreath.Power).IsEqualTo(250);
        await Assert.That(fireBreath.CooldownSeconds).IsEqualTo(30);
        await Assert.That(fireBreath.Description).IsEqualTo("Breathes a cone of fire");
    }

    [Test]
    public async Task PatchWith_NestedPathWithArrayAccess_DifferentArrays_UpdatesCorrectArray()
    {
        // Arrange
        var original = CreateCharacterWithMagic();
        var updates = new Dictionary<string, object>
        {
            ["MagicAndAbilities.LearnedAbilities[Ice Storm].Power"] = 300
        };

        // Act
        var result = original.PatchWith(updates);

        // Assert - Ice Storm in LearnedAbilities should be updated
        var iceStorm = result.MagicAndAbilities!.LearnedAbilities!.Single(a => a.AbilityName == "Ice Storm");
        await Assert.That(iceStorm.Power).IsEqualTo(300);

        // Assert - InstinctiveAbilities should remain unchanged
        var fireBreath = result.MagicAndAbilities.InstinctiveAbilities!.Single(a => a.AbilityName == "Fire Breath");
        await Assert.That(fireBreath.Power).IsEqualTo(150);
    }

    [Test]
    public async Task PatchWith_NestedPathWithArrayAccess_PreservesSiblingProperties()
    {
        // Arrange
        var original = CreateCharacterWithMagic();
        var updates = new Dictionary<string, object>
        {
            ["MagicAndAbilities.InstinctiveAbilities[Tail Swipe].Description"] = "A powerful tail sweep"
        };

        // Act
        var result = original.PatchWith(updates);

        // Assert - ManaPool preserved
        await Assert.That(result.MagicAndAbilities!.ManaPool).IsEqualTo("1000/1000");

        // Assert - Character name preserved
        await Assert.That(result.Name).IsEqualTo("Dragon Character");

        // Assert - Other ability preserved
        var fireBreath = result.MagicAndAbilities.InstinctiveAbilities!.Single(a => a.AbilityName == "Fire Breath");
        await Assert.That(fireBreath.Description).IsEqualTo("Breathes a cone of fire");
    }

    [Test]
    public async Task PatchWith_MultipleNestedPathsWithArrayAccess_AppliesAll()
    {
        // Arrange
        var original = CreateCharacterWithMagic();
        var updates = new Dictionary<string, object>
        {
            ["MagicAndAbilities.InstinctiveAbilities[Fire Breath].Power"] = 180,
            ["MagicAndAbilities.InstinctiveAbilities[Tail Swipe].Power"] = 100,
            ["MagicAndAbilities.LearnedAbilities[Ice Storm].CooldownSeconds"] = 45,
            ["MagicAndAbilities.ManaPool"] = "1500/1500"
        };

        // Act
        var result = original.PatchWith(updates);

        // Assert - All updates applied
        var fireBreath = result.MagicAndAbilities!.InstinctiveAbilities!.Single(a => a.AbilityName == "Fire Breath");
        await Assert.That(fireBreath.Power).IsEqualTo(180);

        var tailSwipe = result.MagicAndAbilities!.InstinctiveAbilities!.Single(a => a.AbilityName == "Tail Swipe");
        await Assert.That(tailSwipe.Power).IsEqualTo(100);

        var iceStorm = result.MagicAndAbilities!.LearnedAbilities!.Single(a => a.AbilityName == "Ice Storm");
        await Assert.That(iceStorm.CooldownSeconds).IsEqualTo(45);

        await Assert.That(result.MagicAndAbilities.ManaPool).IsEqualTo("1500/1500");
    }

    [Test]
    public async Task PatchWith_NestedPathWithArrayAccess_ItemNotFound_AddsNewItem()
    {
        // Arrange
        var original = CreateCharacterWithMagic();
        var newAbility = new Ability
        {
            AbilityName = "Lightning Bolt",
            Type = "Lightning",
            Power = 175,
            CooldownSeconds = 20,
            Description = "Strikes with a bolt of lightning"
        };
        var updates = new Dictionary<string, object>
        {
            ["MagicAndAbilities.InstinctiveAbilities[Lightning Bolt]"] = newAbility
        };

        // Act
        var result = original.PatchWith(updates);

        // Assert - New ability should be added
        await Assert.That(result.MagicAndAbilities!.InstinctiveAbilities!.Count).IsEqualTo(3);
        var addedAbility = result.MagicAndAbilities.InstinctiveAbilities.SingleOrDefault(a => a.AbilityName == "Lightning Bolt");
        await Assert.That(addedAbility).IsNotNull();
        await Assert.That(addedAbility!.Type).IsEqualTo("Lightning");
        await Assert.That(addedAbility.Power).IsEqualTo(175);

        // Assert - Existing abilities unchanged
        var fireBreath = result.MagicAndAbilities.InstinctiveAbilities.Single(a => a.AbilityName == "Fire Breath");
        await Assert.That(fireBreath.Power).IsEqualTo(150);
    }

    [Test]
    public async Task PatchWith_NestedPathWithArrayAccess_ArrayNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        var original = CreateCharacterWithMagic();
        var updates = new Dictionary<string, object>
        {
            ["MagicAndAbilities.UltimateAbilities[Super Attack]"] = new Ability { AbilityName = "Super Attack" }
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Task.FromResult(original.PatchWith(updates)));
    }

    [Test]
    public async Task PatchWith_AddMultipleNewArrayItems_AddsAll()
    {
        // Arrange
        var original = CreateCharacterWithSkills();
        var updates = new Dictionary<string, object>
        {
            ["Skills[Telekinesis]"] = new Skill
            {
                SkillName = "Telekinesis",
                Category = "Mental",
                Proficiency = "Novice"
            },
            ["Skills[Pyromancy]"] = new Skill
            {
                SkillName = "Pyromancy",
                Category = "Elemental",
                Proficiency = "Apprentice"
            }
        };

        // Act
        var result = original.PatchWith(updates);

        // Assert - Both new skills should be added
        await Assert.That(result.Skills!.Count).IsEqualTo(4);
        await Assert.That(result.Skills!.Any(s => s.SkillName == "Telekinesis")).IsTrue();
        await Assert.That(result.Skills!.Any(s => s.SkillName == "Pyromancy")).IsTrue();

        // Assert - Existing skills unchanged
        await Assert.That(result.Skills!.Any(s => s.SkillName == "Consciousness Analysis")).IsTrue();
        await Assert.That(result.Skills!.Any(s => s.SkillName == "Reality Manipulation")).IsTrue();
    }

    [Test]
    public async Task PatchWith_MixedUpdateAndAddArrayItems_AppliesBoth()
    {
        // Arrange
        var original = CreateCharacterWithSkills();
        var updates = new Dictionary<string, object>
        {
            // Update existing
            ["Skills[Consciousness Analysis].Development"] = "Updated development",
            // Add new
            ["Skills[Time Manipulation]"] = new Skill
            {
                SkillName = "Time Manipulation",
                Category = "Temporal",
                Proficiency = "Master"
            }
        };

        // Act
        var result = original.PatchWith(updates);

        // Assert - Existing skill updated
        var consciousnessSkill = result.Skills!.Single(s => s.SkillName == "Consciousness Analysis");
        await Assert.That(consciousnessSkill.Development).IsEqualTo("Updated development");

        // Assert - New skill added
        await Assert.That(result.Skills!.Count).IsEqualTo(3);
        var newSkill = result.Skills!.SingleOrDefault(s => s.SkillName == "Time Manipulation");
        await Assert.That(newSkill).IsNotNull();
        await Assert.That(newSkill!.Category).IsEqualTo("Temporal");
    }

    [Test]
    public async Task PatchWith_AddNewItemToEmptyArray_AddsItem()
    {
        // Arrange
        var original = new CharacterWithSkills
        {
            Name = "Empty Skills Character",
            Skills = []
        };
        var newSkill = new Skill
        {
            SkillName = "First Skill",
            Category = "Basic",
            Proficiency = "Beginner"
        };
        var updates = new Dictionary<string, object>
        {
            ["Skills[First Skill]"] = newSkill
        };

        // Act
        var result = original.PatchWith(updates);

        // Assert
        await Assert.That(result.Skills!.Count).IsEqualTo(1);
        await Assert.That(result.Skills![0].SkillName).IsEqualTo("First Skill");
    }

    #endregion

    #region In Development Array Tests

    private sealed class CharacterWithInDevelopment
    {
        [JsonPropertyName("Name")]
        public string? Name { get; set; }

        [JsonPropertyName("in_development")]
        public List<DevelopmentAspect>? InDevelopment { get; set; }

        [JsonExtensionData]
        public IDictionary<string, object>? ExtensionData { get; set; }
    }

    private sealed class DevelopmentAspect
    {
        [JsonPropertyName("aspect")]
        public string? Aspect { get; set; }

        [JsonPropertyName("from")]
        public string? From { get; set; }

        [JsonPropertyName("toward")]
        public string? Toward { get; set; }

        [JsonPropertyName("pressure")]
        public string? Pressure { get; set; }

        [JsonPropertyName("resistance")]
        public string? Resistance { get; set; }

        [JsonPropertyName("intensity")]
        public string? Intensity { get; set; }
    }

    private static CharacterWithInDevelopment CreateCharacterWithInDevelopment() =>
        new()
        {
            Name = "Test Character",
            InDevelopment =
            [
                new DevelopmentAspect
                {
                    Aspect = "Curiosity vs. Discipline",
                    From = "Standard patrol assessment",
                    Toward = "Following protocol",
                    Pressure = "Nothing unusual",
                    Resistance = "None",
                    Intensity = "Low"
                },
                new DevelopmentAspect
                {
                    Aspect = "Trust vs. Suspicion",
                    From = "Neutral stance",
                    Toward = "Building trust",
                    Pressure = "New encounters",
                    Resistance = "Past experiences",
                    Intensity = "Medium"
                }
            ]
        };

    [Test]
    public async Task PatchWith_InDevelopmentArrayItem_WithoutQuotes_ReplacesItem()
    {
        // Arrange
        var original = CreateCharacterWithInDevelopment();
        var updatedAspect = new DevelopmentAspect
        {
            Aspect = "Curiosity vs. Discipline",
            From = "Standard patrol assessment",
            Toward = "Pushing boundaries for answers",
            Pressure = "The human doesn't fit any known pattern—no trail, wrong scent, impossible story",
            Resistance = "Patrol protocol, Varek's authority, the reality that containment has begun",
            Intensity = "Noticing. Not yet acting on it, but circling one too many times."
        };
        var updates = new Dictionary<string, object>
        {
            // Path WITHOUT quotes around the identifier - this is the supported format
            ["in_development[Curiosity vs. Discipline]"] = updatedAspect
        };

        // Act
        var result = original.PatchWith(updates);

        // Assert - The aspect should be updated
        var curiosityAspect = result.InDevelopment!.Single(a => a.Aspect == "Curiosity vs. Discipline");
        await Assert.That(curiosityAspect.Toward).IsEqualTo("Pushing boundaries for answers");
        await Assert.That(curiosityAspect.Pressure).Contains("human doesn't fit any known pattern");
        await Assert.That(curiosityAspect.Resistance).Contains("Patrol protocol");
        await Assert.That(curiosityAspect.Intensity).Contains("circling one too many times");

        // Assert - Other aspects should remain unchanged
        var trustAspect = result.InDevelopment!.Single(a => a.Aspect == "Trust vs. Suspicion");
        await Assert.That(trustAspect.Toward).IsEqualTo("Building trust");

        // Assert - Array length should remain the same
        await Assert.That(result.InDevelopment!.Count).IsEqualTo(2);
    }

    [Test]
    public async Task PatchWith_InDevelopmentArrayItem_WithDotInIdentifier_ReplacesItem()
    {
        // Arrange
        var original = CreateCharacterWithInDevelopment();
        var updatedAspect = new DevelopmentAspect
        {
            Aspect = "Curiosity vs. Discipline",
            From = "Standard patrol assessment",
            Toward = "Pushing boundaries for answers",
            Pressure = "The human doesn't fit any known pattern",
            Resistance = "Patrol protocol",
            Intensity = "High"
        };
        var updates = new Dictionary<string, object>
        {
            // The identifier contains a dot (vs.) - this must be handled correctly
            // The dot inside brackets should NOT be treated as a path separator
            ["in_development[Curiosity vs. Discipline]"] = updatedAspect
        };

        // Act
        var result = original.PatchWith(updates);

        // Assert - The aspect should be updated (not added as new)
        await Assert.That(result.InDevelopment!.Count).IsEqualTo(2);
        var curiosityAspect = result.InDevelopment!.Single(a => a.Aspect == "Curiosity vs. Discipline");
        await Assert.That(curiosityAspect.Toward).IsEqualTo("Pushing boundaries for answers");
    }

    [Test]
    public async Task PatchWith_InDevelopmentArrayItem_WithDotInIdentifier_AndNestedProperty_UpdatesCorrectly()
    {
        // Arrange
        var original = CreateCharacterWithInDevelopment();
        var updates = new Dictionary<string, object>
        {
            // Path: array access with dot in identifier, then property access
            // in_development[Curiosity vs. Discipline].intensity
            // The "vs." dot should NOT split the path
            ["in_development[Curiosity vs. Discipline].intensity"] = "Very high"
        };

        // Act
        var result = original.PatchWith(updates);

        // Assert
        var curiosityAspect = result.InDevelopment!.Single(a => a.Aspect == "Curiosity vs. Discipline");
        await Assert.That(curiosityAspect.Intensity).IsEqualTo("Very high");
        await Assert.That(curiosityAspect.From).IsEqualTo("Standard patrol assessment"); // unchanged
    }

    [Test]
    public async Task PatchWith_InDevelopmentArrayItem_UpdateNestedProperty_WorksCorrectly()
    {
        // Arrange
        var original = CreateCharacterWithInDevelopment();
        var updates = new Dictionary<string, object>
        {
            ["in_development[Curiosity vs. Discipline].intensity"] = "Very high - actively investigating"
        };

        // Act
        var result = original.PatchWith(updates);

        // Assert - Only the intensity should be updated
        var curiosityAspect = result.InDevelopment!.Single(a => a.Aspect == "Curiosity vs. Discipline");
        await Assert.That(curiosityAspect.Intensity).IsEqualTo("Very high - actively investigating");
        await Assert.That(curiosityAspect.From).IsEqualTo("Standard patrol assessment");
        await Assert.That(curiosityAspect.Toward).IsEqualTo("Following protocol");
    }

    [Test]
    public async Task PatchWith_InDevelopmentArrayItem_AddNewAspect_AddsToArray()
    {
        // Arrange
        var original = CreateCharacterWithInDevelopment();
        var newAspect = new DevelopmentAspect
        {
            Aspect = "Loyalty vs. Independence",
            From = "Pack mentality",
            Toward = "Individual choice",
            Pressure = "Conflicting orders",
            Resistance = "Years of training",
            Intensity = "Rising"
        };
        var updates = new Dictionary<string, object>
        {
            ["in_development[Loyalty vs. Independence]"] = newAspect
        };

        // Act
        var result = original.PatchWith(updates);

        // Assert - New aspect should be added
        await Assert.That(result.InDevelopment!.Count).IsEqualTo(3);
        var addedAspect = result.InDevelopment!.SingleOrDefault(a => a.Aspect == "Loyalty vs. Independence");
        await Assert.That(addedAspect).IsNotNull();
        await Assert.That(addedAspect!.From).IsEqualTo("Pack mentality");
    }

    #endregion

    #region Unicode Escape Character Matching Tests

    private sealed class CharacterWithApostropheAspects
    {
        [JsonPropertyName("Name")]
        public string? Name { get; set; }

        [JsonPropertyName("in_development")]
        public List<DevelopmentAspect>? InDevelopment { get; set; }

        [JsonExtensionData]
        public IDictionary<string, object>? ExtensionData { get; set; }
    }

    private static CharacterWithApostropheAspects CreateCharacterWithApostropheAspects() =>
        new()
        {
            Name = "Test Character",
            InDevelopment =
            [
                new DevelopmentAspect
                {
                    Aspect = "Curiosity about Lily's anomaly",
                    From = "Initial observation",
                    Toward = "Deep investigation",
                    Pressure = "Unknown phenomenon",
                    Resistance = "Standard protocols",
                    Intensity = "Medium"
                },
                new DevelopmentAspect
                {
                    Aspect = "Trust in Marcus's judgment",
                    From = "Skeptical",
                    Toward = "Accepting",
                    Pressure = "Evidence mounting",
                    Resistance = "Past betrayals",
                    Intensity = "Low"
                }
            ]
        };

    [Test]
    public async Task PatchWith_ArrayItemWithEscapedApostrophe_MatchesUnescapedApostrophe()
    {
        // Arrange
        var original = CreateCharacterWithApostropheAspects();
        var updatedAspect = new DevelopmentAspect
        {
            Aspect = "Curiosity about Lily's anomaly",
            From = "Initial observation",
            Toward = "Obsessive pursuit",
            Pressure = "The anomaly defies all known laws",
            Resistance = "Fear of the unknown",
            Intensity = "Very high"
        };
        var updates = new Dictionary<string, object>
        {
            // Path uses escaped unicode \u0027 for apostrophe
            // Should match the existing item with unescaped apostrophe
            ["in_development[Curiosity about Lily\u0027s anomaly]"] = updatedAspect
        };

        // Act
        var result = original.PatchWith(updates);

        // Assert - Should update existing item, not add new one
        await Assert.That(result.InDevelopment!.Count).IsEqualTo(2);
        var curiosityAspect = result.InDevelopment!.Single(a => a.Aspect == "Curiosity about Lily's anomaly");
        await Assert.That(curiosityAspect.Toward).IsEqualTo("Obsessive pursuit");
        await Assert.That(curiosityAspect.Intensity).IsEqualTo("Very high");
    }

    [Test]
    public async Task PatchWith_ArrayItemWithEscapedApostrophe_NestedProperty_UpdatesCorrectly()
    {
        // Arrange
        var original = CreateCharacterWithApostropheAspects();
        var updates = new Dictionary<string, object>
        {
            // Path uses escaped unicode \u0027 for apostrophe with nested property access
            ["in_development[Curiosity about Lily\u0027s anomaly].intensity"] = "Critical"
        };

        // Act
        var result = original.PatchWith(updates);

        // Assert - Only intensity should be updated
        var curiosityAspect = result.InDevelopment!.Single(a => a.Aspect == "Curiosity about Lily's anomaly");
        await Assert.That(curiosityAspect.Intensity).IsEqualTo("Critical");
        await Assert.That(curiosityAspect.From).IsEqualTo("Initial observation"); // unchanged
        await Assert.That(curiosityAspect.Toward).IsEqualTo("Deep investigation"); // unchanged
    }

    [Test]
    public async Task PatchWith_ArrayItemWithMultipleEscapedApostrophes_MatchesCorrectly()
    {
        // Arrange
        var original = new CharacterWithApostropheAspects
        {
            Name = "Test Character",
            InDevelopment =
            [
                new DevelopmentAspect
                {
                    Aspect = "Sarah's trust in Marcus's plan",
                    From = "Doubtful",
                    Toward = "Committed",
                    Pressure = "Time running out",
                    Resistance = "Her own instincts",
                    Intensity = "High"
                }
            ]
        };
        var updates = new Dictionary<string, object>
        {
            // Path uses escaped unicode \u0027 for both apostrophes
            ["in_development[Sarah\u0027s trust in Marcus\u0027s plan].intensity"] = "Maximum"
        };

        // Act
        var result = original.PatchWith(updates);

        // Assert
        await Assert.That(result.InDevelopment!.Count).IsEqualTo(1);
        var aspect = result.InDevelopment!.Single();
        await Assert.That(aspect.Intensity).IsEqualTo("Maximum");
        await Assert.That(aspect.From).IsEqualTo("Doubtful"); // unchanged
    }

    [Test]
    public async Task PatchWith_ArrayItemWithMixedEscapedAndUnescapedApostrophes_MatchesCorrectly()
    {
        // Arrange
        var original = CreateCharacterWithApostropheAspects();
        var updates = new Dictionary<string, object>
        {
            // This tests that the identifier matching is normalized
            // Using the literal apostrophe character (which is what \u0027 represents)
            ["in_development[Trust in Marcus's judgment].intensity"] = "High"
        };

        // Act
        var result = original.PatchWith(updates);

        // Assert - Should match the existing item
        await Assert.That(result.InDevelopment!.Count).IsEqualTo(2);
        var trustAspect = result.InDevelopment!.Single(a => a.Aspect == "Trust in Marcus's judgment");
        await Assert.That(trustAspect.Intensity).IsEqualTo("High");
    }

    [Test]
    public async Task PatchWith_ArrayItemWithEscapedQuotes_MatchesUnescapedQuotes()
    {
        // Arrange
        var original = new CharacterWithApostropheAspects
        {
            Name = "Test Character",
            InDevelopment =
            [
                new DevelopmentAspect
                {
                    Aspect = "Understanding the \"hidden truth\"",
                    From = "Confused",
                    Toward = "Enlightened",
                    Pressure = "Mounting evidence",
                    Resistance = "Disbelief",
                    Intensity = "Medium"
                }
            ]
        };
        var updates = new Dictionary<string, object>
        {
            // Path uses escaped unicode \u0022 for double quotes
            ["in_development[Understanding the \u0022hidden truth\u0022].intensity"] = "Very high"
        };

        // Act
        var result = original.PatchWith(updates);

        // Assert
        await Assert.That(result.InDevelopment!.Count).IsEqualTo(1);
        var aspect = result.InDevelopment!.Single();
        await Assert.That(aspect.Intensity).IsEqualTo("Very high");
    }

    #endregion

    #region Quoted Identifier Tests

    [Test]
    public async Task PatchWith_ArrayItemWithQuotedIdentifier_MatchesUnquotedData()
    {
        // Arrange - This is the real-world case where AI returns quoted identifiers
        // Data has: "aspect": "Curiosity about Lily's anomaly"
        // AI returns path: in_development["Curiosity about Lily's anomaly"]
        var original = CreateCharacterWithApostropheAspects();
        var updatedAspect = new DevelopmentAspect
        {
            Aspect = "Curiosity about Lily's anomaly",
            From = "Initial observation",
            Toward = "Obsessive pursuit",
            Pressure = "The anomaly defies all known laws",
            Resistance = "Fear of the unknown",
            Intensity = "Very high"
        };
        var updates = new Dictionary<string, object>
        {
            // Path has quotes around the identifier (as AI sometimes returns)
            ["in_development[\"Curiosity about Lily's anomaly\"]"] = updatedAspect
        };

        // Act
        var result = original.PatchWith(updates);

        // Assert - Should update existing item, not add new one
        await Assert.That(result.InDevelopment!.Count).IsEqualTo(2);
        var curiosityAspect = result.InDevelopment!.Single(a => a.Aspect == "Curiosity about Lily's anomaly");
        await Assert.That(curiosityAspect.Toward).IsEqualTo("Obsessive pursuit");
        await Assert.That(curiosityAspect.Intensity).IsEqualTo("Very high");
    }

    [Test]
    public async Task PatchWith_ArrayItemWithQuotedIdentifier_NestedProperty_UpdatesCorrectly()
    {
        // Arrange
        var original = CreateCharacterWithApostropheAspects();
        var updates = new Dictionary<string, object>
        {
            // Path has quotes around the identifier with nested property access
            ["in_development[\"Curiosity about Lily's anomaly\"].intensity"] = "Critical"
        };

        // Act
        var result = original.PatchWith(updates);

        // Assert - Only intensity should be updated
        var curiosityAspect = result.InDevelopment!.Single(a => a.Aspect == "Curiosity about Lily's anomaly");
        await Assert.That(curiosityAspect.Intensity).IsEqualTo("Critical");
        await Assert.That(curiosityAspect.From).IsEqualTo("Initial observation"); // unchanged
    }

    [Test]
    public async Task PatchWith_ArrayItemWithSingleQuotedIdentifier_MatchesUnquotedData()
    {
        // Arrange
        var original = CreateCharacterWithApostropheAspects();
        var updates = new Dictionary<string, object>
        {
            // Path has single quotes around the identifier
            ["in_development['Trust in Marcus's judgment'].intensity"] = "High"
        };

        // Act
        var result = original.PatchWith(updates);

        // Assert - Should match the existing item
        await Assert.That(result.InDevelopment!.Count).IsEqualTo(2);
        var trustAspect = result.InDevelopment!.Single(a => a.Aspect == "Trust in Marcus's judgment");
        await Assert.That(trustAspect.Intensity).IsEqualTo("High");
    }

    [Test]
    public async Task PatchWith_ArrayItemNotFound_WithQuotedIdentifier_AddsNewItem()
    {
        // Arrange
        var original = CreateCharacterWithApostropheAspects();
        var newAspect = new DevelopmentAspect
        {
            Aspect = "New development aspect",
            From = "Starting point",
            Toward = "Goal",
            Pressure = "External factors",
            Resistance = "Internal factors",
            Intensity = "Medium"
        };
        var updates = new Dictionary<string, object>
        {
            // Path has quotes around a non-existing identifier
            ["in_development[\"New development aspect\"]"] = newAspect
        };

        // Act
        var result = original.PatchWith(updates);

        // Assert - New aspect should be added
        await Assert.That(result.InDevelopment!.Count).IsEqualTo(3);
        var addedAspect = result.InDevelopment!.SingleOrDefault(a => a.Aspect == "New development aspect");
        await Assert.That(addedAspect).IsNotNull();
        await Assert.That(addedAspect!.From).IsEqualTo("Starting point");
    }

    [Test]
    public async Task PatchWith_NestedPathWithQuotedArrayAccess_UpdatesCorrectly()
    {
        // Arrange
        var original = CreateCharacterWithMagic();
        var updates = new Dictionary<string, object>
        {
            // Path has quotes around the identifier in nested array access
            ["MagicAndAbilities.InstinctiveAbilities[\"Fire Breath\"].Power"] = 300
        };

        // Act
        var result = original.PatchWith(updates);

        // Assert
        var fireBreath = result.MagicAndAbilities!.InstinctiveAbilities!.Single(a => a.AbilityName == "Fire Breath");
        await Assert.That(fireBreath.Power).IsEqualTo(300);
        await Assert.That(fireBreath.CooldownSeconds).IsEqualTo(30); // unchanged
    }

    #endregion
}