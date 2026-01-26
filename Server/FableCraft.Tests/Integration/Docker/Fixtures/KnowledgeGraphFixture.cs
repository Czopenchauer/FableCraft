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

    private readonly PostgreSqlContainer _postgres;

    public DockerClient Client { get; private set; } = null!;
    public IVolumeManager VolumeManager { get; private set; } = null!;
    public IContainerManager ContainerManager { get; private set; } = null!;
    public GraphServiceSettings GraphSettings { get; private set; } = null!;
    public IKnowledgeGraphContextService ContextService { get; private set; } = null!;
    private KnowledgeGraphContextService _contextServiceImpl = null!;
    public ApplicationDbContext DbContext { get; private set; } = null!;
    public Microsoft.Extensions.Configuration.IConfiguration Configuration { get; private set; } = null!;

    /// <summary>
    /// RagClient for verifying indexed data in tests.
    /// </summary>
    internal RagClient RagClient { get; private set; } = null!;

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
        // First load user secrets to read the nested keys
        var userSecretsConfig = new ConfigurationBuilder()
            .AddUserSecrets<KnowledgeGraphFixture>()
            .AddEnvironmentVariables()
            .Build();

        // Map nested user secret keys to flat keys expected by AddEnvVar
        var graphRag = userSecretsConfig.GetSection("FableCraft:GraphRag");
        var envVarMapping = new Dictionary<string, string?>
        {
            ["LLM_API_KEY"] = graphRag["LLM:ApiKey"],
            ["LLM_MODEL"] = graphRag["LLM:Model"],
            ["LLM_PROVIDER"] = graphRag["LLM:Provider"],
            ["LLM_MAX_TOKENS"] = graphRag["LLM:MaxTokens"],
            ["LLM_RATE_LIMIT_ENABLED"] = graphRag["LLM:RateLimitEnabled"],
            ["LLM_RATE_LIMIT_REQUESTS"] = graphRag["LLM:RateLimitRequests"],
            ["LLM_RATE_LIMIT_INTERVAL"] = graphRag["LLM:RateLimitInterval"],
            ["EMBEDDING_PROVIDER"] = graphRag["Embedding:Provider"],
            ["EMBEDDING_MODEL"] = graphRag["Embedding:Model"],
            ["EMBEDDING_API_KEY"] = graphRag["Embedding:ApiKey"],
            ["EMBEDDING_DIMENSIONS"] = graphRag["Embedding:Dimensions"],
            ["EMBEDDING_API_VERSION"] = graphRag["Embedding:ApiVersion"],
            ["HUGGINGFACE_TOKENIZER"] = graphRag["HuggingFaceTokenizer"]
        };

        // Filter out null/empty values and add to in-memory configuration
        var validMappings = envVarMapping
            .Where(kv => !string.IsNullOrEmpty(kv.Value))
            .Select(kv => new KeyValuePair<string, string?>(kv.Key, kv.Value));

        Configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(validMappings)
            .Build();

        // 9. Create RagClient with proper base URL for verification
        var ragHttpClient = new HttpClient
        {
            BaseAddress = new Uri($"http://localhost:{GraphSettings.Port}"),
            Timeout = TimeSpan.FromMinutes(5)
        };
        RagClient = new RagClient(ragHttpClient, new NoOpMessageDispatcher(), Log.Logger);

        var ragChunkService = new TestRagChunkService(RagClient, DbContext, GraphSettings.DataStorePath);

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
/// Test implementation of RagChunkService that actually commits data to the RAG API.
/// Writes files to the host data directory (mounted as /app/data-store in container).
/// </summary>
internal sealed class TestRagChunkService : IRagChunkService
{
    private const string ContainerDataStorePath = "/app/data-store";

    private readonly IRagBuilder _ragBuilder;
    private readonly ApplicationDbContext _dbContext;
    private readonly string _hostDataDirectory;

    public TestRagChunkService(IRagBuilder ragBuilder, ApplicationDbContext dbContext, string hostDataDirectory)
    {
        _ragBuilder = ragBuilder;
        _dbContext = dbContext;
        _hostDataDirectory = hostDataDirectory;
    }

    public async Task<List<Chunk>> CreateChunk(
        IEnumerable<ChunkCreationRequest> request,
        Guid adventureId,
        CancellationToken cancellationToken)
    {
        var adventureDir = Path.Combine(_hostDataDirectory, adventureId.ToString());
        if (!Directory.Exists(adventureDir))
        {
            Directory.CreateDirectory(adventureDir);
        }

        var chunks = new List<Chunk>();
        foreach (var creationRequest in request)
        {
            foreach (var datasetName in creationRequest.DatasetName)
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(creationRequest.Content);
                var hash = System.IO.Hashing.XxHash64.HashToUInt64(bytes);
                var name = $"{hash:x16}";
                var hostPath = Path.Combine(adventureDir, $"{name}.{creationRequest.ContentType}");

                // Write file to disk on host (required for RAG API via volume mount)
                await File.WriteAllTextAsync(hostPath, creationRequest.Content, cancellationToken);

                // Store container-relative path for sending to RAG API
                var containerPath = $"{ContainerDataStorePath}/{adventureId}/{name}.{creationRequest.ContentType}";

                var chunk = new Chunk
                {
                    EntityId = creationRequest.EntityId,
                    Name = name,
                    Path = containerPath, // Use container path, not host path
                    ContentType = creationRequest.ContentType.ToString(),
                    ContentHash = hash,
                    AdventureId = adventureId,
                    DatasetName = datasetName,
                    Content = creationRequest.Content,
                    KnowledgeGraphNodeId = null,
                };
                chunks.Add(chunk);
            }
        }

        return chunks;
    }

    public async Task CommitChunksToRagAsync(List<Chunk> chunks, CancellationToken cancellationToken)
    {
        var chunksByDataset = chunks.GroupBy(x => x.DatasetName);

        foreach (var datasetGroup in chunksByDataset)
        {
            var datasetName = datasetGroup.Key;
            // Paths are already container-relative from CreateChunk
            var filePaths = datasetGroup.Select(x => x.Path).Distinct().ToList();

            var addResult = await _ragBuilder.AddDataAsync(filePaths, [datasetName], cancellationToken);

            foreach (var item in datasetGroup)
            {
                if (addResult.TryGetValue(datasetName, out var datasetResults) &&
                    datasetResults.TryGetValue(item.Name, out var nodeId))
                {
                    item.KnowledgeGraphNodeId = nodeId;
                }
            }
        }
    }

    public async Task CognifyDatasetsAsync(string[] datasets, bool temporal = false, CancellationToken cancellationToken = default)
    {
        await _ragBuilder.CognifyAsync(datasets, temporal, cancellationToken);
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
