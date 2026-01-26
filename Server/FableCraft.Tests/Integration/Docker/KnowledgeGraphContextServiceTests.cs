using FableCraft.Application.KnowledgeGraph;
using FableCraft.Infrastructure.Clients;
using FableCraft.Tests.Integration.Docker.Fixtures;

namespace FableCraft.Tests.Integration.Docker;

[ClassDataSource<KnowledgeGraphFixture>(Shared = SharedType.PerTestSession)]
public class KnowledgeGraphContextServiceTests(KnowledgeGraphFixture fixture)
{
    [Test]
    public async Task IndexWorldbookAsync_CreatesVolumeAndIndexesData()
    {
        var worldbookId = Guid.NewGuid();
        var entries = new LorebookIndexEntry[]
        {
            new(Guid.NewGuid(), "The kingdom of Eldoria is ancient.", "txt"),
            new(Guid.NewGuid(), "Dragons once roamed the mountains.", "txt")
        };

        var result = await fixture.ContextService.IndexWorldbookAsync(worldbookId, entries);

        await Assert.That(result.Success)
            .IsTrue()
            .Because($"IndexWorldbookAsync should succeed but failed with: {result.Error}");

        // Verify volume was created
        await Assert.That(await fixture.VolumeManager.ExistsAsync(
            fixture.GraphSettings.GetWorldbookVolumeName(worldbookId))).IsTrue();

        // Verify data was actually indexed by querying the RAG service
        var worldDataset = await fixture.RagClient.GetDatasetsAsync(RagClientExtensions.GetWorldDatasetName());
        await Assert.That(worldDataset.Count)
            .IsGreaterThan(0)
            .Because("World dataset should contain indexed lore entries");
    }

    [Test]
    public async Task InitializeAdventureAsync_CopiesWorldbookAndAddsCharacter()
    {
        var worldbookId = Guid.NewGuid();
        var adventureId = Guid.NewGuid();
        var mainCharacterId = Guid.NewGuid();

        // First index worldbook
        var indexResult = await fixture.ContextService.IndexWorldbookAsync(worldbookId,
        [
            new LorebookIndexEntry(Guid.NewGuid(), "Test world content for adventure init", "txt")
        ]);
        await Assert.That(indexResult.Success)
            .IsTrue()
            .Because($"IndexWorldbookAsync should succeed but failed with: {indexResult.Error}");

        // Then initialize adventure
        var result = await fixture.ContextService.InitializeAdventureAsync(
            adventureId,
            worldbookId,
            new MainCharacterIndexEntry(mainCharacterId, "TestHero", "A brave adventurer for testing"));

        await Assert.That(result.Success)
            .IsTrue()
            .Because($"InitializeAdventureAsync should succeed but failed with: {result.Error}");

        // Verify adventure volume was created
        await Assert.That(await fixture.VolumeManager.ExistsAsync(
            fixture.GraphSettings.GetAdventureVolumeName(adventureId))).IsTrue();

        // Verify main character was indexed
        var mainCharDataset = await fixture.RagClient.GetDatasetsAsync(RagClientExtensions.GetMainCharacterDatasetName());
        await Assert.That(mainCharDataset.Count)
            .IsGreaterThan(0)
            .Because("Main character dataset should contain the indexed character");
    }

