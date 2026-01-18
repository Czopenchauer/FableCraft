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

        var loadedScene = await db.Scenes
            .Include(s => s.CharacterActions)
            .Include(s => s.CharacterStates)
            .Include(s => s.Lorebooks)
            .FirstAsync(s => s.Id == scene.Id);

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
    public async Task WithCharacterStates_MapsCharacterUpdatesCorrectly()
    {
        await using var db = fixture.CreateDbContext();
        var builder = CreateBuilder();

        var adventure = TestData.CreateAdventure();
        var scene1 = TestData.CreateScene(adventure.Id, 1, selectedAction: "Action 1");
        var scene2 = TestData.CreateScene(adventure.Id, 2, selectedAction: "Action 2");
        adventure.Scenes.Add(scene1);
        adventure.Scenes.Add(scene2);

        var character = TestData.CreateCharacter(adventure.Id, scene1, "Elena");
        var state1 = TestData.CreateCharacterState(character.Id, scene1, 1, "Elena");
        var state2 = TestData.CreateCharacterState(character.Id, scene2, 2, "Elena");
        character.CharacterStates.Add(state1);
        character.CharacterStates.Add(state2);
        scene2.CharacterStates.Add(state2);
        adventure.Characters.Add(character);

        db.Adventures.Add(adventure);
        await db.SaveChangesAsync();

        var currentScene = await db.Scenes
            .Include(s => s.CharacterActions)
            .Include(s => s.CharacterStates)
            .Include(s => s.CharacterMemories)
            .Include(s => s.CharacterRelationships)
            .Include(s => s.CharacterSceneRewrites)
            .Include(s => s.Lorebooks)
            .FirstAsync(s => s.Id == scene2.Id);

        var context = await builder.BuildRegenerationContextAsync(adventure.Id, currentScene, CancellationToken.None);

        await Assert.That(context.CharacterUpdates).Count().IsEqualTo(1);
        await Assert.That(context.CharacterUpdates.Single().Name).IsEqualTo("Elena");
        await Assert.That(context.CharacterUpdates.Single().CharacterId).IsEqualTo(character.Id);
    }

    [Test]
    public async Task WithLorebooks_MapsLoreByCategory()
    {
        await using var db = fixture.CreateDbContext();
        var builder = CreateBuilder();

        var adventure = TestData.CreateAdventure();
        var scene = TestData.CreateScene(adventure.Id, 1, selectedAction: "Look around");

        scene.Lorebooks.Add(TestData.CreateLorebookEntry(adventure.Id, scene.Id, "Lore", "Ancient History",
            "{\"title\":\"Ancient History\",\"description\":\"Tales of old\"}"));
        scene.Lorebooks.Add(TestData.CreateLorebookEntry(adventure.Id, scene.Id, "Location", "The Tavern",
            "{\"name\":\"The Tavern\",\"description\":\"A cozy place\"}"));
        scene.Lorebooks.Add(TestData.CreateLorebookEntry(adventure.Id, scene.Id, "Item", "Magic Sword",
            "{\"name\":\"Magic Sword\",\"description\":\"A powerful weapon\"}"));

        adventure.Scenes.Add(scene);
        db.Adventures.Add(adventure);
        await db.SaveChangesAsync();

        var loadedScene = await db.Scenes
            .Include(s => s.CharacterActions)
            .Include(s => s.CharacterStates)
            .Include(s => s.Lorebooks)
            .FirstAsync(s => s.Id == scene.Id);

        var context = await builder.BuildRegenerationContextAsync(adventure.Id, loadedScene, CancellationToken.None);

        await Assert.That(context.NewLore).Count().IsEqualTo(1);
        await Assert.That(context.NewLocations).Count().IsEqualTo(1);
        await Assert.That(context.NewItems).Count().IsEqualTo(1);
    }

    [Test]
    public async Task WithNewAndExistingCharacters_SeparatesCorrectly()
    {
        await using var db = fixture.CreateDbContext();
        var builder = CreateBuilder();

        var adventure = TestData.CreateAdventure();
        var scene1 = TestData.CreateScene(adventure.Id, 1, selectedAction: "Action 1");
        var scene2 = TestData.CreateScene(adventure.Id, 2, selectedAction: "Action 2");
        adventure.Scenes.Add(scene1);
        adventure.Scenes.Add(scene2);

        // Existing character (introduced in scene1, version=1)
        var existingChar = TestData.CreateCharacter(adventure.Id, scene1, "Elena", isNew: false);
        existingChar.CharacterStates.Add(TestData.CreateCharacterState(existingChar.Id, scene1, 1, "Elena"));
        existingChar.CharacterStates.Add(TestData.CreateCharacterState(existingChar.Id, scene2, 2, "Elena"));
        scene2.CharacterStates.Add(existingChar.CharacterStates.Last());

        // New character (introduced in scene2, version=0, sequenceNumber=0 indicates new)
        var newChar = TestData.CreateCharacter(adventure.Id, scene2, "Marcus", isNew: true);
        var newCharState = TestData.CreateCharacterState(newChar.Id, scene2, 0, "Marcus");
        newChar.CharacterStates.Add(newCharState);
        scene2.CharacterStates.Add(newCharState);

        adventure.Characters.Add(existingChar);
        adventure.Characters.Add(newChar);

        db.Adventures.Add(adventure);
        await db.SaveChangesAsync();

        var currentScene = await db.Scenes
            .Include(s => s.CharacterActions)
            .Include(s => s.CharacterStates)
            .Include(s => s.CharacterMemories)
            .Include(s => s.CharacterRelationships)
            .Include(s => s.CharacterSceneRewrites)
            .Include(s => s.Lorebooks)
            .FirstAsync(s => s.Id == scene2.Id);

        var context = await builder.BuildRegenerationContextAsync(adventure.Id, currentScene, CancellationToken.None);

        await Assert.That(context.NewCharacters).Count().IsEqualTo(1);
        await Assert.That(context.NewCharacters.Single().Name).IsEqualTo("Marcus");
        await Assert.That(context.CharacterUpdates).Count().IsEqualTo(1);
        await Assert.That(context.CharacterUpdates.Single().Name).IsEqualTo("Elena");
    }

    [Test]
    public async Task WhenRegenerating_ExistingCharactersHaveStateFromBeforeCurrentScene()
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
        var state1 = TestData.CreateCharacterState(character.Id, scene1, 1, "Elena");
        state1.CharacterStats = new() { Name = "Elena", Motivations = new { primary = "Scene 1 motivation" }, Routine = new { } };
        var state2 = TestData.CreateCharacterState(character.Id, scene2, 2, "Elena");
        state2.CharacterStats = new() { Name = "Elena", Motivations = new { primary = "Scene 2 motivation" }, Routine = new { } };
        var state3 = TestData.CreateCharacterState(character.Id, scene3, 3, "Elena");
        state3.CharacterStats = new() { Name = "Elena", Motivations = new { primary = "Scene 3 motivation - should not appear" }, Routine = new { } };

        character.CharacterStates.Add(state1);
        character.CharacterStates.Add(state2);
        character.CharacterStates.Add(state3);
        scene3.CharacterStates.Add(state3);
        adventure.Characters.Add(character);

        db.Adventures.Add(adventure);
        await db.SaveChangesAsync();

        var currentScene = await db.Scenes
            .Include(s => s.CharacterActions)
            .Include(s => s.CharacterStates)
            .Include(s => s.CharacterMemories)
            .Include(s => s.CharacterRelationships)
            .Include(s => s.CharacterSceneRewrites)
            .Include(s => s.Lorebooks)
            .FirstAsync(s => s.Id == scene3.Id);

        var context = await builder.BuildRegenerationContextAsync(adventure.Id, currentScene, CancellationToken.None);

        var elena = context.Characters.Single(c => c.Name == "Elena");
        // When regenerating scene 3, character state should reflect state before scene 3 was generated
        await Assert.That(elena.CharacterState.Motivations?.ToString()).Contains("Scene 2 motivation");
        await Assert.That(elena.CharacterState.Motivations?.ToString()).DoesNotContain("Scene 3");
    }

    [Test]
    public async Task WhenRegenerating_ExcludesMemoriesFromCurrentScene()
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
        character.CharacterStates.Add(TestData.CreateCharacterState(character.Id, scene2, 2, "Elena"));
        scene2.CharacterStates.Add(character.CharacterStates.Last());

        character.CharacterMemories.Add(TestData.CreateCharacterMemory(character.Id, scene1.Id, "Memory from scene 1"));
        character.CharacterMemories.Add(TestData.CreateCharacterMemory(character.Id, scene2.Id, "Memory from current scene"));

        adventure.Characters.Add(character);

        db.Adventures.Add(adventure);
        await db.SaveChangesAsync();

        var currentScene = await db.Scenes
            .Include(s => s.CharacterActions)
            .Include(s => s.CharacterStates)
            .Include(s => s.CharacterMemories)
            .Include(s => s.CharacterRelationships)
            .Include(s => s.CharacterSceneRewrites)
            .Include(s => s.Lorebooks)
            .FirstAsync(s => s.Id == scene2.Id);

        var context = await builder.BuildRegenerationContextAsync(adventure.Id, currentScene, CancellationToken.None);

        var elena = context.Characters.Single(c => c.Name == "Elena");
        await Assert.That(elena.CharacterMemories).Count().IsEqualTo(1);
        await Assert.That(elena.CharacterMemories.Single().MemoryContent).IsEqualTo("Memory from scene 1");
    }

    [Test]
    public async Task WhenRegenerating_ExcludesRelationshipsFromCurrentScene()
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
        character.CharacterStates.Add(TestData.CreateCharacterState(character.Id, scene3, 3, "Elena"));
        scene3.CharacterStates.Add(character.CharacterStates.Last());

        character.CharacterRelationships.Add(TestData.CreateCharacterRelationship(character.Id, scene1.Id, 1, "Hero", "Initial meeting"));
        character.CharacterRelationships.Add(TestData.CreateCharacterRelationship(character.Id, scene2.Id, 2, "Hero", "Growing friendship"));
        character.CharacterRelationships.Add(TestData.CreateCharacterRelationship(character.Id, scene3.Id, 3, "Hero", "Current scene relationship"));

        adventure.Characters.Add(character);

        db.Adventures.Add(adventure);
        await db.SaveChangesAsync();

        var currentScene = await db.Scenes
            .Include(s => s.CharacterActions)
            .Include(s => s.CharacterStates)
            .Include(s => s.CharacterMemories)
            .Include(s => s.CharacterRelationships)
            .Include(s => s.CharacterSceneRewrites)
            .Include(s => s.Lorebooks)
            .FirstAsync(s => s.Id == scene3.Id);

        var context = await builder.BuildRegenerationContextAsync(adventure.Id, currentScene, CancellationToken.None);

        var elena = context.Characters.Single(c => c.Name == "Elena");
        // Should have relationship state from before current scene
        await Assert.That(elena.Relationships).Count().IsEqualTo(1);
        await Assert.That(elena.Relationships.Single().Dynamic.ToString()).DoesNotContain("Current scene");
    }

    [Test]
    public async Task WhenRegenerating_ExcludesSceneRewritesFromCurrentScene()
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
        character.CharacterStates.Add(TestData.CreateCharacterState(character.Id, scene2, 2, "Elena"));
        scene2.CharacterStates.Add(character.CharacterStates.Last());

        character.CharacterSceneRewrites.Add(TestData.CreateCharacterSceneRewrite(character.Id, scene1.Id, 1, "Scene 1 from Elena's perspective"));
        character.CharacterSceneRewrites.Add(TestData.CreateCharacterSceneRewrite(character.Id, scene2.Id, 2, "Current scene rewrite"));

        adventure.Characters.Add(character);

        db.Adventures.Add(adventure);
        await db.SaveChangesAsync();

        var currentScene = await db.Scenes
            .Include(s => s.CharacterActions)
            .Include(s => s.CharacterStates)
            .Include(s => s.CharacterMemories)
            .Include(s => s.CharacterRelationships)
            .Include(s => s.CharacterSceneRewrites)
            .Include(s => s.Lorebooks)
            .FirstAsync(s => s.Id == scene2.Id);

        var context = await builder.BuildRegenerationContextAsync(adventure.Id, currentScene, CancellationToken.None);

        var elena = context.Characters.Single(c => c.Name == "Elena");
        await Assert.That(elena.SceneRewrites).Count().IsEqualTo(1);
        await Assert.That(elena.SceneRewrites.Single().Content).IsEqualTo("Scene 1 from Elena's perspective");
    }

    [Test]
    public async Task WhenRegenerating_NewCharactersIncludeAllTheirData()
    {
        await using var db = fixture.CreateDbContext();
        var builder = CreateBuilder();

        var adventure = TestData.CreateAdventure();
        var scene1 = TestData.CreateScene(adventure.Id, 1, selectedAction: "Action 1");
        adventure.Scenes.Add(scene1);

        var newChar = TestData.CreateCharacter(adventure.Id, scene1, "Marcus", isNew: true);
        var newCharState = TestData.CreateCharacterState(newChar.Id, scene1, 0, "Marcus");
        newChar.CharacterStates.Add(newCharState);
        newChar.CharacterMemories.Add(TestData.CreateCharacterMemory(newChar.Id, scene1.Id, "First memory"));
        newChar.CharacterRelationships.Add(TestData.CreateCharacterRelationship(newChar.Id, scene1.Id, 1, "Hero", "First meeting"));
        newChar.CharacterSceneRewrites.Add(TestData.CreateCharacterSceneRewrite(newChar.Id, scene1.Id, 1, "Marcus POV"));
        scene1.CharacterStates.Add(newCharState);

        adventure.Characters.Add(newChar);

        db.Adventures.Add(adventure);
        await db.SaveChangesAsync();

        var currentScene = await db.Scenes
            .Include(s => s.CharacterActions)
            .Include(s => s.CharacterStates)
            .Include(s => s.CharacterMemories)
            .Include(s => s.CharacterRelationships)
            .Include(s => s.CharacterSceneRewrites)
            .Include(s => s.Lorebooks)
            .FirstAsync(s => s.Id == scene1.Id);

        var context = await builder.BuildRegenerationContextAsync(adventure.Id, currentScene, CancellationToken.None);

        await Assert.That(context.NewCharacters).Count().IsEqualTo(1);
        var marcus = context.NewCharacters.Single();
        await Assert.That(marcus.Name).IsEqualTo("Marcus");
        await Assert.That(marcus.CharacterMemories).Count().IsEqualTo(1);
        await Assert.That(marcus.Relationships).Count().IsEqualTo(1);
        await Assert.That(marcus.SceneRewrites).Count().IsEqualTo(1);
    }

    [Test]
    public async Task NoCharacters_ReturnsEmptyCharacterCollections()
    {
        await using var db = fixture.CreateDbContext();
        var builder = CreateBuilder();

        var adventure = TestData.CreateAdventure();
        var scene = TestData.CreateScene(adventure.Id, 1, selectedAction: "Look around");
        adventure.Scenes.Add(scene);

        db.Adventures.Add(adventure);
        await db.SaveChangesAsync();

        var loadedScene = await db.Scenes
            .Include(s => s.CharacterActions)
            .Include(s => s.CharacterStates)
            .Include(s => s.Lorebooks)
            .FirstAsync(s => s.Id == scene.Id);

        var context = await builder.BuildRegenerationContextAsync(adventure.Id, loadedScene, CancellationToken.None);

        await Assert.That(context.Characters).IsEmpty();
        await Assert.That(context.NewCharacters).IsEmpty();
        await Assert.That(context.CharacterUpdates).IsEmpty();
    }
}
