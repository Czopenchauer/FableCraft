using FableCraft.Application.NarrativeEngine;
using FableCraft.Tests.Integration.SceneRegeneration.Fixtures;

using Microsoft.EntityFrameworkCore;

namespace FableCraft.Tests.Integration.SceneRegeneration;

[ClassDataSource<PostgresContainerFixture>(Shared = SharedType.PerTestSession)]
public class BuildRegenerationContextTests(PostgresContainerFixture fixture)
{
    private IGenerationContextBuilder CreateBuilder()
    {
        var db = fixture.CreateDbContext();
        return new GenerationContextBuilder(db);
    }

    [Test]
    public async Task SingleScene_ReturnsEmptyPreviousScenes()
    {
        await using var db = fixture.CreateDbContext();
        var builder = CreateBuilder();

        var adventure = TestData.CreateAdventure();
        var scene = TestData.CreateScene(adventure.Id, 1, selectedAction: "Look around");
        adventure.Scenes.Add(scene);

        db.Adventures.Add(adventure);
        await db.SaveChangesAsync();

        var loadedScene = await LoadSceneWithIncludes(db, scene.Id);

        var context = await builder.BuildRegenerationContextAsync(adventure.Id, loadedScene, CancellationToken.None);

        await Assert.That(context.SceneContext).IsEmpty();
        await Assert.That(context.AdventureId).IsEqualTo(adventure.Id);
        await Assert.That(context.NewSceneId).IsEqualTo(scene.Id);
    }

    [Test]
    public async Task MultipleScenes_IncludesUpTo20PreviousScenes()
    {
        await using var db = fixture.CreateDbContext();
        var builder = CreateBuilder();

        var adventure = TestData.CreateAdventure();
        for (var i = 1; i <= 25; i++)
        {
            adventure.Scenes.Add(TestData.CreateScene(adventure.Id, i, selectedAction: $"Action {i}"));
        }

        db.Adventures.Add(adventure);
        await db.SaveChangesAsync();

        var currentScene = await db.Scenes
            .Include(s => s.CharacterActions)
            .Include(s => s.CharacterStates)
            .Include(s => s.Lorebooks)
            .FirstAsync(s => s.AdventureId == adventure.Id && s.SequenceNumber == 25);

        var context = await builder.BuildRegenerationContextAsync(adventure.Id, currentScene, CancellationToken.None);

        await Assert.That(context.SceneContext).Count().IsEqualTo(20);
        await Assert.That(context.SceneContext.Max(s => s.SequenceNumber)).IsEqualTo(24);
        await Assert.That(context.SceneContext.Min(s => s.SequenceNumber)).IsEqualTo(5);
    }

    [Test]
    public async Task CharacterUpdates_ContainsStateFromCurrentScene()
    {
        await using var db = fixture.CreateDbContext();
        var builder = CreateBuilder();

        var adventure = TestData.CreateAdventure();
        var scene1 = TestData.CreateScene(adventure.Id, 1, selectedAction: "Action 1");
        var scene2 = TestData.CreateScene(adventure.Id, 2, selectedAction: "Action 2");
        adventure.Scenes.Add(scene1);
        adventure.Scenes.Add(scene2);

        var character = TestData.CreateCharacter(adventure.Id, scene1, "Elena", description: "A brave warrior");
        var state1 = TestData.CreateCharacterState(character.Id, scene1, 1, "Elena - scene 1 state");
        var state2 = TestData.CreateCharacterState(character.Id, scene2, 2, "Elena - scene 2 state");
        character.CharacterStates.Add(state1);
        character.CharacterStates.Add(state2);
        scene2.CharacterStates.Add(state2);
        adventure.Characters.Add(character);

        db.Adventures.Add(adventure);
        await db.SaveChangesAsync();

        var currentScene = await LoadSceneWithIncludes(db, scene2.Id);

        var context = await builder.BuildRegenerationContextAsync(adventure.Id, currentScene, CancellationToken.None);

        await Assert.That(context.CharacterUpdates).Count().IsEqualTo(1);
        var elenaUpdate = context.CharacterUpdates.Single();
        // CharacterUpdates contains state from the CURRENT scene (scene2)
        await Assert.That(elenaUpdate.CharacterState.Name).IsEqualTo("Elena - scene 2 state");
        // Description comes from the character entity via existingCharContext
        await Assert.That(elenaUpdate.Description).IsEqualTo("A brave warrior");
        await Assert.That(elenaUpdate.CharacterId).IsEqualTo(character.Id);
    }

