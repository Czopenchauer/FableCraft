using FableCraft.Infrastructure.Persistence.Entities.Adventure;

namespace FableCraft.Tests.Integration.SceneRegeneration;

internal static class TestData
{
    public static Adventure CreateAdventure(
        Guid? id = null,
        string? name = null,
        TrackerStructure? trackerStructure = null)
    {
        var adventureId = id ?? Guid.NewGuid();
        return new Adventure
        {
            Id = adventureId,
            Name = name ?? $"Test Adventure {Guid.NewGuid():N}",
            FirstSceneGuidance = "Start in a tavern.",
            AdventureStartTime = "12:00 01-01-845",
            PromptPath = "Default",
            MainCharacter = new MainCharacter
            {
                Id = Guid.NewGuid(),
                AdventureId = adventureId,
                Name = "Hero",
                Description = "A brave adventurer."
            },
            TrackerStructure = trackerStructure ?? CreateTrackerStructure(),
            Lorebook = [],
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public static Scene CreateScene(
        Guid adventureId,
        int sequenceNumber,
        Guid? id = null,
        string? narrativeText = null,
        string? selectedAction = null)
    {
        var sceneId = id ?? Guid.NewGuid();
        var scene = new Scene
        {
            Id = sceneId,
            AdventureId = adventureId,
            SequenceNumber = sequenceNumber,
            NarrativeText = narrativeText ?? $"Scene {sequenceNumber} narrative text.",
            EnrichmentStatus = EnrichmentStatus.Enriched,
            Metadata = new Metadata
            {
                Tracker = new Infrastructure.Persistence.Entities.Adventure.Tracker
                {
                    Scene = CreateSceneTracker(sequenceNumber),
                    MainCharacter = new MainCharacterState
                    {
                        MainCharacterDescription = "The hero looks determined.",
                        MainCharacter = CreateMainCharacterTracker()
                    }
                }
            },
            CreatedAt = DateTimeOffset.UtcNow
        };

        if (selectedAction != null)
        {
            scene.CharacterActions.Add(new MainCharacterAction
            {
                Id = Guid.NewGuid(),
                SceneId = sceneId,
                ActionDescription = selectedAction,
                Selected = true
            });
        }

        return scene;
    }

    public static Character CreateCharacter(
        Guid adventureId,
        Scene introductionScene,
        string name,
        Guid? id = null,
        string? description = null,
        bool isNew = false,
        CharacterImportance? importance = null)
    {
        return new Character
        {
            Id = id ?? Guid.NewGuid(),
            AdventureId = adventureId,
            IntroductionScene = introductionScene.Id,
            Scene = introductionScene,
            Name = name,
            Description = description ?? $"{name} is a mysterious character.",
            Importance = importance ?? CharacterImportance.Significant,
            Version = isNew ? 0 : 1
        };
    }

    public static CharacterState CreateCharacterState(
        Guid characterId,
        Scene scene,
        int sequenceNumber,
        string characterName,
        Guid? id = null)
    {
        return new CharacterState
        {
            Id = id ?? Guid.NewGuid(),
            CharacterId = characterId,
            SceneId = scene.Id,
            Scene = scene,
            SequenceNumber = sequenceNumber,
            CharacterStats = new CharacterStats
            {
                Name = characterName,
                Motivations = new { primary = "Survive" },
                Routine = new { morning = "Wake up early" }
            },
            Tracker = CreateCharacterTracker(characterName)
        };
    }

    public static CharacterMemory CreateCharacterMemory(
        Guid characterId,
        Guid sceneId,
        string summary,
        Guid? id = null,
        double salience = 5.0)
    {
        return new CharacterMemory
        {
            Id = id ?? Guid.NewGuid(),
            CharacterId = characterId,
            SceneId = sceneId,
            Summary = summary,
            Salience = salience,
            SceneTracker = CreateSceneTracker(1)
        };
    }

    public static CharacterRelationship CreateCharacterRelationship(
        Guid characterId,
        Guid sceneId,
        int sequenceNumber,
        string targetCharacterName,
        string dynamic,
        Guid? id = null)
    {
        return new CharacterRelationship
        {
            Id = id ?? Guid.NewGuid(),
            CharacterId = characterId,
            SceneId = sceneId,
            SequenceNumber = sequenceNumber,
            TargetCharacterName = targetCharacterName,
            Dynamic = dynamic,
            UpdateTime = $"{sequenceNumber}2:00",
            Data = new Dictionary<string, object>
            {
                ["foundation"] = "Just met",
                ["trust"] = "Neutral"
            }
        };
    }

    public static CharacterSceneRewrite CreateCharacterSceneRewrite(
        Guid characterId,
        Guid sceneId,
        int sequenceNumber,
        string content,
        Guid? id = null)
    {
        return new CharacterSceneRewrite
        {
            Id = id ?? Guid.NewGuid(),
            CharacterId = characterId,
            SceneId = sceneId,
            SequenceNumber = sequenceNumber,
            Content = content,
            SceneTracker = CreateSceneTracker(sequenceNumber)
        };
    }

    public static LorebookEntry CreateLorebookEntry(
        Guid adventureId,
        Guid sceneId,
        string category,
        string title,
        string content,
        Guid? id = null)
    {
        return new LorebookEntry
        {
            Id = id ?? Guid.NewGuid(),
            AdventureId = adventureId,
            SceneId = sceneId,
            Title = title,
            Description = $"Description for {title}",
            Category = category,
            Content = content,
            Priority = 1
        };
    }

    public static TrackerStructure CreateTrackerStructure()
    {
        return new TrackerStructure
        {
            Story =
            [
                new FieldDefinition { Name = "time", Type = FieldType.String, Prompt = "Current time" },
                new FieldDefinition { Name = "location", Type = FieldType.String, Prompt = "Current location" }
            ],
            MainCharacter =
            [
                new FieldDefinition { Name = "appearance", Type = FieldType.String, Prompt = "Appearance" }
            ],
            Characters =
            [
                new FieldDefinition { Name = "location", Type = FieldType.String, Prompt = "Location" }
            ]
        };
    }

    public static SceneTracker CreateSceneTracker(int sequenceNumber = 1)
    {
        return new SceneTracker
        {
            Time = $"1{sequenceNumber}:00",
            Location = "Tavern",
            Weather = "Clear",
            CharactersPresent = ["Hero"],
            AdditionalProperties = new Dictionary<string, object>()
        };
    }

    public static CharacterTracker CreateCharacterTracker(string name)
    {
        return new CharacterTracker
        {
            Name = name,
            Location = "Tavern",
            Appearance = "Wearing simple clothes",
            GeneralBuild = "Average",
            AdditionalProperties = new Dictionary<string, object>()
        };
    }

    public static MainCharacterTracker CreateMainCharacterTracker()
    {
        return new MainCharacterTracker
        {
            Name = "Hero",
            Appearance = "Tall and strong",
            GeneralBuild = "Athletic",
            AdditionalProperties = new Dictionary<string, object>()
        };
    }
}
