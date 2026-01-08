using System.Text.Json;
using System.Text.Json.Serialization;

using FableCraft.Infrastructure.Persistence;

namespace FableCraft.Tests.Persistence;

public class JsonExtensionsTests
{
    #region Test Models

    private sealed class CharacterStats
    {
        [JsonPropertyName("character_identity")]
        public required CharacterIdentity CharacterIdentity { get; set; }

        [JsonPropertyName("emotional_landscape")]
        public EmotionalLandscape? EmotionalLandscape { get; set; }

        [JsonPropertyName("goals_and_motivations")]
        public GoalsAndMotivations? GoalsAndMotivations { get; set; }

        [JsonPropertyName("behavioral_tendencies")]
        public BehavioralTendencies? BehavioralTendencies { get; set; }

        [JsonPropertyName("character_arc")]
        public CharacterArc? CharacterArc { get; set; }

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

    private sealed class EmotionalLandscape
    {
        [JsonPropertyName("current_state")]
        public EmotionalState? CurrentState { get; set; }

        [JsonPropertyName("baseline")]
        public string? Baseline { get; set; }
    }

    private sealed class EmotionalState
    {
        [JsonPropertyName("primary_emotion")]
        public string? PrimaryEmotion { get; set; }

        [JsonPropertyName("secondary_emotions")]
        public List<string>? SecondaryEmotions { get; set; }

        [JsonPropertyName("intensity")]
        public double Intensity { get; set; }

        [JsonPropertyName("cause")]
        public string? Cause { get; set; }
    }

    private sealed class GoalsAndMotivations
    {
        [JsonPropertyName("primary_goal")]
        public string? PrimaryGoal { get; set; }

        [JsonPropertyName("immediate_intention")]
        public string? ImmediateIntention { get; set; }
    }

    private sealed class BehavioralTendencies
    {
        [JsonPropertyName("decision_style")]
        public string? DecisionStyle { get; set; }

        [JsonPropertyName("current_plan")]
        public CurrentPlan? CurrentPlan { get; set; }
    }

    private sealed class CurrentPlan
    {
        [JsonPropertyName("intention")]
        public string? Intention { get; set; }

        [JsonPropertyName("steps")]
        public List<string>? Steps { get; set; }
    }

    private sealed class CharacterArc
    {
        [JsonPropertyName("current_stage")]
        public string? CurrentStage { get; set; }

        [JsonPropertyName("progress_percentage")]
        public int ProgressPercentage { get; set; }
    }

    private static CharacterStats CreateTestCharacter() => new()
    {
        CharacterIdentity = new CharacterIdentity
        {
            Name = "Viktor Volkov",
            Role = "Antagonist"
        },
        EmotionalLandscape = new EmotionalLandscape
        {
            CurrentState = new EmotionalState
            {
                PrimaryEmotion = "calm",
                SecondaryEmotions = ["calculating"],
                Intensity = 0.3,
                Cause = "routine business"
            },
            Baseline = "stoic"
        },
        GoalsAndMotivations = new GoalsAndMotivations
        {
            PrimaryGoal = "Expand territory",
            ImmediateIntention = "Monitor competitors"
        },
        BehavioralTendencies = new BehavioralTendencies
        {
            DecisionStyle = "methodical",
            CurrentPlan = new CurrentPlan
            {
                Intention = "gather information",
                Steps = ["observe", "report", "plan"]
            }
        },
        CharacterArc = new CharacterArc
        {
            CurrentStage = "rising_action",
            ProgressPercentage = 25
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
        await Assert.That(result.EmotionalLandscape!.CurrentState!.PrimaryEmotion).IsEqualTo("calm");
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
        await Assert.That(result.EmotionalLandscape!.Baseline).IsEqualTo("stoic");
    }

    [Test]
    public async Task PatchWith_NestedPath_ReplacesAtPath()
    {
        // Arrange
        var original = CreateTestCharacter();
        var newCurrentState = new EmotionalState
        {
            PrimaryEmotion = "anxious",
            SecondaryEmotions = ["calculating", "defensive"],
            Intensity = 0.7,
            Cause = "protagonist getting too close"
        };
        var updates = new Dictionary<string, object>
        {
            ["emotional_landscape.current_state"] = newCurrentState
        };

        // Act
        var result = original.PatchWith(updates);

        // Assert
        await Assert.That(result.EmotionalLandscape!.CurrentState!.PrimaryEmotion).IsEqualTo("anxious");
        await Assert.That(result.EmotionalLandscape.CurrentState.Intensity).IsEqualTo(0.7);
        await Assert.That(result.EmotionalLandscape.CurrentState.SecondaryEmotions).Contains("defensive");
        // Sibling properties should remain unchanged
        await Assert.That(result.EmotionalLandscape.Baseline).IsEqualTo("stoic");
    }

    [Test]
    public async Task PatchWith_DeepNestedPath_ReplacesAtPath()
    {
        // Arrange
        var original = CreateTestCharacter();
        var newPlan = new CurrentPlan
        {
            Intention = "assess damage",
            Steps = ["talk to dockmaster", "check records", "prepare alibi"]
        };
        var updates = new Dictionary<string, object>
        {
            ["behavioral_tendencies.current_plan"] = newPlan
        };

        // Act
        var result = original.PatchWith(updates);

        // Assert
        await Assert.That(result.BehavioralTendencies!.CurrentPlan!.Intention).IsEqualTo("assess damage");
        await Assert.That(result.BehavioralTendencies.CurrentPlan.Steps).Contains("prepare alibi");
        // Sibling property should remain unchanged
        await Assert.That(result.BehavioralTendencies.DecisionStyle).IsEqualTo("methodical");
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
            ["emotional_landscape.current_state"] = new EmotionalState
            {
                PrimaryEmotion = "anxious",
                SecondaryEmotions = ["calculating", "defensive"],
                Intensity = 0.7,
                Cause = "protagonist getting too close"
            },
            ["goals_and_motivations.immediate_intention"] = "gather intelligence before next confrontation",
            ["behavioral_tendencies.current_plan"] = new CurrentPlan
            {
                Intention = "assess damage",
                Steps = ["talk to dockmaster", "check records", "prepare alibi"]
            }
        };

        // Act
        var result = original.PatchWith(updates);

        // Assert - All updates applied
        await Assert.That(result.EmotionalLandscape!.CurrentState!.PrimaryEmotion).IsEqualTo("anxious");
        await Assert.That(result.GoalsAndMotivations!.ImmediateIntention).IsEqualTo("gather intelligence before next confrontation");
        await Assert.That(result.BehavioralTendencies!.CurrentPlan!.Intention).IsEqualTo("assess damage");

        // Assert - Unchanged properties preserved
        await Assert.That(result.CharacterIdentity.Name).IsEqualTo("Viktor Volkov");
        await Assert.That(result.EmotionalLandscape.Baseline).IsEqualTo("stoic");
        await Assert.That(result.GoalsAndMotivations.PrimaryGoal).IsEqualTo("Expand territory");
        await Assert.That(result.BehavioralTendencies.DecisionStyle).IsEqualTo("methodical");
    }

