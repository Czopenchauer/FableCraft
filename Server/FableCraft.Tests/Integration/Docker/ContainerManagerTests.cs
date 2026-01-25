using FableCraft.Infrastructure.Docker;
using FableCraft.Tests.Integration.Docker.Fixtures;

namespace FableCraft.Tests.Integration.Docker;

[ClassDataSource<DockerFixture>(Shared = SharedType.PerTestSession)]
public class ContainerManagerTests(DockerFixture fixture)
{
    [Test]
    public async Task RecreateAsync_CreatesAndStartsContainer()
    {
        var containerName = fixture.CreateTestContainerName();
        var config = new ContainerConfig
        {
            Name = containerName,
            Image = "nginx:alpine",
            Ports = ["18080:80"]
        };

        var containerId = await fixture.ContainerManager.RecreateAsync(config);

        var status = await fixture.ContainerManager.GetStatusAsync(containerName);
        await Assert.That(status).IsNotNull();
        await Assert.That(status!.IsRunning).IsTrue();
        await Assert.That(status.Id).IsEqualTo(containerId);
    }

    [Test]
    public async Task RecreateAsync_StopsAndRemovesExisting()
    {
        var containerName = fixture.CreateTestContainerName();
        var config = new ContainerConfig
        {
            Name = containerName,
            Image = "nginx:alpine",
            Ports = ["18081:80"]
        };

        var id1 = await fixture.ContainerManager.RecreateAsync(config);
        var id2 = await fixture.ContainerManager.RecreateAsync(config);

        await Assert.That(id2).IsNotEqualTo(id1);

        var status = await fixture.ContainerManager.GetStatusAsync(containerName);
        await Assert.That(status!.Id).IsEqualTo(id2);
    }

    [Test]
    public async Task WaitForHealthyAsync_SucceedsWhenEndpointResponds()
    {
        var containerName = fixture.CreateTestContainerName();
        var config = new ContainerConfig
        {
            Name = containerName,
            Image = "nginx:alpine",
            Ports = ["18082:80"]
        };

        await fixture.ContainerManager.RecreateAsync(config);

        // nginx responds on / with 200
        await fixture.ContainerManager.WaitForHealthyAsync(
            containerName,
            "http://localhost:18082/",
            TimeSpan.FromSeconds(30));

        // If we get here without timeout, test passes
    }

    [Test]
    public async Task WaitForHealthyAsync_ThrowsTimeoutIfNeverHealthy()
    {
        var containerName = fixture.CreateTestContainerName();
        var config = new ContainerConfig
        {
            Name = containerName,
            Image = "nginx:alpine",
            Ports = ["18083:80"]
        };

        await fixture.ContainerManager.RecreateAsync(config);

        // Bad endpoint that will never respond with 200
        // WaitForHealthyAsync throws TimeoutException, but the underlying cancellation
        // may surface as TaskCanceledException or OperationCanceledException
        var exception = await Assert.ThrowsAsync<Exception>(async () =>
        {
            await fixture.ContainerManager.WaitForHealthyAsync(
                containerName,
                "http://localhost:18083/nonexistent-health-endpoint",
                TimeSpan.FromSeconds(3));
        });

        await Assert.That(exception is TimeoutException or TaskCanceledException or OperationCanceledException).IsTrue();
    }
}
