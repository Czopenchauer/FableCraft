using Docker.DotNet;
using Docker.DotNet.Models;
using FableCraft.Infrastructure.Docker;
using FableCraft.Infrastructure.Docker.Configuration;
using Microsoft.Extensions.Options;
using Serilog;
using TUnit.Core.Interfaces;

namespace FableCraft.Tests.Integration.Docker.Fixtures;

/// <summary>
/// Shared fixture for Docker volume and container tests.
/// Provides Docker client, VolumeManager, and ContainerManager with automatic cleanup.
/// </summary>
public class DockerFixture : IAsyncInitializer, IAsyncDisposable
{
    private const string TestPrefix = "fablecraft-test-";
    private readonly List<string> _testVolumes = [];
    private readonly List<string> _testContainers = [];

    public DockerClient Client { get; private set; } = null!;
    public IVolumeManager VolumeManager { get; private set; } = null!;
    public IContainerManager ContainerManager { get; private set; } = null!;
    internal DockerSettings DockerSettings { get; private set; } = null!;

    /// <summary>
    /// Utility image for test operations. Exposed as public for test helpers.
    /// </summary>
    public string UtilityImage => DockerSettings.UtilityImage;

    public async Task InitializeAsync()
    {
        var socketPath = OperatingSystem.IsWindows()
            ? "npipe://./pipe/docker_engine"
            : "unix:///var/run/docker.sock";

        Client = new DockerClientConfiguration(new Uri(socketPath)).CreateClient();

        // Verify Docker is running
        await Client.System.PingAsync();

        // Setup settings and pull utility images
        DockerSettings = new DockerSettings { UtilityImage = "alpine:latest" };
        await EnsureImagePulledAsync("alpine:latest");
        await EnsureImagePulledAsync("nginx:alpine");

        var settingsOptions = Options.Create(DockerSettings);
        VolumeManager = new VolumeManager(Client, settingsOptions, Log.Logger);

        var httpClientFactory = new TestHttpClientFactory();
        ContainerManager = new ContainerManager(Client, Log.Logger, httpClientFactory);
    }

    public async ValueTask DisposeAsync()
    {
        // Cleanup test containers first (they might be using volumes)
        foreach (var container in _testContainers)
        {
            try
            {
                await ContainerManager.RemoveAsync(container, force: true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        // Cleanup test volumes
        foreach (var volume in _testVolumes)
        {
            try
            {
                await VolumeManager.DeleteAsync(volume, force: true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        Client.Dispose();
    }

    /// <summary>
    /// Creates a unique test volume name and registers it for cleanup.
    /// </summary>
    public string CreateTestVolumeName()
    {
        var name = $"{TestPrefix}vol-{Guid.NewGuid():N}";
        _testVolumes.Add(name);
        return name;
    }

    /// <summary>
    /// Creates a unique test container name and registers it for cleanup.
    /// </summary>
    public string CreateTestContainerName()
    {
        var name = $"{TestPrefix}ctr-{Guid.NewGuid():N}";
        _testContainers.Add(name);
        return name;
    }

    private async Task EnsureImagePulledAsync(string image)
    {
        try
        {
            await Client.Images.InspectImageAsync(image);
        }
        catch (DockerImageNotFoundException)
        {
            await Client.Images.CreateImageAsync(
                new ImagesCreateParameters { FromImage = image },
                null,
                new Progress<JSONMessage>());
        }
    }
}

internal class TestHttpClientFactory : IHttpClientFactory
{
    public HttpClient CreateClient(string name) => new();
}
