# Data Model (Infrastructure, Persistence, Entities)

The data model is built using Entity Framework Core entities. The root of the hierarchy is the `Adventure`, which contains multiple `Scenes`, `Characters`, and other related data.

### Entities Class Diagram

```mermaid
classDiagram
    class Adventure {
        +Guid Id
        +string Name
        +string FirstSceneGuidance
        +DateTime AdventureStartTime
        +ProcessingStatus ProcessingStatus
        +DateTimeOffset CreatedAt
        +DateTimeOffset? LastPlayedAt
        +string? AuthorNotes
        +MainCharacter MainCharacter
        +List~Character~ Characters
        +TrackerStructure TrackerStructure
        +List~LorebookEntry~ Lorebook
        +List~Scene~ Scenes
    }

    class Scene {
        +Guid Id
        +Guid AdventureId
        +int SequenceNumber
        +string? AdventureSummary
        +string NarrativeText
        +CommitStatus CommitStatus
        +Metadata Metadata
        +DateTime CreatedAt
        +List~CharacterState~ CharacterStates
        +List~MainCharacterAction~ CharacterActions
        +List~LorebookEntry~ Lorebooks
    }

    class MainCharacter {
        +Guid Id
        +Guid AdventureId
        +string Name
        +string Description
    }

    class Character {
        +Guid Id
        +Guid AdventureId
        +string Name
        +string Description
        +List~CharacterState~ CharacterStates
    }

    class CharacterState {
        +Guid Id
        +Guid CharacterId
        +Character Character
        +Guid SceneId
        +Scene Scene
        +CharacterStats CharacterStats
        +CharacterTracker Tracker
        +int SequenceNumber
        +string Description
    }

    class MainCharacterAction {
        +Guid Id
        +string ActionDescription
        +bool Selected
    }

    class LorebookEntry {
        +Guid Id
        +Guid AdventureId
        +Guid? SceneId
        +string Description
        +int Priority
        +string Content
        +string Category
        +ContentType ContentType
    }

    class Metadata {
        +NarrativeDirectorOutput NarrativeMetadata
        +Tracker Tracker
    }

    class Tracker {
        +StoryTracker Story
        +string[] CharactersPresent
        +CharacterTracker? MainCharacter
        +CharacterTracker[]? Characters
    }

    Adventure "1" -- "1" MainCharacter : has
    Adventure "1" -- "*" Character : contains
    Adventure "1" -- "*" Scene : contains
    Adventure "1" -- "*" LorebookEntry : contains
    Scene "1" -- "*" CharacterState : contains
    Scene "1" -- "*" MainCharacterAction : contains
    Scene "1" -- "*" LorebookEntry : contains
    Character "1" -- "*" CharacterState : tracks history
    Scene "1" -- "1" Metadata : has
    Metadata "1" -- "1" Tracker : contains
```