    [Test]
    public async Task CharacterUpdates_IncludesMemoriesFromCurrentScene()
    {
        await using var db = fixture.CreateDbContext();
        var builder = CreateBuilder();

        var adventure = TestData.CreateAdventure();
        var scene1 = TestData.CreateScene(adventure.Id, 1, selectedAction: "Action 1");
        var scene2 = TestData.CreateScene(adventure.Id, 2, selectedAction: "Action 2");
        adventure.Scenes.Add(scene1);
        adventure.Scenes.Add(scene2);

        var character = TestData.CreateCharacter(adventure.Id, scene1, "Elena");
        character.CharacterStates.Add(TestData.CreateCharacterState(character.Id, scene1, 1, "Elena"));
        var state2 = TestData.CreateCharacterState(character.Id, scene2, 2, "Elena");
        character.CharacterStates.Add(state2);
        scene2.CharacterStates.Add(state2);

        // Memory created in scene2 - should be in CharacterUpdates
        var scene2Memory = TestData.CreateCharacterMemory(character.Id, scene2.Id, "Memory created in scene 2");
        character.CharacterMemories.Add(scene2Memory);
        scene2.CharacterMemories.Add(scene2Memory);

        adventure.Characters.Add(character);

        db.Adventures.Add(adventure);
        await db.SaveChangesAsync();

        var currentScene = await LoadSceneWithIncludes(db, scene2.Id);

        var context = await builder.BuildRegenerationContextAsync(adventure.Id, currentScene, CancellationToken.None);

        var elenaUpdate = context.CharacterUpdates.Single();
        await Assert.That(elenaUpdate.CharacterMemories).Count().IsEqualTo(1);
        await Assert.That(elenaUpdate.CharacterMemories.Single().MemoryContent).IsEqualTo("Memory created in scene 2");
    }

    [Test]
    public async Task WithLorebooks_MapsLoreByCategory()
    {
        await using var db = fixture.CreateDbContext();
        var builder = CreateBuilder();

        var adventure = TestData.CreateAdventure();
        var scene = TestData.CreateScene(adventure.Id, 1, selectedAction: "Look around");

        scene.Lorebooks.Add(TestData.CreateLorebookEntry(adventure.Id, scene.Id, "Lore", "Ancient History",
            "{\"name\":\"Ancient History\",\"content\":\"Tales of old\"}"));
        scene.Lorebooks.Add(TestData.CreateLorebookEntry(adventure.Id, scene.Id, "Location", "The Tavern",
            "{\"name\":\"The Tavern\",\"description\":\"A cozy place\"}"));
        scene.Lorebooks.Add(TestData.CreateLorebookEntry(adventure.Id, scene.Id, "Item", "Magic Sword",
            "{\"name\":\"Magic Sword\",\"description\":\"A powerful weapon\"}"));

        adventure.Scenes.Add(scene);
        db.Adventures.Add(adventure);
        await db.SaveChangesAsync();

        var loadedScene = await LoadSceneWithIncludes(db, scene.Id);

        var context = await builder.BuildRegenerationContextAsync(adventure.Id, loadedScene, CancellationToken.None);

        await Assert.That(context.NewLore).Count().IsEqualTo(1);
        await Assert.That(context.NewLore.Single().Title).IsEqualTo("Ancient History");
        await Assert.That(context.NewLocations).Count().IsEqualTo(1);
        await Assert.That(context.NewLocations!.Single().Title).IsEqualTo("The Tavern");
        await Assert.That(context.NewItems).Count().IsEqualTo(1);
        await Assert.That(context.NewItems!.Single().Name).IsEqualTo("Magic Sword");
    }

