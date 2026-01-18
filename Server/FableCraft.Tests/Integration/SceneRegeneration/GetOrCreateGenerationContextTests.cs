using FableCraft.Application.NarrativeEngine;
using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities;
using FableCraft.Tests.Integration.SceneRegeneration.Fixtures;

using Microsoft.EntityFrameworkCore;

namespace FableCraft.Tests.Integration.SceneRegeneration;

[ClassDataSource<PostgresContainerFixture>(Shared = SharedType.PerTestSession)]
public class GetOrCreateGenerationContextTests(PostgresContainerFixture fixture)
{
    private IGenerationContextBuilder CreateBuilder()
    {
        var db = fixture.CreateDbContext();
        return new GenerationContextBuilder(db);
    }

    [Test]
    public async Task WhenNoProcessExists_CreatesNewProcess()
    {
        await using var db = fixture.CreateDbContext();
        var builder = CreateBuilder();

        var adventure = TestData.CreateAdventure();
        var scene = TestData.CreateScene(adventure.Id, 1, selectedAction: "Look around");
        adventure.Scenes.Add(scene);

        db.Adventures.Add(adventure);
        await db.SaveChangesAsync();

        var (context, step) = await builder.GetOrCreateGenerationContextAsync(
            adventure.Id, "Explore the forest", CancellationToken.None);

        await Assert.That(context.AdventureId).IsEqualTo(adventure.Id);
        await Assert.That(context.PlayerAction).IsEqualTo("Explore the forest");
        await Assert.That(context.NewSceneId).IsNotNull();
        await Assert.That(step).IsEqualTo(GenerationProcessStep.NotStarted);

        // Verify process was persisted
        var savedProcess = await db.GenerationProcesses.FirstOrDefaultAsync(p => p.AdventureId == adventure.Id);
        await Assert.That(savedProcess).IsNotNull();
    }

    [Test]
    public async Task WhenProcessExistsWithSameAction_ReusesExistingProcess()
    {
        await using var db = fixture.CreateDbContext();
        var builder = CreateBuilder();

        var adventure = TestData.CreateAdventure();
        var scene = TestData.CreateScene(adventure.Id, 1, selectedAction: "Look around");
        adventure.Scenes.Add(scene);

        var existingSceneId = Guid.NewGuid();
        var existingContext = new GenerationContext
        {
            AdventureId = adventure.Id,
            PlayerAction = "Explore the forest",
            NewSceneId = existingSceneId
        };
        var existingProcess = new GenerationProcess
        {
            AdventureId = adventure.Id,
            Context = existingContext.ToJsonString(),
            Step = GenerationProcessStep.GeneratingScene
        };

        db.Adventures.Add(adventure);
        db.GenerationProcesses.Add(existingProcess);
        await db.SaveChangesAsync();

        var (context, step) = await builder.GetOrCreateGenerationContextAsync(
            adventure.Id, "Explore the forest", CancellationToken.None);

        await Assert.That(context.PlayerAction).IsEqualTo("Explore the forest");
        await Assert.That(context.NewSceneId).IsEqualTo(existingSceneId);
        await Assert.That(step).IsEqualTo(GenerationProcessStep.GeneratingScene);
    }

    [Test]
    public async Task WhenProcessExistsWithDifferentAction_CreatesNewProcess()
    {
        await using var db = fixture.CreateDbContext();
        var builder = CreateBuilder();

        var adventure = TestData.CreateAdventure();
        var scene = TestData.CreateScene(adventure.Id, 1, selectedAction: "Look around");
        adventure.Scenes.Add(scene);

        var oldSceneId = Guid.NewGuid();
        var existingContext = new GenerationContext
        {
            AdventureId = adventure.Id,
            PlayerAction = "Explore the forest",
            NewSceneId = oldSceneId
        };
        var existingProcess = new GenerationProcess
        {
            AdventureId = adventure.Id,
            Context = existingContext.ToJsonString(),
            Step = GenerationProcessStep.GeneratingScene
        };

        db.Adventures.Add(adventure);
        db.GenerationProcesses.Add(existingProcess);
        await db.SaveChangesAsync();

        var (context, step) = await builder.GetOrCreateGenerationContextAsync(
            adventure.Id, "Go to the tavern", CancellationToken.None);

        await Assert.That(context.PlayerAction).IsEqualTo("Go to the tavern");
        await Assert.That(context.NewSceneId).IsNotEqualTo(oldSceneId);
        await Assert.That(step).IsEqualTo(GenerationProcessStep.NotStarted);
    }

    [Test]
    public async Task PopulatesScenesFromAdventure()
    {
        await using var db = fixture.CreateDbContext();
        var builder = CreateBuilder();

        var adventure = TestData.CreateAdventure();
        for (var i = 1; i <= 5; i++)
        {
            adventure.Scenes.Add(TestData.CreateScene(adventure.Id, i, selectedAction: $"Action {i}"));
        }

        db.Adventures.Add(adventure);
        await db.SaveChangesAsync();

        var (context, _) = await builder.GetOrCreateGenerationContextAsync(
            adventure.Id, "New action", CancellationToken.None);

        await Assert.That(context.SceneContext).Count().IsEqualTo(5);
        await Assert.That(context.SceneContext.Max(s => s.SequenceNumber)).IsEqualTo(5);
    }

    [Test]
    public async Task LimitsScenesTo20()
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

        var (context, _) = await builder.GetOrCreateGenerationContextAsync(
            adventure.Id, "New action", CancellationToken.None);

