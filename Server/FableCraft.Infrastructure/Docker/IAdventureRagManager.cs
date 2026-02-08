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

    /// <summary>
    /// Deletes the existing adventure volume and reinitializes it from the worldbook template.
    /// Used to recover corrupted GraphRAG containers.
    /// </summary>
    Task RecreateFromWorldbook(Adventure adventure, CancellationToken cancellationToken = default);
}

internal sealed class AdventureRagManager(IOptions<GraphServiceSettings> settings, VolumeManager volumeManager, GraphContainerRegistry graphContainerRegistry)
    : IAdventureRagManager, IWorldbookRagManager
{
    private readonly GraphServiceSettings _settings = settings.Value;

    public async Task InitializeFromWorldbook(Adventure adventure, CancellationToken cancellationToken = default)
    {
        var sourceVolume = _settings.GetWorldbookVolumeName(adventure.WorldbookId);
        var destVolume = _settings.GetAdventureVolumeName(adventure.Id);
        if (!await volumeManager.ExistsAsync(sourceVolume, cancellationToken))
        {
            throw new WorldbookNotIndexedException(adventure.WorldbookId);
        }

        if (!await volumeManager.ExistsAsync(destVolume, cancellationToken))
        {
            await volumeManager.CopyAsync(sourceVolume, destVolume, cancellationToken);
        }

        await graphContainerRegistry.EnsureAdventureContainerRunningAsync(adventure.Id, cancellationToken);
    }

    public async Task RecreateFromWorldbook(Adventure adventure, CancellationToken cancellationToken = default)
    {
        var sourceVolume = _settings.GetWorldbookVolumeName(adventure.WorldbookId);
        var destVolume = _settings.GetAdventureVolumeName(adventure.Id);

        if (!await volumeManager.ExistsAsync(sourceVolume, cancellationToken))
        {
            throw new WorldbookNotIndexedException(adventure.WorldbookId);
        }

        await graphContainerRegistry.RemoveContainerAsync(new ContainerKey(adventure.Id, ContainerType.Adventure), cancellationToken);

        if (await volumeManager.ExistsAsync(destVolume, cancellationToken))
        {
            await volumeManager.DeleteAsync(destVolume, force: true, cancellationToken);
        }

        await volumeManager.CopyAsync(sourceVolume, destVolume, cancellationToken);

        await graphContainerRegistry.EnsureAdventureContainerRunningAsync(adventure.Id, cancellationToken);
    }

    public async Task IndexWorldbook(Guid worldbookId, CancellationToken cancellationToken = default)
    {
        var volumeName = _settings.GetWorldbookVolumeName(worldbookId);
        if (!await volumeManager.ExistsAsync(volumeName, cancellationToken))
        {
            await volumeManager.CreateAsync(volumeName, cancellationToken);
        }

        await graphContainerRegistry.EnsureWorldbookContainerRunningAsync(worldbookId, cancellationToken);
    }
}