    [Test]
    public async Task SeparatesNewAndExistingCharacters_ByIntroductionSceneAndVersion()
    {
        await using var db = fixture.CreateDbContext();
        var builder = CreateBuilder();

        var adventure = TestData.CreateAdventure();
        var scene1 = TestData.CreateScene(adventure.Id, 1, selectedAction: "Action 1");
        var scene2 = TestData.CreateScene(adventure.Id, 2, selectedAction: "Action 2");
        adventure.Scenes.Add(scene1);
        adventure.Scenes.Add(scene2);

        // Existing character: introduced in scene1, version=1
        var existingChar = TestData.CreateCharacter(adventure.Id, scene1, "Elena", isNew: false);
        existingChar.CharacterStates.Add(TestData.CreateCharacterState(existingChar.Id, scene1, 1, "Elena"));
        var elenaState2 = TestData.CreateCharacterState(existingChar.Id, scene2, 2, "Elena");
        existingChar.CharacterStates.Add(elenaState2);
        scene2.CharacterStates.Add(elenaState2);

        // New character: introduced in scene2 (IntroductionScene=scene2.Id), version=0, sequenceNumber=0
        var newChar = TestData.CreateCharacter(adventure.Id, scene2, "Marcus", isNew: true);
        var marcusState = TestData.CreateCharacterState(newChar.Id, scene2, 0, "Marcus");
        newChar.CharacterStates.Add(marcusState);
        scene2.CharacterStates.Add(marcusState);

        adventure.Characters.Add(existingChar);
        adventure.Characters.Add(newChar);

        db.Adventures.Add(adventure);
        await db.SaveChangesAsync();

        var currentScene = await LoadSceneWithIncludes(db, scene2.Id);

        var context = await builder.BuildRegenerationContextAsync(adventure.Id, currentScene, CancellationToken.None);

        // NewCharacters: characters with IntroductionScene == currentScene and Version == 0
        await Assert.That(context.NewCharacters).Count().IsEqualTo(1);
        await Assert.That(context.NewCharacters.Single().Name).IsEqualTo("Marcus");

        // CharacterUpdates: existing characters (SequenceNumber != 0) in the current scene
        await Assert.That(context.CharacterUpdates).Count().IsEqualTo(1);
        await Assert.That(context.CharacterUpdates.Single().Name).IsEqualTo("Elena");
    }

    [Test]
    public async Task Characters_ContainsStateFromBeforeCurrentScene()
    {
        await using var db = fixture.CreateDbContext();
        var builder = CreateBuilder();

        var adventure = TestData.CreateAdventure();
        var scene1 = TestData.CreateScene(adventure.Id, 1, selectedAction: "Action 1");
        var scene2 = TestData.CreateScene(adventure.Id, 2, selectedAction: "Action 2");
        var scene3 = TestData.CreateScene(adventure.Id, 3, selectedAction: "Action 3");
        adventure.Scenes.Add(scene1);
        adventure.Scenes.Add(scene2);
        adventure.Scenes.Add(scene3);

        var character = TestData.CreateCharacter(adventure.Id, scene1, "Elena", isNew: false);
        character.CharacterStates.Add(TestData.CreateCharacterState(character.Id, scene1, 1, "Elena after scene 1"));
        character.CharacterStates.Add(TestData.CreateCharacterState(character.Id, scene2, 2, "Elena after scene 2"));
        var state3 = TestData.CreateCharacterState(character.Id, scene3, 3, "Elena after scene 3");
        character.CharacterStates.Add(state3);
        scene3.CharacterStates.Add(state3);
        adventure.Characters.Add(character);

        db.Adventures.Add(adventure);
        await db.SaveChangesAsync();

        var currentScene = await LoadSceneWithIncludes(db, scene3.Id);

        var context = await builder.BuildRegenerationContextAsync(adventure.Id, currentScene, CancellationToken.None);

        // Characters collection should have state from BEFORE current scene (for regeneration input)
        var elena = context.Characters.Single();
        await Assert.That(elena.CharacterState.Name).IsEqualTo("Elena after scene 2");
    }