    [Test]
    public async Task PatchWith_UpdateSiblingPaths_BothApplied()
    {
        // Arrange
        var original = CreateTestCharacter();
        var updates = new Dictionary<string, object>
        {
            ["emotional_landscape.current_state"] = new EmotionalState
            {
                PrimaryEmotion = "angry",
                SecondaryEmotions = ["frustrated"],
                Intensity = 0.8,
                Cause = "betrayal discovered"
            },
            ["emotional_landscape.baseline"] = "volatile"
        };

        // Act
        var result = original.PatchWith(updates);

        // Assert - Both emotional_landscape children updated
        await Assert.That(result.EmotionalLandscape!.CurrentState!.PrimaryEmotion).IsEqualTo("angry");
        await Assert.That(result.EmotionalLandscape.Baseline).IsEqualTo("volatile");
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
            ["goals_and_motivations.immediate_intention"] = "flee the scene"
        };

        // Act
        var result = original.PatchWith(updates);

        // Assert
        await Assert.That(result.GoalsAndMotivations!.ImmediateIntention).IsEqualTo("flee the scene");
        await Assert.That(result.GoalsAndMotivations.PrimaryGoal).IsEqualTo("Expand territory");
    }

    [Test]
    public async Task PatchWith_IntegerValue_ReplacesInteger()
    {
        // Arrange
        var original = CreateTestCharacter();
        var updates = new Dictionary<string, object>
        {
            ["character_arc.progress_percentage"] = 75
        };

        // Act
        var result = original.PatchWith(updates);

        // Assert
        await Assert.That(result.CharacterArc!.ProgressPercentage).IsEqualTo(75);
        await Assert.That(result.CharacterArc.CurrentStage).IsEqualTo("rising_action");
    }

    [Test]
    public async Task PatchWith_DoubleValue_ReplacesDouble()
    {
        // Arrange
        var original = CreateTestCharacter();
        var newState = new EmotionalState
        {
            PrimaryEmotion = "calm",
            SecondaryEmotions = [],
            Intensity = 0.95,
            Cause = "meditation"
        };
        var updates = new Dictionary<string, object>
        {
            ["emotional_landscape.current_state"] = newState
        };

        // Act
        var result = original.PatchWith(updates);

        // Assert
        await Assert.That(result.EmotionalLandscape!.CurrentState!.Intensity).IsEqualTo(0.95);
    }

    #endregion

    #region Array Value Tests