        await Assert.That(context.SceneContext).Count().IsEqualTo(20);
        // Should have the 20 most recent scenes
        await Assert.That(context.SceneContext.Max(s => s.SequenceNumber)).IsEqualTo(25);
        await Assert.That(context.SceneContext.Min(s => s.SequenceNumber)).IsEqualTo(6);
    }

    [Test]
    public async Task PopulatesCharactersWithLatestState()
    {
        await using var db = fixture.CreateDbContext();
        var builder = CreateBuilder();

        var adventure = TestData.CreateAdventure();
        var scene1 = TestData.CreateScene(adventure.Id, 1, selectedAction: "Action 1");
        var scene2 = TestData.CreateScene(adventure.Id, 2, selectedAction: "Action 2");
        adventure.Scenes.Add(scene1);
        adventure.Scenes.Add(scene2);

        var character = TestData.CreateCharacter(adventure.Id, scene1, "Elena");
        character.CharacterStates.Add(TestData.CreateCharacterState(character.Id, scene1, 1, "Elena - Old"));
        character.CharacterStates.Add(TestData.CreateCharacterState(character.Id, scene2, 2, "Elena - Latest"));
        adventure.Characters.Add(character);

        db.Adventures.Add(adventure);
        await db.SaveChangesAsync();

        var (context, _) = await builder.GetOrCreateGenerationContextAsync(
            adventure.Id, "New action", CancellationToken.None);

        await Assert.That(context.Characters).Count().IsEqualTo(1);
        var elena = context.Characters.Single();
        await Assert.That(elena.CharacterState.Name).IsEqualTo("Elena - Latest");
    }

    [Test]
    public async Task PopulatesCharacterMemoriesAndRelationships()
    {
        await using var db = fixture.CreateDbContext();
        var builder = CreateBuilder();

        var adventure = TestData.CreateAdventure();
        var scene = TestData.CreateScene(adventure.Id, 1, selectedAction: "Action 1");
        adventure.Scenes.Add(scene);

        var character = TestData.CreateCharacter(adventure.Id, scene, "Elena");
        character.CharacterStates.Add(TestData.CreateCharacterState(character.Id, scene, 1, "Elena"));
        character.CharacterMemories.Add(TestData.CreateCharacterMemory(character.Id, scene.Id, "Important memory"));
        character.CharacterRelationships.Add(TestData.CreateCharacterRelationship(character.Id, scene.Id, 1, "Hero", "Friendly"));
        adventure.Characters.Add(character);

        db.Adventures.Add(adventure);
        await db.SaveChangesAsync();

        var (context, _) = await builder.GetOrCreateGenerationContextAsync(
            adventure.Id, "New action", CancellationToken.None);

        var elena = context.Characters.Single();
        await Assert.That(elena.CharacterMemories).Count().IsEqualTo(1);
        await Assert.That(elena.CharacterMemories.Single().MemoryContent).IsEqualTo("Important memory");
        await Assert.That(elena.Relationships).Count().IsEqualTo(1);
        await Assert.That(elena.Relationships.Single().TargetCharacterName).IsEqualTo("Hero");
    }

    [Test]
    public async Task PopulatesLoreFromMostRecentScene()
    {
        await using var db = fixture.CreateDbContext();
        var builder = CreateBuilder();

        var adventure = TestData.CreateAdventure();
        var scene1 = TestData.CreateScene(adventure.Id, 1, selectedAction: "Action 1");
        var scene2 = TestData.CreateScene(adventure.Id, 2, selectedAction: "Action 2");

        scene1.Lorebooks.Add(TestData.CreateLorebookEntry(adventure.Id, scene1.Id, "Lore", "Old Lore",
            "{\"title\":\"Old Lore\",\"description\":\"From scene 1\"}"));
        scene2.Lorebooks.Add(TestData.CreateLorebookEntry(adventure.Id, scene2.Id, "Lore", "New Lore",
            "{\"title\":\"New Lore\",\"description\":\"From scene 2\"}"));

        adventure.Scenes.Add(scene1);
        adventure.Scenes.Add(scene2);

        db.Adventures.Add(adventure);
        await db.SaveChangesAsync();

        var (context, _) = await builder.GetOrCreateGenerationContextAsync(
            adventure.Id, "New action", CancellationToken.None);

        await Assert.That(context.PreviouslyGeneratedLore).Count().IsEqualTo(1);
        await Assert.That(context.PreviouslyGeneratedLore.Single().Title).IsEqualTo("New Lore");
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

        var (context, _) = await builder.GetOrCreateGenerationContextAsync(
            adventure.Id, "New action", CancellationToken.None);

        await Assert.That(context.PromptPath).IsEqualTo("CustomPrompts");
        await Assert.That(context.AdventureStartTime).IsEqualTo("08:00 15-03-845");
        await Assert.That(context.MainCharacter).IsNotNull();
        await Assert.That(context.MainCharacter.Name).IsEqualTo("Hero");
        await Assert.That(context.TrackerStructure).IsNotNull();
    }

    [Test]
    public async Task WhenProcessHasNullContext_CreatesNewProcess()
    {
        await using var db = fixture.CreateDbContext();
        var builder = CreateBuilder();

        var adventure = TestData.CreateAdventure();
        var scene = TestData.CreateScene(adventure.Id, 1, selectedAction: "Look around");
        adventure.Scenes.Add(scene);

        // Simulate corrupted/null context scenario
        var existingProcess = new GenerationProcess
        {
            AdventureId = adventure.Id,
            Context = "null",
            Step = GenerationProcessStep.GeneratingScene
        };

        db.Adventures.Add(adventure);
        db.GenerationProcesses.Add(existingProcess);
        await db.SaveChangesAsync();

        var (context, step) = await builder.GetOrCreateGenerationContextAsync(
            adventure.Id, "New action", CancellationToken.None);

        await Assert.That(context.PlayerAction).IsEqualTo("New action");
        await Assert.That(step).IsEqualTo(GenerationProcessStep.NotStarted);
    }
}