    [Test]
    public async Task Characters_ExcludesMemoriesFromCurrentScene()
    {
        await using var db = fixture.CreateDbContext();
        var builder = CreateBuilder();

        var adventure = TestData.CreateAdventure();
        var scene1 = TestData.CreateScene(adventure.Id, 1, selectedAction: "Action 1");
        var scene2 = TestData.CreateScene(adventure.Id, 2, selectedAction: "Action 2");
        adventure.Scenes.Add(scene1);
        adventure.Scenes.Add(scene2);

        var character = TestData.CreateCharacter(adventure.Id, scene1, "Elena", isNew: false);
        character.CharacterStates.Add(TestData.CreateCharacterState(character.Id, scene1, 1, "Elena"));
        var state2 = TestData.CreateCharacterState(character.Id, scene2, 2, "Elena");
        character.CharacterStates.Add(state2);
        scene2.CharacterStates.Add(state2);

        character.CharacterMemories.Add(TestData.CreateCharacterMemory(character.Id, scene1.Id, "Memory from scene 1"));
        character.CharacterMemories.Add(TestData.CreateCharacterMemory(character.Id, scene2.Id, "Memory from current scene - excluded"));

        adventure.Characters.Add(character);

        db.Adventures.Add(adventure);
        await db.SaveChangesAsync();

        var currentScene = await LoadSceneWithIncludes(db, scene2.Id);

        var context = await builder.BuildRegenerationContextAsync(adventure.Id, currentScene, CancellationToken.None);

        var elena = context.Characters.Single();
        await Assert.That(elena.CharacterMemories).Count().IsEqualTo(1);
        await Assert.That(elena.CharacterMemories.Single().MemoryContent).IsEqualTo("Memory from scene 1");
    }

    [Test]
    public async Task Characters_ExcludesLatestRelationshipPerTarget()
    {
        await using var db = fixture.CreateDbContext();
        var builder = CreateBuilder();

        var adventure = TestData.CreateAdventure();
        var scene1 = TestData.CreateScene(adventure.Id, 1, selectedAction: "Action 1");
        var scene2 = TestData.CreateScene(adventure.Id, 2, selectedAction: "Action 2");
        var scene3 = TestData.CreateScene(adventure.Id, 3, selectedAction: "Action 3");
        adventure.Scenes.Add(scene1);
        adventure.Scenes.Add(scene2);
        adventure.Scenes.Add(scene3);

        var character = TestData.CreateCharacter(adventure.Id, scene1, "Elena", isNew: false);
        character.CharacterStates.Add(TestData.CreateCharacterState(character.Id, scene1, 1, "Elena"));
        character.CharacterStates.Add(TestData.CreateCharacterState(character.Id, scene2, 2, "Elena"));
        var state3 = TestData.CreateCharacterState(character.Id, scene3, 3, "Elena");
        character.CharacterStates.Add(state3);
        scene3.CharacterStates.Add(state3);

        // Three relationships with same target, different sequence numbers
        character.CharacterRelationships.Add(TestData.CreateCharacterRelationship(character.Id, scene1.Id, 1, "Hero", "Just met"));
        character.CharacterRelationships.Add(TestData.CreateCharacterRelationship(character.Id, scene2.Id, 2, "Hero", "Getting closer"));
        character.CharacterRelationships.Add(TestData.CreateCharacterRelationship(character.Id, scene3.Id, 3, "Hero", "Best friends now"));

        adventure.Characters.Add(character);

        db.Adventures.Add(adventure);
        await db.SaveChangesAsync();

        var currentScene = await LoadSceneWithIncludes(db, scene3.Id);

        var context = await builder.BuildRegenerationContextAsync(adventure.Id, currentScene, CancellationToken.None);

        var elena = context.Characters.Single();
        // Should skip the most recent (sequence 3) and return second-latest (sequence 2)
        await Assert.That(elena.Relationships).Count().IsEqualTo(1);
        await Assert.That(elena.Relationships.Single().Dynamic.ToString()).IsEqualTo("Getting closer");
        await Assert.That(elena.Relationships.Single().SequenceNumber).IsEqualTo(2);
    }