    [Test]
    public async Task PatchWith_ArrayValue_ReplacesArray()
    {
        // Arrange
        var original = CreateTestCharacter();
        var newPlan = new CurrentPlan
        {
            Intention = "escape",
            Steps = ["disable alarm", "exit through back", "meet contact", "leave city"]
        };
        var updates = new Dictionary<string, object>
        {
            ["behavioral_tendencies.current_plan"] = newPlan
        };

        // Act
        var result = original.PatchWith(updates);

        // Assert
        await Assert.That(result.BehavioralTendencies!.CurrentPlan!.Steps!.Count).IsEqualTo(4);
        await Assert.That(result.BehavioralTendencies.CurrentPlan.Steps).Contains("leave city");
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
            ["behavioral_tendencies.current_plan"] = null!
        };

        // Act
        var result = original.PatchWith(updates);

        // Assert
        await Assert.That(result.BehavioralTendencies!.CurrentPlan).IsNull();
        await Assert.That(result.BehavioralTendencies.DecisionStyle).IsEqualTo("methodical");
    }

    [Test]
    public async Task PatchWith_PreservesUnrelatedNestedStructure()
    {
        // Arrange
        var original = CreateTestCharacter();
        var updates = new Dictionary<string, object>
        {
            ["character_arc.current_stage"] = "climax"
        };

        // Act
        var result = original.PatchWith(updates);

        // Assert - Updated property changed
        await Assert.That(result.CharacterArc!.CurrentStage).IsEqualTo("climax");

        // Assert - All other nested structures preserved completely
        await Assert.That(result.CharacterIdentity.Name).IsEqualTo("Viktor Volkov");
        await Assert.That(result.EmotionalLandscape!.CurrentState!.PrimaryEmotion).IsEqualTo("calm");
        await Assert.That(result.EmotionalLandscape.CurrentState.SecondaryEmotions).Contains("calculating");
        await Assert.That(result.GoalsAndMotivations!.PrimaryGoal).IsEqualTo("Expand territory");
        await Assert.That(result.BehavioralTendencies!.CurrentPlan!.Steps).Contains("observe");
    }

    [Test]
    public async Task PatchWith_JsonElementValue_WorksCorrectly()
    {
        // Arrange - Simulates receiving updates from JSON deserialization
        var original = CreateTestCharacter();
        var updateJson = """
        {
            "primary_emotion": "terrified",
            "secondary_emotions": ["panicked"],
            "intensity": 0.9,
            "cause": "ambush"
        }
        """;
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(updateJson);
        var updates = new Dictionary<string, object>
        {
            ["emotional_landscape.current_state"] = jsonElement
        };

        // Act
        var result = original.PatchWith(updates);

        // Assert
        await Assert.That(result.EmotionalLandscape!.CurrentState!.PrimaryEmotion).IsEqualTo("terrified");
        await Assert.That(result.EmotionalLandscape.CurrentState.Intensity).IsEqualTo(0.9);
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
            ["emotional_landscape.current_state"] = new EmotionalState
            {
                PrimaryEmotion = "anxious",
                SecondaryEmotions = ["calculating", "defensive"],
                Intensity = 0.7,
                Cause = "protagonist getting too close"
            },
            ["goals_and_motivations.immediate_intention"] = "gather intelligence before next confrontation",
            ["behavioral_tendencies.current_plan"] = new CurrentPlan
            {
                Intention = "assess damage",
                Steps = ["talk to dockmaster", "check records", "prepare alibi"]
            }
        };

        // Act
        var result = original.PatchWith(updates);

        // Assert - Volatile state updated as per ADR
        await Assert.That(result.EmotionalLandscape!.CurrentState!.PrimaryEmotion).IsEqualTo("anxious");
        await Assert.That(result.EmotionalLandscape.CurrentState.SecondaryEmotions).Contains("defensive");
        await Assert.That(result.EmotionalLandscape.CurrentState.Intensity).IsEqualTo(0.7);
        await Assert.That(result.EmotionalLandscape.CurrentState.Cause).IsEqualTo("protagonist getting too close");

        await Assert.That(result.GoalsAndMotivations!.ImmediateIntention)
            .IsEqualTo("gather intelligence before next confrontation");

        await Assert.That(result.BehavioralTendencies!.CurrentPlan!.Intention).IsEqualTo("assess damage");
        await Assert.That(result.BehavioralTendencies.CurrentPlan.Steps).Contains("prepare alibi");

        // Assert - Core profile (stable) unchanged as per ADR
        await Assert.That(result.CharacterIdentity.Name).IsEqualTo("Viktor Volkov");
        await Assert.That(result.CharacterIdentity.Role).IsEqualTo("Antagonist");

        // Assert - Sibling properties preserved
        await Assert.That(result.EmotionalLandscape.Baseline).IsEqualTo("stoic");
        await Assert.That(result.GoalsAndMotivations.PrimaryGoal).IsEqualTo("Expand territory");
        await Assert.That(result.BehavioralTendencies.DecisionStyle).IsEqualTo("methodical");
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

    private static CharacterWithSkills CreateCharacterWithSkills() => new()
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

    private static CharacterWithMagic CreateCharacterWithMagic() => new()
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
}
