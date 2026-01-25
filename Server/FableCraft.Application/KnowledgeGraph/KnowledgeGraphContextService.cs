using FableCraft.Infrastructure;
using FableCraft.Infrastructure.Clients;
using FableCraft.Infrastructure.Docker;
using FableCraft.Infrastructure.Docker.Configuration;
using FableCraft.Infrastructure.Persistence.Entities;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

using Serilog;

namespace FableCraft.Application.KnowledgeGraph;

/// <summary>
/// Manages knowledge graph operations with volume-based isolation.
///
/// Thread-safety: All mutating operations acquire an exclusive lock for their
/// entire duration. Only ONE operation can run at a time because the underlying
/// graph service container can only mount one volume at a time.
/// </summary>
internal sealed class KnowledgeGraphContextService : IKnowledgeGraphContextService, IDisposable
{
    private readonly IVolumeManager _volumeManager;
    private readonly IContainerManager _containerManager;
    private readonly IRagChunkService _ragChunkService;
    private readonly GraphServiceSettings _settings;
    private readonly IConfiguration _configuration;
    private readonly ILogger _logger;

    /// <summary>
    /// Exclusive lock for ALL knowledge graph operations.
    /// Only one operation can run at a time.
    /// </summary>
    private readonly SemaphoreSlim _operationLock = new(1, 1);

    /// <summary>
    /// Tracks which volume is currently mounted (if any).
    /// Only accessed while holding _operationLock.
    /// </summary>
    private string? _currentlyMountedVolume;

