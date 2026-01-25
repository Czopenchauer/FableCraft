using Docker.DotNet;
using Docker.DotNet.Models;
using FableCraft.Application.KnowledgeGraph;
using FableCraft.Infrastructure;
using FableCraft.Infrastructure.Clients;
using FableCraft.Infrastructure.Docker;
using FableCraft.Infrastructure.Docker.Configuration;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities;
using FableCraft.Infrastructure.Queue;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Serilog;
using Testcontainers.PostgreSql;
using TUnit.Core.Interfaces;

namespace FableCraft.Tests.Integration.Docker.Fixtures;

/// <summary>
/// Full end-to-end fixture for KnowledgeGraphContextService tests.
/// Combines:
/// - PostgreSQL via Testcontainers (for RagChunkService)
/// - Docker operations (for volume switching)
/// - graph-rag-api container
/// </summary>
public class KnowledgeGraphFixture : IAsyncInitializer, IAsyncDisposable
{
    private const string TestPrefix = "fablecraft-test-";
    private const string TestNetwork = "fablecraft-test-network";

    private readonly List<string> _testVolumes = [];
    private readonly PostgreSqlContainer _postgres;

    public DockerClient Client { get; private set; } = null!;
    public IVolumeManager VolumeManager { get; private set; } = null!;
    public IContainerManager ContainerManager { get; private set; } = null!;
    public GraphServiceSettings GraphSettings { get; private set; } = null!;
    public IKnowledgeGraphContextService ContextService { get; private set; } = null!;
    private KnowledgeGraphContextService _contextServiceImpl = null!;
    public ApplicationDbContext DbContext { get; private set; } = null!;
    public Microsoft.Extensions.Configuration.IConfiguration Configuration { get; private set; } = null!;

    public KnowledgeGraphFixture()
    {
        _postgres = new PostgreSqlBuilder("postgres:16-alpine")
            .WithDatabase("fablecraft_test")
            .WithUsername("test")
            .WithPassword("test")
            .Build();
    }

