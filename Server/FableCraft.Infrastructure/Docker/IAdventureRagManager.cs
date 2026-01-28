using FableCraft.Infrastructure.Docker.Configuration;
using FableCraft.Infrastructure.Docker.Exceptions;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;

using Microsoft.Extensions.Options;

namespace FableCraft.Infrastructure.Docker;

public interface IWorldbookRagManager
{
    Task IndexWorldbook(Guid worldbookId, CancellationToken cancellationToken = default);
}

public interface IAdventureRagManager
{
    Task InitializeFromWorldbook(Adventure adventure, CancellationToken cancellationToken = default);
}

internal sealed class AdventureRagManager : IAdventureRagManager, IWorldbookRagManager
{
    private readonly GraphServiceSettings _settings;
    private readonly IVolumeManager _volumeManager;
    private readonly IGraphContainerRegistry _graphContainerRegistry;

    public AdventureRagManager(IOptions<GraphServiceSettings> settings, IVolumeManager volumeManager, IGraphContainerRegistry graphContainerRegistry)
    {
        _settings = settings.Value;
        _volumeManager = volumeManager;
        _graphContainerRegistry = graphContainerRegistry;
    }

    public async Task InitializeFromWorldbook(Adventure adventure, CancellationToken cancellationToken = default)
    {
        var sourceVolume = _settings.GetWorldbookVolumeName(adventure.WorldbookId);
        var destVolume = _settings.GetAdventureVolumeName(adventure.Id);
        if (!await _volumeManager.ExistsAsync(sourceVolume, cancellationToken))
        {
            throw new WorldbookNotIndexedException(adventure.WorldbookId);
        }

        if (!await _volumeManager.ExistsAsync(destVolume, cancellationToken))
        {
            await _volumeManager.CopyAsync(sourceVolume, destVolume, cancellationToken);
        }

        await _graphContainerRegistry.EnsureAdventureContainerRunningAsync(adventure.Id, cancellationToken);
    }

    public async Task IndexWorldbook(Guid worldbookId, CancellationToken cancellationToken = default)
    {
        var volumeName = _settings.GetWorldbookVolumeName(worldbookId);
        if (!await _volumeManager.ExistsAsync(volumeName, cancellationToken))
        {
            await _volumeManager.CreateAsync(volumeName, cancellationToken);
        }

        await _graphContainerRegistry.EnsureWorldbookContainerRunningAsync(worldbookId, cancellationToken);
    }
}