    [Test]
    public async Task Characters_ExcludesSceneRewritesFromCurrentScene()
    {
        await using var db = fixture.CreateDbContext();
        var builder = CreateBuilder();

        var adventure = TestData.CreateAdventure();
        var scene1 = TestData.CreateScene(adventure.Id, 1, selectedAction: "Action 1");
        var scene2 = TestData.CreateScene(adventure.Id, 2, selectedAction: "Action 2");
        adventure.Scenes.Add(scene1);
        adventure.Scenes.Add(scene2);

        var character = TestData.CreateCharacter(adventure.Id, scene1, "Elena", isNew: false);
        character.CharacterStates.Add(TestData.CreateCharacterState(character.Id, scene1, 1, "Elena"));
        var state2 = TestData.CreateCharacterState(character.Id, scene2, 2, "Elena");
        character.CharacterStates.Add(state2);
        scene2.CharacterStates.Add(state2);

        character.CharacterSceneRewrites.Add(TestData.CreateCharacterSceneRewrite(character.Id, scene1.Id, 1, "Scene 1 POV"));
        character.CharacterSceneRewrites.Add(TestData.CreateCharacterSceneRewrite(character.Id, scene2.Id, 2, "Scene 2 POV - excluded"));

        adventure.Characters.Add(character);

        db.Adventures.Add(adventure);
        await db.SaveChangesAsync();

        var currentScene = await LoadSceneWithIncludes(db, scene2.Id);

        var context = await builder.BuildRegenerationContextAsync(adventure.Id, currentScene, CancellationToken.None);

        var elena = context.Characters.Single();
        await Assert.That(elena.SceneRewrites).Count().IsEqualTo(1);
        await Assert.That(elena.SceneRewrites.Single().Content).IsEqualTo("Scene 1 POV");
    }

    [Test]
    public async Task NewCharacters_IncludesAllTheirData()
    {
        await using var db = fixture.CreateDbContext();
        var builder = CreateBuilder();

        var adventure = TestData.CreateAdventure();
        var scene1 = TestData.CreateScene(adventure.Id, 1, selectedAction: "Action 1");
        adventure.Scenes.Add(scene1);

        var newChar = TestData.CreateCharacter(adventure.Id, scene1, "Marcus", isNew: true, description: "A mysterious stranger");
        var state = TestData.CreateCharacterState(newChar.Id, scene1, 0, "Marcus initial state");
        newChar.CharacterStates.Add(state);
        newChar.CharacterMemories.Add(TestData.CreateCharacterMemory(newChar.Id, scene1.Id, "First memory"));
        newChar.CharacterRelationships.Add(TestData.CreateCharacterRelationship(newChar.Id, scene1.Id, 1, "Hero", "Curious about"));
        newChar.CharacterSceneRewrites.Add(TestData.CreateCharacterSceneRewrite(newChar.Id, scene1.Id, 1, "Marcus sees the hero"));
        scene1.CharacterStates.Add(state);

        adventure.Characters.Add(newChar);

        db.Adventures.Add(adventure);
        await db.SaveChangesAsync();

        var currentScene = await LoadSceneWithIncludes(db, scene1.Id);

        var context = await builder.BuildRegenerationContextAsync(adventure.Id, currentScene, CancellationToken.None);

        await Assert.That(context.NewCharacters).Count().IsEqualTo(1);
        var marcus = context.NewCharacters.Single();
        await Assert.That(marcus.Name).IsEqualTo("Marcus");
        await Assert.That(marcus.Description).IsEqualTo("A mysterious stranger");
        await Assert.That(marcus.CharacterState.Name).IsEqualTo("Marcus initial state");
        await Assert.That(marcus.CharacterMemories).Count().IsEqualTo(1);
        await Assert.That(marcus.CharacterMemories.Single().MemoryContent).IsEqualTo("First memory");
        await Assert.That(marcus.Relationships).Count().IsEqualTo(1);
        await Assert.That(marcus.Relationships.Single().Dynamic.ToString()).IsEqualTo("Curious about");
        await Assert.That(marcus.SceneRewrites).Count().IsEqualTo(1);
    }