    [Test]
    public async Task CommitSceneDataAsync_SwitchesBetweenAdventureVolumes()
    {
        var worldbookId = Guid.NewGuid();
        var adventure1 = Guid.NewGuid();
        var adventure2 = Guid.NewGuid();

        // Setup: index worldbook
        var indexResult = await fixture.ContextService.IndexWorldbookAsync(worldbookId,
        [
            new LorebookIndexEntry(Guid.NewGuid(), "Shared world lore for volume switching test", "txt")
        ]);
        await Assert.That(indexResult.Success)
            .IsTrue()
            .Because($"IndexWorldbookAsync should succeed but failed with: {indexResult.Error}");

        // Initialize both adventures
        var init1 = await fixture.ContextService.InitializeAdventureAsync(adventure1, worldbookId,
            new MainCharacterIndexEntry(Guid.NewGuid(), "Hero1", "First hero"));
        await Assert.That(init1.Success)
            .IsTrue()
            .Because($"InitializeAdventureAsync for adventure1 failed with: {init1.Error}");

        var init2 = await fixture.ContextService.InitializeAdventureAsync(adventure2, worldbookId,
            new MainCharacterIndexEntry(Guid.NewGuid(), "Hero2", "Second hero"));
        await Assert.That(init2.Success)
            .IsTrue()
            .Because($"InitializeAdventureAsync for adventure2 failed with: {init2.Error}");

        // Commit to different adventures alternately - this tests volume switching
        // The test verifies that switching between volumes works correctly
        // Data verification is done in other tests (IndexWorldbook, InitializeAdventure)
        var callbackExecuted = new List<Guid>();

        var r1 = await fixture.ContextService.CommitSceneDataAsync(adventure1, _ =>
        {
            callbackExecuted.Add(adventure1);
            return Task.CompletedTask;
        });

        var r2 = await fixture.ContextService.CommitSceneDataAsync(adventure2, _ =>
        {
            callbackExecuted.Add(adventure2);
            return Task.CompletedTask;
        });

        var r3 = await fixture.ContextService.CommitSceneDataAsync(adventure1, _ =>
        {
            // Back to adventure1 - verify volume switch back works
            callbackExecuted.Add(adventure1);
            return Task.CompletedTask;
        });

        await Assert.That(r1.Success)
            .IsTrue()
            .Because($"CommitSceneDataAsync for adventure1 (first) failed with: {r1.Error}");
        await Assert.That(r2.Success)
            .IsTrue()
            .Because($"CommitSceneDataAsync for adventure2 failed with: {r2.Error}");
        await Assert.That(r3.Success)
            .IsTrue()
            .Because($"CommitSceneDataAsync for adventure1 (second) failed with: {r3.Error}");

        // Verify all callbacks were executed in the expected order
        await Assert.That(callbackExecuted).IsEquivalentTo([adventure1, adventure2, adventure1]);
    }

    [Test]
    public async Task ConcurrentOperations_AreSerializedByLock()
    {
        var worldbookId = Guid.NewGuid();
        var adventureId = Guid.NewGuid();

        await fixture.ContextService.IndexWorldbookAsync(worldbookId,
        [
            new LorebookIndexEntry(Guid.NewGuid(), "Test content", "txt")
        ]);
        await fixture.ContextService.InitializeAdventureAsync(adventureId, worldbookId,
            new MainCharacterIndexEntry(Guid.NewGuid(), "Hero", "Test hero"));

        var executionOrder = new List<int>();
        var task1Started = new TaskCompletionSource();
        var syncPoint = new TaskCompletionSource();

        // First operation will block after signaling it started
        var task1 = fixture.ContextService.CommitSceneDataAsync(adventureId, async _ =>
        {
            executionOrder.Add(1);
            task1Started.SetResult(); // Signal that task1 callback is executing
            await syncPoint.Task;
        });

        // Wait for task1 to actually start its callback (not just be queued)
        // Timeout is longer because container recreation and volume switching takes time
        await task1Started.Task.WaitAsync(TimeSpan.FromMinutes(5));

        // Second operation should wait for lock
        var task2 = fixture.ContextService.CommitSceneDataAsync(adventureId, _ =>
        {
            executionOrder.Add(2);
            return Task.CompletedTask;
        });

        // Give task2 time to queue up (it should be blocked on the lock)
        await Task.Delay(100);

        // Verify task1 started but task2 hasn't executed yet (blocked on lock)
        await Assert.That(executionOrder.Count).IsEqualTo(1);

        // Release task1
        syncPoint.SetResult();
        await Task.WhenAll(task1, task2);

        // Verify order: 1 then 2
        await Assert.That(executionOrder).IsEquivalentTo([1, 2]);
    }
}