    public async Task InitializeAsync()
    {
        // 1. Start PostgreSQL
        await _postgres.StartAsync();

        // 2. Create DbContext and run migrations
        var dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning))
            .Options;
        DbContext = new ApplicationDbContext(dbOptions);
        await DbContext.Database.MigrateAsync();

        // 3. Setup Docker client
        var socketPath = OperatingSystem.IsWindows()
            ? "npipe://./pipe/docker_engine"
            : "unix:///var/run/docker.sock";
        Client = new DockerClientConfiguration(new Uri(socketPath)).CreateClient();

        // 4. Pull required images
        await EnsureImagePulledAsync("alpine:latest");
        // fablecraft-graph-rag-api must be built locally - don't try to pull
        await EnsureImageExistsAsync("fablecraft-graph-rag-api:latest");

        // 5. Create test network
        await CreateTestNetworkAsync();

        // 6. Create managers
        var dockerSettings = Options.Create(new DockerSettings { UtilityImage = "alpine:latest" });
        VolumeManager = new VolumeManager(Client, dockerSettings, Log.Logger);

        var httpClientFactory = new TestHttpClientFactory();
        ContainerManager = new ContainerManager(Client, Log.Logger, httpClientFactory);

        // 7. Create GraphServiceSettings with test prefixes
        GraphSettings = new GraphServiceSettings
        {
            ImageName = "fablecraft-graph-rag-api:latest",
            ContainerName = $"{TestPrefix}graph-rag",
            NetworkName = TestNetwork,
            WorldbookVolumePrefix = $"{TestPrefix}kg-worldbook-",
            AdventureVolumePrefix = $"{TestPrefix}kg-adventure-",
            Port = 18111, // Different port to avoid conflicts
            HealthCheckHost = "localhost", // Use localhost for health checks from host machine
            HealthCheckTimeoutSeconds = 120,
            DataStorePath = Path.Combine(Path.GetTempPath(), $"fablecraft-test-{Guid.NewGuid():N}", "data-store"),
            VisualizationPath = Path.Combine(Path.GetTempPath(), $"fablecraft-test-{Guid.NewGuid():N}", "visualization")
        };

        // Create required directories
        Directory.CreateDirectory(GraphSettings.DataStorePath);
        Directory.CreateDirectory(GraphSettings.VisualizationPath);

        // 8. Build configuration with LLM credentials from user secrets
        Configuration = new ConfigurationBuilder()
            .AddUserSecrets<KnowledgeGraphFixture>()
            .AddEnvironmentVariables()
            .Build();

        // 9. Create RagChunkService dependencies
        var ragClient = new RagClient(
            httpClientFactory.CreateClient("default"),
            new NoOpMessageDispatcher(),
            Log.Logger);

        // Configure the HTTP client base address
        var httpClient = httpClientFactory.CreateClient("default");
        httpClient.BaseAddress = new Uri($"http://localhost:{GraphSettings.Port}");

        var ragChunkService = new TestRagChunkService(ragClient, DbContext);

        // 10. Create a service provider with IRagChunkService for the scope factory
        var services = new ServiceCollection();
        services.AddScoped<IRagChunkService>(_ => ragChunkService);
        var serviceProvider = services.BuildServiceProvider();
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

        // 11. Create KnowledgeGraphContextService
        _contextServiceImpl = new KnowledgeGraphContextService(
            VolumeManager,
            ContainerManager,
            scopeFactory,
            Options.Create(GraphSettings),
            Configuration,
            Log.Logger);
        ContextService = _contextServiceImpl;
    }

    public async ValueTask DisposeAsync()
    {
        // Stop context service (disposes the semaphore)
        _contextServiceImpl?.Dispose();

        // Remove test container
        if (ContainerManager is not null && GraphSettings is not null)
        {
            try
            {
                await ContainerManager.RemoveAsync(GraphSettings.ContainerName, force: true);
            }
            catch
            {
                // Ignore
            }
        }

        // Cleanup test volumes
        if (VolumeManager is not null)
        {
            try
            {
                var testVolumes = await VolumeManager.ListAsync(TestPrefix);
                foreach (var vol in testVolumes)
                {
                    try
                    {
                        await VolumeManager.DeleteAsync(vol.Name, force: true);
                    }
                    catch
                    {
                        // Ignore
                    }
                }
            }
            catch
            {
                // Ignore
            }
        }

        // Remove test network
        if (Client is not null)
        {
            try
            {
                await Client.Networks.DeleteNetworkAsync(TestNetwork);
            }
            catch
            {
                // Ignore
            }
        }

        // Cleanup temp directories
        if (GraphSettings is not null)
        {
            try
            {
                if (Directory.Exists(Path.GetDirectoryName(GraphSettings.DataStorePath)))
                    Directory.Delete(Path.GetDirectoryName(GraphSettings.DataStorePath)!, recursive: true);
            }
            catch
            {
                // Ignore
            }
        }

        // Dispose resources
        Client?.Dispose();
        if (DbContext is not null)
            await DbContext.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    public void TrackVolume(string volumeName) => _testVolumes.Add(volumeName);

    /// <summary>
    /// Gets container logs for debugging test failures.
    /// </summary>
    public async Task<string> GetContainerLogsAsync()
    {
        try
        {
            var containers = await Client.Containers.ListContainersAsync(new ContainersListParameters
            {
                All = true,
                Filters = new Dictionary<string, IDictionary<string, bool>>
                {
                    ["name"] = new Dictionary<string, bool> { [GraphSettings.ContainerName] = true }
                }
            });

            if (containers.Count == 0)
                return $"Container '{GraphSettings.ContainerName}' not found";

            var container = containers[0];
            var status = $"Container Status: {container.State} - {container.Status}\n";

            var logsStream = await Client.Containers.GetContainerLogsAsync(
                container.ID,
                new ContainerLogsParameters
                {
                    ShowStdout = true,
                    ShowStderr = true,
                    Tail = "100"
                });

            // MultiplexedStream needs special handling
            var memoryStream = new MemoryStream();
            await logsStream.CopyOutputToAsync(null, memoryStream, memoryStream, CancellationToken.None);
            memoryStream.Position = 0;
            using var reader = new StreamReader(memoryStream);
            var logs = await reader.ReadToEndAsync();

            return status + "Logs:\n" + logs;
        }
        catch (Exception ex)
        {
            return $"Failed to get container logs: {ex.Message}";
        }
    }

    /// <summary>
    /// Checks if container is running and returns its status.
    /// </summary>
    public async Task<string> GetContainerStatusAsync()
    {
        try
        {
            var status = await ContainerManager.GetStatusAsync(GraphSettings.ContainerName);
            if (status == null)
                return $"Container '{GraphSettings.ContainerName}' not found";

            return $"Container: {status.Name}, State: {status.State}, Running: {status.IsRunning}, Started: {status.StartedAt}";
        }
        catch (Exception ex)
        {
            return $"Failed to get container status: {ex.Message}";
        }
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

    private async Task EnsureImageExistsAsync(string image)
    {
        try
        {
            await Client.Images.InspectImageAsync(image);
        }
        catch (DockerImageNotFoundException)
        {
            throw new InvalidOperationException(
                $"Required Docker image '{image}' not found. Please build it locally first.");
        }
    }

    private async Task CreateTestNetworkAsync()
    {
        try
        {
            await Client.Networks.CreateNetworkAsync(new NetworksCreateParameters
            {
                Name = TestNetwork,
                Driver = "bridge"
            });
        }
        catch (DockerApiException ex) when (ex.Message.Contains("already exists"))
        {
            // OK, reuse existing
        }
    }
}

/// <summary>
/// Test implementation of RagChunkService that exposes the internal service.
/// </summary>
internal sealed class TestRagChunkService : IRagChunkService
{
    private readonly IRagBuilder _ragBuilder;
    private readonly ApplicationDbContext _dbContext;

    public TestRagChunkService(IRagBuilder ragBuilder, ApplicationDbContext dbContext)
    {
        _ragBuilder = ragBuilder;
        _dbContext = dbContext;
    }

    public Task<List<Infrastructure.Persistence.Entities.Chunk>> CreateChunk(
        IEnumerable<ChunkCreationRequest> request,
        Guid adventureId,
        CancellationToken cancellationToken)
    {
        // For testing, we don't actually create chunks
        return Task.FromResult(new List<Infrastructure.Persistence.Entities.Chunk>());
    }

    public Task CommitChunksToRagAsync(List<Infrastructure.Persistence.Entities.Chunk> chunks, CancellationToken cancellationToken)
    {
        // For testing, we don't actually commit
        return Task.CompletedTask;
    }

    public Task CognifyDatasetsAsync(string[] datasets, bool temporal = false, CancellationToken cancellationToken = default)
    {
        // For testing, we don't actually cognify
        return Task.CompletedTask;
    }
}

/// <summary>
/// No-op message dispatcher for tests.
/// </summary>
internal sealed class NoOpMessageDispatcher : IMessageDispatcher
{
    public Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : IMessage
    {
        return Task.CompletedTask;
    }
}