    public KnowledgeGraphContextService(
        IVolumeManager volumeManager,
        IContainerManager containerManager,
        IRagChunkService ragChunkService,
        IOptions<GraphServiceSettings> settings,
        IConfiguration configuration,
        ILogger logger)
    {
        _volumeManager = volumeManager;
        _containerManager = containerManager;
        _ragChunkService = ragChunkService;
        _settings = settings.Value;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<IndexingResult> IndexWorldbookAsync(
        Guid worldbookId,
        IReadOnlyList<LorebookIndexEntry> lorebookEntries,
        CancellationToken ct = default)
    {
        if (lorebookEntries.Count == 0)
        {
            return new IndexingResult(false, "No lorebook entries to index.");
        }

        var volumeName = _settings.GetWorldbookVolumeName(worldbookId);

        _logger.Information(
            "Starting worldbook indexing for {WorldbookId} with {EntryCount} entries",
            worldbookId,
            lorebookEntries.Count);

        await _operationLock.WaitAsync(ct);
        try
        {
            if (await _volumeManager.ExistsAsync(volumeName, ct))
            {
                _logger.Information("Deleting existing worldbook template {WorldbookId}", worldbookId);
                await _volumeManager.DeleteAsync(volumeName, force: true, ct);
            }

            await _volumeManager.CreateAsync(volumeName, ct);

            await EnsureContainerWithVolumeAsync(volumeName, ct);

            var chunkRequests = lorebookEntries
                .Select(e => new ChunkCreationRequest(
                    e.Id,
                    e.Content,
                    Enum.Parse<ContentType>(e.ContentType, ignoreCase: true),
                    [RagClientExtensions.GetWorldDatasetName()]))
                .ToList();

            var chunks = await _ragChunkService.CreateChunk(chunkRequests, worldbookId, ct);
            await _ragChunkService.CommitChunksToRagAsync(chunks, ct);
            await _ragChunkService.CognifyDatasetsAsync([RagClientExtensions.GetWorldDatasetName()], cancellationToken: ct);

            _logger.Information(
                "Successfully indexed worldbook {WorldbookId} with {ChunkCount} chunks",
                worldbookId,
                chunks.Count);

            return new IndexingResult(true);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to index worldbook {WorldbookId}", worldbookId);

            return new IndexingResult(false, ex.Message);
        }
        finally
        {
            _operationLock.Release();
        }
    }

    public async Task<IndexingResult> InitializeAdventureAsync(
        Guid adventureId,
        Guid worldbookId,
        MainCharacterIndexEntry mainCharacter,
        CancellationToken ct = default)
    {
        var sourceVolume = _settings.GetWorldbookVolumeName(worldbookId);
        var destVolume = _settings.GetAdventureVolumeName(adventureId);

        _logger.Information(
            "Initializing adventure {AdventureId} from worldbook {WorldbookId}",
            adventureId,
            worldbookId);

        await _operationLock.WaitAsync(ct);
        try
        {
            if (!await _volumeManager.ExistsAsync(sourceVolume, ct))
            {
                return new IndexingResult(false,
                    $"Worldbook {worldbookId} has not been indexed. Run indexing first.");
            }

            if (await _volumeManager.ExistsAsync(destVolume, ct))
            {
                return new IndexingResult(false,
                    $"Adventure {adventureId} already has a knowledge graph volume.");
            }

            await _volumeManager.CopyAsync(sourceVolume, destVolume, ct);

            await EnsureContainerWithVolumeAsync(destVolume, ct);

            string[] datasets = [RagClientExtensions.GetMainCharacterDatasetName(), RagClientExtensions.GetWorldDatasetName()];
            var chunkRequests = new List<ChunkCreationRequest>
            {
                new(mainCharacter.Id,
                    FormatMainCharacterDescription(mainCharacter),
                    ContentType.txt,
                    datasets)
            };

            var chunks = await _ragChunkService.CreateChunk(chunkRequests, adventureId, ct);
            await _ragChunkService.CommitChunksToRagAsync(chunks, ct);
            await _ragChunkService.CognifyDatasetsAsync(datasets, cancellationToken: ct);

            _logger.Information(
                "Successfully initialized adventure {AdventureId} from worldbook {WorldbookId}",
                adventureId,
                worldbookId);

            return new IndexingResult(true);
        }
        catch (Exception ex)
        {
            _logger.Error(ex,
                "Failed to initialize adventure {AdventureId} from worldbook {WorldbookId}",
                adventureId,
                worldbookId);

            return new IndexingResult(false, ex.Message);
        }
        finally
        {
            _operationLock.Release();
        }
    }

    public async Task<IndexingResult> CommitSceneDataAsync(
        Guid adventureId,
        Func<CancellationToken, Task> commitAction,
        CancellationToken ct = default)
    {
        var volumeName = _settings.GetAdventureVolumeName(adventureId);

        _logger.Information("Committing scene data for adventure {AdventureId}", adventureId);

        await _operationLock.WaitAsync(ct);
        try
        {
            if (!await _volumeManager.ExistsAsync(volumeName, ct))
            {
                return new IndexingResult(false,
                    $"Adventure {adventureId} does not have a knowledge graph volume.");
            }

            await EnsureContainerWithVolumeAsync(volumeName, ct);

            await commitAction(ct);

            _logger.Information("Successfully committed scene data for adventure {AdventureId}", adventureId);
            return new IndexingResult(true);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to commit scene data for adventure {AdventureId}", adventureId);
            return new IndexingResult(false, ex.Message);
        }
        finally
        {
            _operationLock.Release();
        }
    }

    public async Task<bool> IsWorldbookIndexedAsync(Guid worldbookId, CancellationToken ct = default)
    {
        var volumeName = _settings.GetWorldbookVolumeName(worldbookId);
        return await _volumeManager.ExistsAsync(volumeName, ct);
    }

    public async Task DeleteAdventureVolumeAsync(Guid adventureId, CancellationToken ct = default)
    {
        var volumeName = _settings.GetAdventureVolumeName(adventureId);

        await _operationLock.WaitAsync(ct);
        try
        {
            if (_currentlyMountedVolume == volumeName)
            {
                _currentlyMountedVolume = null;
            }

            await _volumeManager.DeleteAsync(volumeName, force: true, ct);
            _logger.Information("Deleted adventure volume {AdventureId}", adventureId);
        }
        finally
        {
            _operationLock.Release();
        }
    }

    public async Task DeleteWorldbookVolumeAsync(Guid worldbookId, CancellationToken ct = default)
    {
        var volumeName = _settings.GetWorldbookVolumeName(worldbookId);

        await _operationLock.WaitAsync(ct);
        try
        {
            if (_currentlyMountedVolume == volumeName)
            {
                _currentlyMountedVolume = null;
            }

            await _volumeManager.DeleteAsync(volumeName, force: true, ct);
            _logger.Information("Deleted worldbook volume {WorldbookId}", worldbookId);
        }
        finally
        {
            _operationLock.Release();
        }
    }

    /// <summary>
    /// Ensures the container is running with the specified volume mounted.
    /// Only call while holding _operationLock.
    /// </summary>
    private async Task EnsureContainerWithVolumeAsync(string volumeName, CancellationToken ct)
    {
        if (_currentlyMountedVolume == volumeName)
        {
            var status = await _containerManager.GetStatusAsync(_settings.ContainerName, ct);
            if (status?.IsRunning == true)
            {
                return;
            }
        }

        var config = BuildContainerConfig(volumeName);
        await _containerManager.RecreateAsync(config, ct);

        var healthUrl = BuildHealthUrl();
        var timeout = TimeSpan.FromSeconds(_settings.HealthCheckTimeoutSeconds);
        await _containerManager.WaitForHealthyAsync(_settings.ContainerName, healthUrl, timeout, ct);

        _currentlyMountedVolume = volumeName;
    }

    private ContainerConfig BuildContainerConfig(string volumeName)
    {
        var environment = new Dictionary<string, string>();

        AddEnvVar(environment, "LLM_API_KEY", "LLM_API_KEY");
        AddEnvVar(environment, "LLM_MODEL", "LLM_MODEL", "gpt-4");
        AddEnvVar(environment, "LLM_PROVIDER", "LLM_PROVIDER", "openai");
        AddEnvVar(environment, "LLM_ENDPOINT", "LLM_ENDPOINT");
        AddEnvVar(environment, "LLM_API_VERSION", "LLM_API_VERSION");
        AddEnvVar(environment, "LLM_MAX_TOKENS", "LLM_MAX_TOKENS", "16384");
        AddEnvVar(environment, "LLM_RATE_LIMIT_ENABLED", "LLM_RATE_LIMIT_ENABLED", "false");
        AddEnvVar(environment, "LLM_RATE_LIMIT_REQUESTS", "LLM_RATE_LIMIT_REQUESTS", "60");
        AddEnvVar(environment, "LLM_RATE_LIMIT_INTERVAL", "LLM_RATE_LIMIT_INTERVAL", "60");

        AddEnvVar(environment, "EMBEDDING_PROVIDER", "EMBEDDING_PROVIDER", "openai");
        AddEnvVar(environment, "EMBEDDING_MODEL", "EMBEDDING_MODEL", "text-embedding-3-large");
        AddEnvVar(environment, "EMBEDDING_ENDPOINT", "EMBEDDING_ENDPOINT");
        AddEnvVar(environment, "EMBEDDING_API_VERSION", "EMBEDDING_API_VERSION");
        AddEnvVar(environment, "EMBEDDING_DIMENSIONS", "EMBEDDING_DIMENSIONS", "3072");
        AddEnvVar(environment, "EMBEDDING_MAX_TOKENS", "EMBEDDING_MAX_TOKENS", "8191");
        AddEnvVar(environment, "EMBEDDING_BATCH_SIZE", "EMBEDDING_BATCH_SIZE", "36");
        AddEnvVar(environment, "HUGGINGFACE_TOKENIZER", "HUGGINGFACE_TOKENIZER");

        return new ContainerConfig
        {
            Name = _settings.ContainerName,
            Image = _settings.ImageName,
            Environment = environment,
            Volumes =
            [
                $"{volumeName}:{_settings.VolumeMountPath}",
                $"{_settings.VisualizationPath}:/app/visualization",
                $"{_settings.DataStorePath}:/app/data-store"
            ],
            Ports = [$"{_settings.Port}:{_settings.Port}"],
            NetworkName = _settings.NetworkName,
            Labels = new Dictionary<string, string>
            {
                ["fablecraft.managed"] = "true",
                ["fablecraft.service"] = "knowledge-graph"
            }
        };
    }

    private string BuildHealthUrl()
    {
        return $"http://{_settings.ContainerName}:{_settings.Port}{_settings.HealthEndpoint}";
    }

    private void AddEnvVar(Dictionary<string, string> env, string key, string envVarName, string? defaultValue = null)
    {
        var value = _configuration[key]
                    ?? Environment.GetEnvironmentVariable(envVarName)
                    ?? defaultValue;

        if (!string.IsNullOrEmpty(value))
        {
            env[key] = value;
        }
    }

    private static string FormatMainCharacterDescription(MainCharacterIndexEntry mc) =>
        $"Name: {mc.Name}\n\n{mc.Description}";

    public void Dispose()
    {
        _operationLock.Dispose();
    }
}