    [Test]
    public async Task NoCharacters_ReturnsEmptyCollections()
    {
        await using var db = fixture.CreateDbContext();
        var builder = CreateBuilder();

        var adventure = TestData.CreateAdventure();
        var scene = TestData.CreateScene(adventure.Id, 1, selectedAction: "Look around");
        adventure.Scenes.Add(scene);

        db.Adventures.Add(adventure);
        await db.SaveChangesAsync();

        var loadedScene = await LoadSceneWithIncludes(db, scene.Id);

        var context = await builder.BuildRegenerationContextAsync(adventure.Id, loadedScene, CancellationToken.None);

        await Assert.That(context.Characters).IsEmpty();
        await Assert.That(context.NewCharacters).IsEmpty();
        await Assert.That(context.CharacterUpdates).IsEmpty();
    }

    [Test]
    public async Task PlayerAction_TakenFromSelectedCharacterAction()
    {
        await using var db = fixture.CreateDbContext();
        var builder = CreateBuilder();

        var adventure = TestData.CreateAdventure();
        var scene = TestData.CreateScene(adventure.Id, 1, selectedAction: "Search the room carefully");
        adventure.Scenes.Add(scene);

        db.Adventures.Add(adventure);
        await db.SaveChangesAsync();

        var loadedScene = await LoadSceneWithIncludes(db, scene.Id);

        var context = await builder.BuildRegenerationContextAsync(adventure.Id, loadedScene, CancellationToken.None);

        await Assert.That(context.PlayerAction).IsEqualTo("Search the room carefully");
    }

    [Test]
    public async Task SetsUpRequiredFieldsFromAdventure()
    {
        await using var db = fixture.CreateDbContext();
        var builder = CreateBuilder();

        var adventure = TestData.CreateAdventure();
        adventure.PromptPath = "CustomPrompts";
        adventure.AdventureStartTime = "08:00 15-03-845";
        var scene = TestData.CreateScene(adventure.Id, 1, selectedAction: "Look around");
        adventure.Scenes.Add(scene);

        db.Adventures.Add(adventure);
        await db.SaveChangesAsync();

        var loadedScene = await LoadSceneWithIncludes(db, scene.Id);

        var context = await builder.BuildRegenerationContextAsync(adventure.Id, loadedScene, CancellationToken.None);

        await Assert.That(context.PromptPath).IsEqualTo("CustomPrompts");
        await Assert.That(context.AdventureStartTime).IsEqualTo("08:00 15-03-845");
        await Assert.That(context.MainCharacter).IsNotNull();
        await Assert.That(context.TrackerStructure).IsNotNull();
    }

    private static async Task<Infrastructure.Persistence.Entities.Adventure.Scene> LoadSceneWithIncludes(
        Infrastructure.Persistence.ApplicationDbContext db,
        Guid sceneId)
    {
        return await db.Scenes
            .Include(s => s.CharacterActions)
            .Include(s => s.CharacterStates)
            .Include(s => s.CharacterMemories)
            .Include(s => s.CharacterRelationships)
            .Include(s => s.CharacterSceneRewrites)
            .Include(s => s.Lorebooks)
            .FirstAsync(s => s.Id == sceneId);
    }
}
