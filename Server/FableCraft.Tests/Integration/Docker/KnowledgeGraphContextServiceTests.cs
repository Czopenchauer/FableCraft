using FableCraft.Application.KnowledgeGraph;
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
        await Assert.That(await fixture.VolumeManager.ExistsAsync(
            fixture.GraphSettings.GetWorldbookVolumeName(worldbookId))).IsTrue();
    }

    [Test]
    public async Task InitializeAdventureAsync_CopiesWorldbookAndAddsCharacter()
    {
        var worldbookId = Guid.NewGuid();
        var adventureId = Guid.NewGuid();

        // First index worldbook
        await fixture.ContextService.IndexWorldbookAsync(worldbookId,
        [
            new LorebookIndexEntry(Guid.NewGuid(), "Test world content", "txt")
        ]);

        // Then initialize adventure
        var result = await fixture.ContextService.InitializeAdventureAsync(
            adventureId,
            worldbookId,
            new MainCharacterIndexEntry(Guid.NewGuid(), "Hero", "A brave adventurer"));

        await Assert.That(result.Success).IsTrue();
        await Assert.That(await fixture.VolumeManager.ExistsAsync(
            fixture.GraphSettings.GetAdventureVolumeName(adventureId))).IsTrue();
    }

    [Test]
    public async Task CommitSceneDataAsync_SwitchesBetweenAdventureVolumes()
    {
        var worldbookId = Guid.NewGuid();
        var adventure1 = Guid.NewGuid();
        var adventure2 = Guid.NewGuid();

        // Setup: index worldbook and initialize both adventures
        await fixture.ContextService.IndexWorldbookAsync(worldbookId,
        [
            new LorebookIndexEntry(Guid.NewGuid(), "Shared world lore", "txt")
        ]);
        await fixture.ContextService.InitializeAdventureAsync(adventure1, worldbookId,
            new MainCharacterIndexEntry(Guid.NewGuid(), "Hero1", "First hero"));
        await fixture.ContextService.InitializeAdventureAsync(adventure2, worldbookId,
            new MainCharacterIndexEntry(Guid.NewGuid(), "Hero2", "Second hero"));

        // Commit to different adventures alternately
        var commitResults = new List<bool>();

        var r1 = await fixture.ContextService.CommitSceneDataAsync(adventure1, _ =>
        {
            commitResults.Add(true);
            return Task.CompletedTask;
        });

        var r2 = await fixture.ContextService.CommitSceneDataAsync(adventure2, _ =>
        {
            commitResults.Add(true);
            return Task.CompletedTask;
        });

        var r3 = await fixture.ContextService.CommitSceneDataAsync(adventure1, _ =>
        {
            commitResults.Add(true);
            return Task.CompletedTask;
        });

        await Assert.That(r1.Success).IsTrue();
        await Assert.That(r2.Success).IsTrue();
        await Assert.That(r3.Success).IsTrue();
        await Assert.That(commitResults.Count).IsEqualTo(3);
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
        await task1Started.Task.WaitAsync(TimeSpan.FromMinutes(2));

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
