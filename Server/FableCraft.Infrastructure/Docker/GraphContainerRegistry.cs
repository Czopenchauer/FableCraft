using System.Collections.Concurrent;

using FableCraft.Infrastructure.Docker.Configuration;
using FableCraft.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

using Serilog;

namespace FableCraft.Infrastructure.Docker;

/// <summary>
/// Manages multiple graph service containers, one per adventure.
/// Handles port allocation, LRU eviction, and container lifecycle.
/// </summary>
internal interface IGraphContainerRegistry
{
    /// <summary>
    /// Gets or creates a container for the adventure.
    /// Returns the base URL for the container (e.g., "http://localhost:8112").
    /// </summary>
    Task<string> EnsureAdventureContainerRunningAsync(Guid adventureId, CancellationToken ct);

    /// <summary>
    /// Gets or creates a container for the worldbook.
    /// Returns the base URL for the container (e.g., "http://localhost:8112").
    /// </summary>
    Task<string> EnsureWorldbookContainerRunningAsync(Guid worldbookId, CancellationToken ct);
}

internal interface IContainerMonitor
{
    void Increment(Guid? adventureIdValue);

    void Decrement(Guid? adventureIdValue);
}

internal sealed class GraphContainerRegistry : IGraphContainerRegistry, IContainerMonitor, IAsyncDisposable
{
    private readonly ConcurrentDictionary<Guid, ContainerInfo> _containers = new();
    private readonly ContainerManager _containerManager;
    private readonly IVolumeManager _volumeManager;
    private readonly IConfiguration _config;
    private readonly GraphServiceSettings _settings;
    private readonly ILogger _logger;
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _operationLocks = new();
    private readonly ApplicationDbContext _dbContext;

    public GraphContainerRegistry(
        ContainerManager containerManager,
        IOptions<GraphServiceSettings> settings,
        IConfiguration config,
        ILogger logger, ApplicationDbContext dbContext, IVolumeManager volumeManager)
    {
        _containerManager = containerManager;
        _config = config;
        _settings = settings.Value;
        _logger = logger;
        _dbContext = dbContext;
        _volumeManager = volumeManager;
    }

    public async Task<string> EnsureAdventureContainerRunningAsync(Guid adventureId, CancellationToken ct)
    {
        var adventureName = await _dbContext.Adventures.Select(x => new
        {
            x.Id,
            x.Name,
        }).SingleAsync(x => x.Id == adventureId, cancellationToken: ct);

        var volumeExists = await _volumeManager.ExistsAsync(_settings.GetAdventureVolumeName(adventureId), ct);
        if (!volumeExists)
        {
            throw new Exception($"Could not find volume for adventure {adventureId}");
        }

        return await EnsureContainerAsync(adventureId, _settings.GetContainerName(adventureName.Id.ToString()), _settings.GetAdventureVolumeName(adventureId), ct);
    }

    public async Task<string> EnsureWorldbookContainerRunningAsync(Guid worldbookId, CancellationToken ct)
    {
        var worldbook = await _dbContext.Worldbooks.Select(x => new
        {
            x.Id,
            x.Name
        }).SingleAsync(x => x.Id == worldbookId, cancellationToken: ct);

        var volumeExists = await _volumeManager.ExistsAsync(_settings.GetWorldbookVolumeName(worldbookId), ct);
        if (!volumeExists)
        {
            throw new Exception($"Could not find volume for worldbook {worldbookId}");
        }

        return await EnsureContainerAsync(worldbookId, _settings.GetContainerName(worldbook.Id.ToString()), _settings.GetWorldbookVolumeName(worldbookId), ct);
    }

