using FableCraft.Application.NarrativeEngine.Interfaces;
using FableCraft.Application.NarrativeEngine.Models;

namespace FableCraft.Application.NarrativeEngine;

/// <summary>
/// Concrete implementation of shared context for agents
/// </summary>
public class AgentContext : IAgentContext
{
    public List<SceneContext> RecentScenes { get; set; } = new();
    public List<StoryBeat> StoryBeats { get; set; } = new();
    public string CurrentArcPosition { get; set; } = "Beginning";
    public PacingHistory PacingHistory { get; set; } = new()
    {
        RecentBeatTypes = new List<string>(),
        CurrentTension = 0.5,
        ScenesSinceLastClimactic = 0
    };
    public Dictionary<string, string> WorldConsistencyGuidelines { get; set; } = new();
    public List<string> GenreConventions { get; set; } = new();

    /// <summary>
    /// Create a default context for testing
    /// </summary>
    public static AgentContext CreateDefault()
    {
        return new AgentContext
        {
            RecentScenes = new List<SceneContext>(),
            StoryBeats = new List<StoryBeat>
            {
                new StoryBeat
                {
                    BeatId = "long_1",
                    Tier = "Long",
                    Description = "Establish the world and introduce main conflict",
                    Objectives = new List<string> { "Introduce protagonist", "Present inciting incident" },
                    Progress = 0.3,
                    IsCompleted = false,
                    Dependencies = new List<string>()
                },
                new StoryBeat
                {
                    BeatId = "medium_1",
                    Tier = "Medium",
                    Description = "First challenge and character development",
                    Objectives = new List<string> { "Character faces obstacle", "Reveal character trait" },
                    Progress = 0.0,
                    IsCompleted = false,
                    Dependencies = new List<string> { "long_1" }
                }
            },
            CurrentArcPosition = "Act 1: Setup",
            PacingHistory = new PacingHistory
            {
                RecentBeatTypes = new List<string> { "Setup", "Introduction", "Dialogue" },
                CurrentTension = 0.4,
                ScenesSinceLastClimactic = 3
            },
            WorldConsistencyGuidelines = new Dictionary<string, string>
            {
                { "Magic System", "Magic requires personal sacrifice - blood, memories, or years of life" },
                { "Technology Level", "Medieval with minor magical enhancements" },
                { "Geography", "Island archipelago with distinct regional cultures" },
                { "Power Structure", "Feudal system with mage guilds holding significant influence" }
            },
            GenreConventions = new List<string>
            {
                "Fantasy with dark undertones",
                "Morally gray characters preferred over purely good/evil",
                "Consequences matter - actions have lasting impact",
                "Show don't tell - reveal through action and dialogue",
                "Avoid deus ex machina resolutions"
            }
        };
    }

    /// <summary>
    /// Update context after a scene is generated
    /// </summary>
    public void UpdateAfterScene(SceneOutput scene)
    {
        // Add new scene to recent scenes
        var newSceneContext = new SceneContext
        {
            SceneId = scene.SceneId,
            Summary = ExtractSummary(scene.Prose),
            KeyEvents = scene.ObjectivesAdvanced,
            CharacterDevelopments = ExtractCharacterDevelopments(scene.CharacterUpdates),
            LocationChanges = ExtractLocations(scene.NewEntities),
            Timestamp = DateTime.UtcNow,
            NarrativeArcPosition = scene.NarrativeArcPosition
        };

        RecentScenes.Add(newSceneContext);

        // Keep only last 20 scenes
        if (RecentScenes.Count > 20)
        {
            RecentScenes.RemoveAt(0);
        }

        // Update pacing history
        if (scene.Metadata.TryGetValue("Beat Type", out var beatTypeObj))
        {
            var beatType = beatTypeObj.ToString() ?? "Unknown";
            PacingHistory.RecentBeatTypes.Add(beatType);
            if (PacingHistory.RecentBeatTypes.Count > 10)
            {
                PacingHistory.RecentBeatTypes.RemoveAt(0);
            }

            if (beatType.Contains("Climactic", StringComparison.OrdinalIgnoreCase))
            {
                PacingHistory.ScenesSinceLastClimactic = 0;
                PacingHistory.CurrentTension = 0.9;
            }
            else
            {
                PacingHistory.ScenesSinceLastClimactic++;
                PacingHistory.CurrentTension = Math.Max(0.2, PacingHistory.CurrentTension - 0.1);
            }
        }

        // Update story beat progress
        foreach (var objective in scene.ObjectivesAdvanced)
        {
            var beat = StoryBeats.FirstOrDefault(b =>
                b.Objectives.Any(o => o.Contains(objective, StringComparison.OrdinalIgnoreCase)));

            if (beat != null)
            {
                beat.Progress = Math.Min(1.0, beat.Progress + 0.2);
                if (beat.Progress >= 1.0)
                {
                    beat.IsCompleted = true;
                }
            }
        }

        // Update arc position
        CurrentArcPosition = scene.NarrativeArcPosition;
    }

    private string ExtractSummary(string prose)
    {
        // Take first 200 characters as summary
        var summary = prose.Length > 200 ? prose.Substring(0, 200) + "..." : prose;
        return summary.Replace("\n", " ").Trim();
    }

    private Dictionary<string, string> ExtractCharacterDevelopments(Dictionary<string, object> characterUpdates)
    {
        var developments = new Dictionary<string, string>();

        foreach (var update in characterUpdates)
        {
            if (update.Key.EndsWith("_EmotionalState"))
            {
                var characterName = update.Key.Replace("_EmotionalState", "");
                developments[characterName] = $"Emotional state: {update.Value}";
            }
        }

        return developments;
    }

    private List<string> ExtractLocations(List<WorldEntity> newEntities)
    {
        return newEntities
            .Where(e => e.EntityType == "Location" || e is Location)
            .Select(e => e.Name)
            .ToList();
    }
}
