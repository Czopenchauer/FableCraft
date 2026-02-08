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
        var existingProcess = CreateGenerationProcess(adventure.Id, "Explore the forest", existingSceneId, GenerationProcessStep.GeneratingScene);

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
    public async Task WhenProcessExistsWithDifferentAction_ReplacesWithNewProcess()
    {
        await using var db = fixture.CreateDbContext();
        var builder = CreateBuilder();

        var adventure = TestData.CreateAdventure();
        var scene = TestData.CreateScene(adventure.Id, 1, selectedAction: "Look around");
        adventure.Scenes.Add(scene);

        var oldSceneId = Guid.NewGuid();
        var existingProcess = CreateGenerationProcess(adventure.Id, "Explore the forest", oldSceneId, GenerationProcessStep.GeneratingScene);

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
    public async Task SceneContext_IncludesAllScenesUpTo20()
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
    public async Task SceneContext_LimitedTo20MostRecent()
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

        await Assert.That(context.SceneContext).Count().IsEqualTo(24);
        await Assert.That(context.SceneContext.Max(s => s.SequenceNumber)).IsEqualTo(24);
        await Assert.That(context.SceneContext.Min(s => s.SequenceNumber)).IsEqualTo(1);
    }

    [Test]
    public async Task Characters_HaveLatestState()
    {
        await using var db = fixture.CreateDbContext();
        var builder = CreateBuilder();

        var adventure = TestData.CreateAdventure();
        var scene1 = TestData.CreateScene(adventure.Id, 1, selectedAction: "Action 1");
        var scene2 = TestData.CreateScene(adventure.Id, 2, selectedAction: "Action 2");
        adventure.Scenes.Add(scene1);
        adventure.Scenes.Add(scene2);

        var character = TestData.CreateCharacter(adventure.Id, scene1, "Elena", description: "A skilled fighter");
        character.CharacterStates.Add(TestData.CreateCharacterState(character.Id, scene1, 1, "Elena - after scene 1"));
        character.CharacterStates.Add(TestData.CreateCharacterState(character.Id, scene2, 2, "Elena - after scene 2"));
        adventure.Characters.Add(character);

        db.Adventures.Add(adventure);
        await db.SaveChangesAsync();

        var (context, _) = await builder.GetOrCreateGenerationContextAsync(
            adventure.Id, "New action", CancellationToken.None);

        await Assert.That(context.Characters).Count().IsEqualTo(1);
        var elena = context.Characters.Single();
        await Assert.That(elena.Name).IsEqualTo("Elena");
        await Assert.That(elena.Description).IsEqualTo("A skilled fighter");
        await Assert.That(elena.CharacterState.Name).IsEqualTo("Elena - after scene 2");
    }

    [Test]
    public async Task Characters_IncludeMemoriesAndLatestRelationships()
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
        character.CharacterStates.Add(TestData.CreateCharacterState(character.Id, scene2, 2, "Elena"));
        character.CharacterMemories.Add(TestData.CreateCharacterMemory(character.Id, scene1.Id, "Important memory"));
        character.CharacterRelationships.Add(TestData.CreateCharacterRelationship(character.Id, scene1.Id, 1, "Hero", "Neutral"));
        character.CharacterRelationships.Add(TestData.CreateCharacterRelationship(character.Id, scene2.Id, 2, "Hero", "Friendly"));
        adventure.Characters.Add(character);

        db.Adventures.Add(adventure);
        await db.SaveChangesAsync();

        var (context, _) = await builder.GetOrCreateGenerationContextAsync(
            adventure.Id, "New action", CancellationToken.None);

        var elena = context.Characters.Single();
        await Assert.That(elena.CharacterMemories).Count().IsEqualTo(1);
        await Assert.That(elena.CharacterMemories.Single().MemoryContent).IsEqualTo("Important memory");
        await Assert.That(elena.Relationships).Count().IsEqualTo(1);
        await Assert.That(elena.Relationships.Single().Dynamic).IsEqualTo("Friendly");
    }

    [Test]
    public async Task PreviouslyGeneratedLore_FromMostRecentScene()
    {
        await using var db = fixture.CreateDbContext();
        var builder = CreateBuilder();

        var adventure = TestData.CreateAdventure();
        var scene1 = TestData.CreateScene(adventure.Id, 1, selectedAction: "Action 1");
        var scene2 = TestData.CreateScene(adventure.Id, 2, selectedAction: "Action 2");

        scene1.Lorebooks.Add(TestData.CreateLorebookEntry(adventure.Id, scene1.Id, "Lore", "Old Lore",
            "{\"name\":\"Old Lore\",\"content\":\"From scene 1\"}"));
        scene2.Lorebooks.Add(TestData.CreateLorebookEntry(adventure.Id, scene2.Id, "Lore", "New Lore",
            "{\"name\":\"New Lore\",\"content\":\"From scene 2\"}"));

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

        var corruptedProcess = new GenerationProcess
        {
            AdventureId = adventure.Id,
            Context = "null",
            Step = GenerationProcessStep.GeneratingScene
        };

        db.Adventures.Add(adventure);
        db.GenerationProcesses.Add(corruptedProcess);
        await db.SaveChangesAsync();

        var (context, step) = await builder.GetOrCreateGenerationContextAsync(
            adventure.Id, "New action", CancellationToken.None);

        await Assert.That(context.PlayerAction).IsEqualTo("New action");
        await Assert.That(step).IsEqualTo(GenerationProcessStep.NotStarted);
    }

    private static GenerationProcess CreateGenerationProcess(
        Guid adventureId,
        string playerAction,
        Guid? newSceneId = null,
        GenerationProcessStep step = GenerationProcessStep.NotStarted)
    {
        var context = new GenerationContext
        {
            AdventureId = adventureId,
            PlayerAction = playerAction,
            NewSceneId = newSceneId ?? Guid.NewGuid()
        };
        return new GenerationProcess
        {
            AdventureId = adventureId,
            Context = context.ToJsonString(),
            Step = step
        };
    }
}
