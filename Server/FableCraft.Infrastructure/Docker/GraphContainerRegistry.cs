using System.Collections.Concurrent;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

using FableCraft.Infrastructure.Docker.Configuration;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities;

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

    /// <summary>
    /// Stops and removes the container for the given identifier (adventure or worldbook).
    /// </summary>
    Task RemoveContainerAsync(Guid identifier, CancellationToken ct);
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
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;

    public GraphContainerRegistry(
        ContainerManager containerManager,
        IOptions<GraphServiceSettings> settings,
        IConfiguration config,
        ILogger logger, IDbContextFactory<ApplicationDbContext> dbContextFactory, IVolumeManager volumeManager)
    {
        _containerManager = containerManager;
        _config = config;
        _settings = settings.Value;
        _logger = logger;
        _dbContextFactory = dbContextFactory;
        _volumeManager = volumeManager;
    }

    public async Task<string> EnsureAdventureContainerRunningAsync(Guid adventureId, CancellationToken ct)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(ct);
        var adventure = await dbContext.Adventures
            .Include(a => a.GraphRagSettings)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.GraphRagSettings
            })
            .SingleAsync(x => x.Id == adventureId, cancellationToken: ct);

        var volumeExists = await _volumeManager.ExistsAsync(_settings.GetAdventureVolumeName(adventureId), ct);
        if (!volumeExists)
        {
            throw new Exception($"Could not find volume for adventure {adventureId}");
        }

        return await EnsureContainerAsync(
            adventureId,
            _settings.GetContainerName(adventure.Id.ToString()),
            _settings.GetAdventureVolumeName(adventureId),
            adventure.GraphRagSettings,
            ct);
    }

    public async Task<string> EnsureWorldbookContainerRunningAsync(Guid worldbookId, CancellationToken ct)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(ct);
        var worldbook = await dbContext.Worldbooks
            .Include(w => w.GraphRagSettings)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.GraphRagSettings
            })
            .SingleAsync(x => x.Id == worldbookId, cancellationToken: ct);

        var volumeExists = await _volumeManager.ExistsAsync(_settings.GetWorldbookVolumeName(worldbookId), ct);
        if (!volumeExists)
        {
            throw new Exception($"Could not find volume for worldbook {worldbookId}");
        }

        return await EnsureContainerAsync(
            worldbookId,
            _settings.GetContainerName(worldbook.Id.ToString()),
            _settings.GetWorldbookVolumeName(worldbookId),
            worldbook.GraphRagSettings,
            ct);
    }

    private async Task<string> EnsureContainerAsync(
        Guid identifier,
        string containerName,
        string volumeName,
        GraphRagSettings? graphRagSettings,
        CancellationToken ct)
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
            var containerConfig = BuildContainerConfig(containerName, volumeName, hostPort, graphRagSettings);
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

        while (usedPorts.Contains(port) || !IsPortAvailable(port))
        {
            port++;
            if (port > 65535)
            {
                throw new InvalidOperationException("No available ports");
            }
        }

        return port;
    }

    private static bool IsPortAvailable(int port)
    {
        var activeTcpListeners = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners();
        if (activeTcpListeners.Any(endpoint => endpoint.Port == port))
        {
            return false;
        }

        try
        {
            using var listener = new TcpListener(IPAddress.Loopback, port);
            listener.Start();
            listener.Stop();
            return true;
        }
        catch (SocketException)
        {
            return false;
        }
    }

    private ContainerConfig BuildContainerConfig(
        string containerName,
        string volumeName,
        int hostPort,
        GraphRagSettings? graphRagSettings)
    {
        return new ContainerConfig
        {
            Name = containerName,
            Image = _settings.ImageName,
            Environment = _settings.GetEnvVariable(_config, graphRagSettings),
            Volumes =
            [
                $"{volumeName}:{_settings.VolumeMountPath}",
                $"{_settings.GetEffectiveVisualizationPath()}/{containerName}:/app/visualization",
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
            _containers.TryUpdate(adventureIdValue.Value, updated, containerInfo);
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