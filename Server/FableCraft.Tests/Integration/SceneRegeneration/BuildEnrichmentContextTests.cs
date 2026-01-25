using FableCraft.Application.NarrativeEngine;
using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities;
using FableCraft.Tests.Integration.SceneRegeneration.Fixtures;

namespace FableCraft.Tests.Integration.SceneRegeneration;

[ClassDataSource<PostgresContainerFixture>(Shared = SharedType.PerTestSession)]
public class BuildEnrichmentContextTests(PostgresContainerFixture fixture)
{
    private IGenerationContextBuilder CreateBuilder()
    {
        var db = fixture.CreateDbContext();
        return new GenerationContextBuilder(db);
    }

    [Test]
    public async Task ReturnsContextFromStoredGenerationProcess()
    {
        await using var db = fixture.CreateDbContext();
        var builder = CreateBuilder();

        var adventure = TestData.CreateAdventure();
        var scene = TestData.CreateScene(adventure.Id, 1, selectedAction: "Look around");
        adventure.Scenes.Add(scene);

        var storedSceneId = Guid.NewGuid();
        var generationProcess = CreateGenerationProcess(adventure.Id, "Test action", storedSceneId);

        db.Adventures.Add(adventure);
        db.GenerationProcesses.Add(generationProcess);
        await db.SaveChangesAsync();

        var context = await builder.BuildEnrichmentContextAsync(adventure.Id, CancellationToken.None);

        await Assert.That(context.AdventureId).IsEqualTo(adventure.Id);
        await Assert.That(context.PlayerAction).IsEqualTo("Test action");
        await Assert.That(context.NewSceneId).IsEqualTo(storedSceneId);
    }

    [Test]
    public async Task IncludesPreviousScenesSkippingMostRecent()
    {
        await using var db = fixture.CreateDbContext();
        var builder = CreateBuilder();

        var adventure = TestData.CreateAdventure();
        for (var i = 1; i <= 5; i++)
        {
            adventure.Scenes.Add(TestData.CreateScene(adventure.Id, i, selectedAction: $"Action {i}"));
        }

        db.Adventures.Add(adventure);
        db.GenerationProcesses.Add(CreateGenerationProcess(adventure.Id));
        await db.SaveChangesAsync();

        var context = await builder.BuildEnrichmentContextAsync(adventure.Id, CancellationToken.None);

        // Should have 4 scenes (5 total - 1 skipped most recent)
        await Assert.That(context.SceneContext).Count().IsEqualTo(4);
        // Most recent scene (sequence 5) should be excluded - it's being enriched
        await Assert.That(context.SceneContext.Any(s => s.SequenceNumber == 5)).IsFalse();
        await Assert.That(context.SceneContext.Max(s => s.SequenceNumber)).IsEqualTo(4);
    }

    [Test]
    public async Task LimitsTo20PreviousScenesAfterSkipping()
    {
        await using var db = fixture.CreateDbContext();
        var builder = CreateBuilder();

        var adventure = TestData.CreateAdventure();
        for (var i = 1; i <= 25; i++)
        {
            adventure.Scenes.Add(TestData.CreateScene(adventure.Id, i, selectedAction: $"Action {i}"));
        }

        db.Adventures.Add(adventure);
        db.GenerationProcesses.Add(CreateGenerationProcess(adventure.Id));
        await db.SaveChangesAsync();

        var context = await builder.BuildEnrichmentContextAsync(adventure.Id, CancellationToken.None);

        // Takes 20 most recent, then skips 1, so 19 scenes
        await Assert.That(context.SceneContext).Count().IsEqualTo(19);
        // Most recent (25) is skipped since it's being enriched
        await Assert.That(context.SceneContext.Max(s => s.SequenceNumber)).IsEqualTo(24);
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

        var character = TestData.CreateCharacter(adventure.Id, scene1, "Elena", description: "A skilled archer");
        character.CharacterStates.Add(TestData.CreateCharacterState(character.Id, scene1, 1, "Elena - after scene 1"));
        character.CharacterStates.Add(TestData.CreateCharacterState(character.Id, scene2, 2, "Elena - after scene 2"));
        character.CharacterMemories.Add(TestData.CreateCharacterMemory(character.Id, scene1.Id, "Saw a dragon"));
        adventure.Characters.Add(character);

        db.Adventures.Add(adventure);
        db.GenerationProcesses.Add(CreateGenerationProcess(adventure.Id));
        await db.SaveChangesAsync();

        var context = await builder.BuildEnrichmentContextAsync(adventure.Id, CancellationToken.None);

        await Assert.That(context.Characters).Count().IsEqualTo(1);
        var elena = context.Characters.Single();
        await Assert.That(elena.Name).IsEqualTo("Elena");
        await Assert.That(elena.Description).IsEqualTo("A skilled archer");
        // Should have the latest state (from scene 2)
        await Assert.That(elena.CharacterState.Name).IsEqualTo("Elena - after scene 2");
        // Should include memories
        await Assert.That(elena.CharacterMemories).Count().IsEqualTo(1);
    }

    [Test]
    public async Task Characters_IncludeLatestRelationshipPerTarget()
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
        // Two relationships with same target - should get latest
        character.CharacterRelationships.Add(TestData.CreateCharacterRelationship(character.Id, scene1.Id, 1, "Hero", "Stranger"));
        character.CharacterRelationships.Add(TestData.CreateCharacterRelationship(character.Id, scene2.Id, 2, "Hero", "Ally"));
        adventure.Characters.Add(character);

        db.Adventures.Add(adventure);
        db.GenerationProcesses.Add(CreateGenerationProcess(adventure.Id));
        await db.SaveChangesAsync();

        var context = await builder.BuildEnrichmentContextAsync(adventure.Id, CancellationToken.None);

        var elena = context.Characters.Single();
        await Assert.That(elena.Relationships).Count().IsEqualTo(1);
        // Should have the latest relationship (sequence 2)
        await Assert.That(elena.Relationships.Single().Dynamic.ToString()).IsEqualTo("Ally");
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
        db.GenerationProcesses.Add(CreateGenerationProcess(adventure.Id));
        await db.SaveChangesAsync();

        var context = await builder.BuildEnrichmentContextAsync(adventure.Id, CancellationToken.None);

        // Should get lore from the most recent scene only
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
        db.GenerationProcesses.Add(CreateGenerationProcess(adventure.Id));
        await db.SaveChangesAsync();

        var context = await builder.BuildEnrichmentContextAsync(adventure.Id, CancellationToken.None);

        await Assert.That(context.PromptPath).IsEqualTo("CustomPrompts");
        await Assert.That(context.AdventureStartTime).IsEqualTo("08:00 15-03-845");
        await Assert.That(context.MainCharacter).IsNotNull();
        await Assert.That(context.TrackerStructure).IsNotNull();
    }

    private static GenerationProcess CreateGenerationProcess(
        Guid adventureId,
        string playerAction = "Test action",
        Guid? newSceneId = null)
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
            Step = GenerationProcessStep.GeneratingScene
        };
    }
}