    private async Task<string> EnsureContainerAsync(Guid identifier, string containerName, string volumeName, CancellationToken ct)
    {
        if (_containers.TryGetValue(identifier, out var existing))
        {
            var status = await _containerManager.GetStatusAsync(existing.Name, ct);
            if (status?.IsRunning == true)
            {
                _containers[identifier] = existing with { LastAccessed = DateTimeOffset.UtcNow };
                return existing.BaseUrl;
            }
        }

        var creationLock = _operationLocks.GetOrAdd(identifier, _ => new SemaphoreSlim(1, 1));
        await creationLock.WaitAsync(ct);
        try
        {
            var hostPort = AllocatePort();
            var containerConfig = BuildContainerConfig(containerName, volumeName, hostPort);
            await _containerManager.StartAsync(containerConfig, ct);
            var baseUrl = _settings.GetContainerBaseUrl(_settings.ContainerPort, containerName);
            _containers.TryAdd(identifier, new ContainerInfo(containerName, baseUrl, hostPort, 0, DateTimeOffset.UtcNow));
            return baseUrl;
        }
        finally
        {
            creationLock.Release();
        }
    }

    public async Task RemoveContainerAsync(Guid identifier, CancellationToken ct)
    {
        var registryLock = _operationLocks.GetOrAdd(identifier, _ => new SemaphoreSlim(1, 1));
        await registryLock.WaitAsync(ct);
        try
        {
            if (!_containers.TryRemove(identifier, out var info))
            {
                return;
            }

            try
            {
                await _containerManager.StopAsync(info.Name, TimeSpan.FromSeconds(10), ct);
                await _containerManager.RemoveAsync(info.Name, force: true, ct);
                _logger.Information("Removed container {ContainerName} for adventure {AdventureId}", info.Name, identifier);
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Failed to remove container {ContainerName}", info.Name);
            }
        }
        finally
        {
            registryLock.Release();
        }
    }

    private int AllocatePort()
    {
        var port = _settings.BasePort;
        var usedPorts = _containers.Values.Select(c => c.Port).ToHashSet();

        while (usedPorts.Contains(port))
        {
            port++;
            if (port > 65535)
            {
                throw new InvalidOperationException("No available ports");
            }
        }

        return port;
    }

    private ContainerConfig BuildContainerConfig(string containerName, string volumeName, int hostPort)
    {
        return new ContainerConfig
        {
            Name = containerName,
            Image = _settings.ImageName,
            Environment = _settings.GetEnvVariable(_config),
            Volumes =
            [
                $"{volumeName}:{_settings.VolumeMountPath}",
                $"{_settings.GetEffectiveVisualizationPath()}:/app/visualization",
                $"{_settings.GetEffectiveDataStorePath()}:/app/data-store"
            ],
            Ports =
            [
                $"{hostPort}:{_settings.ContainerPort}"
            ],
            NetworkName = _settings.NetworkName,
            Labels = new Dictionary<string, string>
            {
                ["fablecraft.managed"] = "true",
                ["fablecraft.service"] = "knowledge-graph"
            },
            // Use ContainerPort for health check (container-to-container communication)
            HealthEndpoint = _settings.BuildHealthCheck(_settings.ContainerPort, containerName)
        };
    }

    private sealed record ContainerInfo(
        string Name,
        string BaseUrl,
        int Port,
        int PendingOperationCount,
        DateTimeOffset LastAccessed);

    public void Decrement(Guid? adventureIdValue)
    {
        if (adventureIdValue == null)
        {
            return;
        }

        if (_containers.TryGetValue(adventureIdValue.Value, out var containerInfo))
        {
            var updated = containerInfo with { PendingOperationCount = Math.Max(containerInfo.PendingOperationCount - 1, 0) };
            _containers.TryUpdate(adventureIdValue.Value, containerInfo with { PendingOperationCount = Math.Max(containerInfo.PendingOperationCount - 1, 0) }, containerInfo);
            _logger.Information("Container {ContainerName} has {request} pending operation", containerInfo.Name, updated.PendingOperationCount);
        }
    }

    public void Increment(Guid? adventureIdValue)
    {
        if (adventureIdValue is null)
        {
            return;
        }

        if (_containers.TryGetValue(adventureIdValue.Value, out var containerInfo))
        {
            var updated = containerInfo with { PendingOperationCount = containerInfo.PendingOperationCount + 1 };
            _containers.TryUpdate(adventureIdValue.Value, updated, containerInfo);
            _logger.Information("Container {ContainerName} has {request} pending operation", containerInfo.Name, updated.PendingOperationCount);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await Task.WhenAll(_containers.Select(x => RemoveContainerAsync(x.Key, CancellationToken.None)));
    }
}