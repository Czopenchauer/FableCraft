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

internal readonly record struct ContainerKey(Guid Identifier, ContainerType ContainerType)
{
    public override string ToString()
    {
        return $"{Identifier}:{ContainerType}";
    }
}

internal enum ContainerType
{
    Adventure,
    Worldbook
}

internal interface IContainerMonitor
{
    void Increment(ContainerKey key);

    void Decrement(ContainerKey key);
}

internal sealed class GraphContainerRegistry : IContainerMonitor, IAsyncDisposable
{
    private readonly ConcurrentDictionary<ContainerKey, ContainerInfo> _containers = new();
    private readonly ConcurrentDictionary<ContainerKey, (CancellationTokenSource, Task)> _evictionTask = new();
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _operationLocks = new();
    private readonly ContainerManager _containerManager;
    private readonly VolumeManager _volumeManager;
    private readonly IConfiguration _config;
    private readonly IOptionsMonitor<GraphServiceSettings> _settings;
    private readonly ILogger _logger;
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;

    public GraphContainerRegistry(
        ContainerManager containerManager,
        IOptionsMonitor<GraphServiceSettings> settings,
        IConfiguration config,
        ILogger logger,
        IDbContextFactory<ApplicationDbContext> dbContextFactory,
        VolumeManager volumeManager)
    {
        _containerManager = containerManager;
        _config = config;
        _settings = settings;
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

        var volumeExists = await _volumeManager.ExistsAsync(_settings.CurrentValue.GetAdventureVolumeName(adventureId), ct);
        if (!volumeExists)
        {
            throw new Exception($"Could not find volume for adventure {adventureId}");
        }

        return await EnsureContainerAsync(
            new ContainerKey(adventureId, ContainerType.Adventure),
            _settings.CurrentValue.GetContainerName(adventure.Id.ToString()),
            _settings.CurrentValue.GetAdventureVolumeName(adventureId),
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

        var volumeExists = await _volumeManager.ExistsAsync(_settings.CurrentValue.GetWorldbookVolumeName(worldbookId), ct);
        if (!volumeExists)
        {
            throw new Exception($"Could not find volume for worldbook {worldbookId}");
        }

        return await EnsureContainerAsync(
            new ContainerKey(worldbookId, ContainerType.Worldbook),
            _settings.CurrentValue.GetContainerName(worldbook.Id.ToString()),
            _settings.CurrentValue.GetWorldbookVolumeName(worldbookId),
            worldbook.GraphRagSettings,
            ct);
    }

    private async Task<string> EnsureContainerAsync(
        ContainerKey identifier,
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

        var creationLock = _operationLocks.GetOrAdd(identifier.Identifier, _ => new SemaphoreSlim(1, 1));
        try
        {
            await creationLock.WaitAsync(ct);

            var hostPort = AllocatePort();
            var containerConfig = BuildContainerConfig(containerName, volumeName, hostPort, graphRagSettings);
            await _containerManager.StartAsync(containerConfig, ct);
            var baseUrl = _settings.CurrentValue.GetContainerBaseUrl(_settings.CurrentValue.ContainerPort, containerName);
            _containers.TryAdd(identifier, new ContainerInfo(containerName, baseUrl, hostPort, 0, DateTimeOffset.UtcNow));

            if (_evictionTask.TryRemove(identifier, out var value))
            {
                await value.Item1.CancelAsync();
                value.Item1.Dispose();
            }
            var newToken = new CancellationTokenSource();
            _evictionTask.TryAdd(identifier, (newToken, Task.Run(() => Eviction(identifier, _settings.CurrentValue.EvictionTime, newToken.Token), newToken.Token)));
            return baseUrl;
        }
        finally
        {
            creationLock.Release();
        }
    }

    public async Task RemoveContainerAsync(ContainerKey identifier, CancellationToken ct)
    {
        var registryLock = _operationLocks.GetOrAdd(identifier.Identifier, _ => new SemaphoreSlim(1, 1));
        try
        {
            await registryLock.WaitAsync(ct);

            if (!_containers.TryRemove(identifier, out var info))
            {
                return;
            }

            try
            {
                await DumpContainerLogsAsync(info.Name, ct);
                await _containerManager.StopAsync(info.Name, TimeSpan.FromSeconds(10), ct);
                await _containerManager.RemoveAsync(info.Name, force: true, ct);
                if (_evictionTask.TryRemove(identifier, out var value))
                {
                    await value.Item1.CancelAsync();
                    value.Item1.Dispose();
                }

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
        var port = _settings.CurrentValue.BasePort;
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
            Image = _settings.CurrentValue.ImageName,
            Environment = _settings.CurrentValue.GetEnvVariable(_config, graphRagSettings),
            Volumes =
            [
                $"{volumeName}:{_settings.CurrentValue.VolumeMountPath}",
                $"{_settings.CurrentValue.GetEffectiveVisualizationPath()}/{containerName}:/app/visualization",
                $"{_settings.CurrentValue.GetEffectiveDataStorePath()}:/app/data-store"
            ],
            Ports =
            [
                $"{hostPort}:{_settings.CurrentValue.ContainerPort}"
            ],
            NetworkName = _settings.CurrentValue.NetworkName,
            Labels = new Dictionary<string, string>
            {
                ["fablecraft.managed"] = "true",
                ["fablecraft.service"] = "knowledge-graph"
            },
            HealthEndpoint = _settings.CurrentValue.BuildHealthCheck(_settings.CurrentValue.ContainerPort, containerName)
        };
    }

    private sealed record ContainerInfo(
        string Name,
        string BaseUrl,
        int Port,
        int PendingOperationCount,
        DateTimeOffset LastAccessed);

    private async Task Eviction(ContainerKey key, TimeSpan delay, CancellationToken ct)
    {
        try
        {
            _logger.Information("Waiting {delay} for container {ContainerName} to evict", delay, key.ToString());
            await Task.Delay(delay, ct);
            _logger.Information("Start evicting container {ContainerName}", key.ToString());
            if (_containers.TryGetValue(key, out var containerInfo) && containerInfo.PendingOperationCount == 0 && DateTimeOffset.UtcNow - containerInfo.LastAccessed - TimeSpan.FromSeconds(1) >= delay)
            {
                var removalLock = _operationLocks.GetOrAdd(key.Identifier, _ => new SemaphoreSlim(1, 1));
                try
                {
                    await removalLock.WaitAsync(ct);
                    if (_containers.TryGetValue(key, out containerInfo)
                        && containerInfo.PendingOperationCount == 0
                        && DateTimeOffset.UtcNow - containerInfo.LastAccessed > delay)
                    {
                        await DumpContainerLogsAsync(containerInfo.Name, ct);
                        await _containerManager.StopAsync(containerInfo.Name, TimeSpan.FromSeconds(10), ct);
                        await _containerManager.RemoveAsync(containerInfo.Name, force: true, ct);
                        _containers.TryRemove(key, out _);
                    }
                }
                finally
                {
                    removalLock.Release();
                }
            }
            else
            {
                var elapsed = containerInfo != null ? DateTimeOffset.UtcNow - containerInfo.LastAccessed : TimeSpan.Zero;
                _logger.Information(
                    "Evicting container {ContainerName} skipped - LastAccessed: {lastAccessed}, Elapsed: {elapsed}, Required: {delay}, Operations: {operations}",
                    key.ToString(),
                    containerInfo?.LastAccessed,
                    elapsed,
                    delay,
                    containerInfo?.PendingOperationCount);

                if (containerInfo != null && elapsed < delay)
                {
                    var remainingTime = delay - elapsed + TimeSpan.FromSeconds(5);
                    _logger.Information("Rescheduling eviction for {ContainerName} in {remainingTime}", key.ToString(), remainingTime);

                    if (_evictionTask.TryGetValue(key, out (CancellationTokenSource token, Task task) value))
                    {
                        await value.token.CancelAsync();
                        value.token.Dispose();

                        var newToken = new CancellationTokenSource();
                        _evictionTask.TryRemove(key, out _);
                        _evictionTask.TryAdd(key, (newToken, Task.Run(() => Eviction(key, _settings.CurrentValue.EvictionTime, newToken.Token), newToken.Token)));
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.Information("Eviction for {container} canceled", key.ToString());
        }
        catch (ObjectDisposedException)
        {
            _logger.Information("Eviction for {container} canceled", key.ToString());
        }
    }

    public void Decrement(ContainerKey key)
    {
        if (!_containers.TryGetValue(key, out var containerInfo))
        {
            _logger.Information("Container {ContainerName} not found", key.ToString());
            return;
        }

        lock (containerInfo)
        {
            var updated = containerInfo with
            {
                PendingOperationCount = Math.Max(containerInfo.PendingOperationCount - 1, 0),
                LastAccessed = DateTimeOffset.UtcNow
            };
            _containers.TryUpdate(key, updated, containerInfo);
            _logger.Information("Container {ContainerName} has {request} pending operation", containerInfo.Name, updated.PendingOperationCount);
        }
    }

    public void Increment(ContainerKey key)
    {
        if (!_containers.TryGetValue(key, out var containerInfo))
        {
            _logger.Information("Container {ContainerName} not found", key.ToString());
            return;
        }

        lock (containerInfo)
        {
            var updated = containerInfo with
            {
                PendingOperationCount = containerInfo.PendingOperationCount + 1,
                LastAccessed = DateTimeOffset.UtcNow
            };
            _containers.TryUpdate(key, updated, containerInfo);
            _logger.Information("Container {ContainerName} has {request} pending operation", containerInfo.Name, updated.PendingOperationCount);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await Task.WhenAll(_containers.Select(x => RemoveContainerAsync(x.Key, CancellationToken.None)));
    }

    private async Task DumpContainerLogsAsync(string containerName, CancellationToken ct)
    {
        try
        {
            var logsPath = _settings.CurrentValue.GetEffectiveLogsPath();
            await _containerManager.DumpLogsAsync(containerName, logsPath, ct);
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to dump logs for container {ContainerName}", containerName);
        }
    }
}