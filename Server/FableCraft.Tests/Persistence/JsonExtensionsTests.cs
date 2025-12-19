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
}
