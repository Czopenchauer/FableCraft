using Docker.DotNet;
using Docker.DotNet.Models;

using FableCraft.Infrastructure.Docker.Configuration;

using Microsoft.Extensions.Options;

using Serilog;

namespace FableCraft.Infrastructure.Docker;

internal sealed class VolumeManager : IVolumeManager
{
    private readonly DockerClient _client;
    private readonly DockerSettings _settings;
    private readonly ILogger _logger;

    public VolumeManager(
        DockerClient client,
        IOptions<DockerSettings> settings,
        ILogger logger)
    {
        _client = client;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task CreateAsync(string volumeName, CancellationToken ct = default)
    {
        _logger.Information("Creating Docker volume {VolumeName}", volumeName);

        await _client.Volumes.CreateAsync(new VolumesCreateParameters
            {
                Name = volumeName,
                Labels = new Dictionary<string, string>
                {
                    ["fablecraft.managed"] = "true",
                    ["fablecraft.created"] = DateTimeOffset.UtcNow.ToString("O")
                }
            },
            ct);

        _logger.Information("Created Docker volume {VolumeName}", volumeName);
    }

    public async Task CopyAsync(string source, string destination, CancellationToken ct = default)
    {
        _logger.Information("Copying volume {Source} to {Destination}", source, destination);

        if (!await ExistsAsync(destination, ct))
        {
            await CreateAsync(destination, ct);
        }

        var containerName = $"fablecraft-volume-copy-{Guid.NewGuid():N}";

        try
        {
            var createResponse = await _client.Containers.CreateContainerAsync(new CreateContainerParameters
                {
                    Image = _settings.UtilityImage,
                    Name = containerName[..63],
                    Cmd = ["sh", "-c", "cp -a /source/. /destination/"],
                    HostConfig = new HostConfig
                    {
                        Binds =
                        [
                            $"{source}:/source:ro",
                            $"{destination}:/destination"
                        ],
                        AutoRemove = false
                    }
                },
                ct);

            _logger.Debug("Created copy container {ContainerId}", createResponse.ID);

            await _client.Containers.StartContainerAsync(createResponse.ID, new ContainerStartParameters(), ct);

            var waitResponse = await _client.Containers.WaitContainerAsync(createResponse.ID, ct);

            if (waitResponse.StatusCode != 0)
            {
                throw new InvalidOperationException(
                    $"Volume copy failed with exit code {waitResponse.StatusCode}");
            }

            _logger.Information("Copied volume {Source} to {Destination}", source, destination);
        }
        finally
        {
            try
            {
                await _client.Containers.RemoveContainerAsync(containerName,
                    new ContainerRemoveParameters { Force = true },
                    ct);
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Failed to remove copy container {ContainerName}", containerName);
            }
        }
    }

    public async Task DeleteAsync(string volumeName, bool force = false, CancellationToken ct = default)
    {
        _logger.Information("Deleting Docker volume {VolumeName} (force={Force})", volumeName, force);

        try
        {
            await _client.Volumes.RemoveAsync(volumeName, force, ct);
            _logger.Information("Deleted Docker volume {VolumeName}", volumeName);
        }
        catch (DockerApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.Debug("Volume {VolumeName} not found, nothing to delete", volumeName);
        }
    }

    public async Task<bool> ExistsAsync(string volumeName, CancellationToken ct = default)
    {
        try
        {
            await _client.Volumes.InspectAsync(volumeName, ct);
            return true;
        }
        catch (DockerApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public async Task<string> ExportAsync(string volumeName, string outputDirectory, CancellationToken ct = default)
    {
        _logger.Information("Exporting volume {VolumeName} to {OutputDirectory}", volumeName, outputDirectory);

        Directory.CreateDirectory(outputDirectory);

        var tarFileName = $"{volumeName}-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.tar";
        var tarPath = Path.Combine(outputDirectory, tarFileName);
        var containerName = $"fablecraft-volume-export-{Guid.NewGuid():N}";

        try
        {
            var createResponse = await _client.Containers.CreateContainerAsync(new CreateContainerParameters
                {
                    Image = _settings.UtilityImage,
                    Name = containerName,
                    Cmd = ["tar", "-cvf", "/export/volume.tar", "-C", "/data", "."],
                    HostConfig = new HostConfig
                    {
                        Binds =
                        [
                            $"{volumeName}:/data:ro",
                            $"{Path.GetDirectoryName(tarPath)}:/export"
                        ],
                        AutoRemove = false
                    }
                },
                ct);

            await _client.Containers.StartContainerAsync(createResponse.ID, new ContainerStartParameters(), ct);

            var waitResponse = await _client.Containers.WaitContainerAsync(createResponse.ID, ct);

            if (waitResponse.StatusCode != 0)
            {
                throw new InvalidOperationException(
                    $"Volume export failed with exit code {waitResponse.StatusCode}");
            }

            var tempTarPath = Path.Combine(Path.GetDirectoryName(tarPath)!, "volume.tar");
            if (File.Exists(tempTarPath))
            {
                File.Move(tempTarPath, tarPath, overwrite: true);
            }

            _logger.Information("Exported volume {VolumeName} to {TarPath}", volumeName, tarPath);
            return tarPath;
        }
        finally
        {
            try
            {
                await _client.Containers.RemoveContainerAsync(containerName,
                    new ContainerRemoveParameters { Force = true },
                    ct);
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Failed to remove export container {ContainerName}", containerName);
            }
        }
    }

    public async Task ImportAsync(string tarPath, string volumeName, bool overwrite = false, CancellationToken ct = default)
    {
        _logger.Information("Importing {TarPath} to volume {VolumeName} (overwrite={Overwrite})",
            tarPath,
            volumeName,
            overwrite);

        if (!File.Exists(tarPath))
        {
            throw new FileNotFoundException($"Tar file not found: {tarPath}");
        }

        if (!await ExistsAsync(volumeName, ct))
        {
            await CreateAsync(volumeName, ct);
        }

        var containerName = $"fablecraft-volume-import-{Guid.NewGuid():N}";
        var tarDirectory = Path.GetDirectoryName(Path.GetFullPath(tarPath))!;
        var tarFileName = Path.GetFileName(tarPath);

        try
        {
            var cmd = overwrite
                ? $"rm -rf /data/* && tar -xvf /import/{tarFileName} -C /data"
                : $"tar -xvf /import/{tarFileName} -C /data";

            var createResponse = await _client.Containers.CreateContainerAsync(new CreateContainerParameters
                {
                    Image = _settings.UtilityImage,
                    Name = containerName,
                    Cmd = ["sh", "-c", cmd],
                    HostConfig = new HostConfig
                    {
                        Binds =
                        [
                            $"{volumeName}:/data",
                            $"{tarDirectory}:/import:ro"
                        ],
                        AutoRemove = false
                    }
                },
                ct);

            await _client.Containers.StartContainerAsync(createResponse.ID, new ContainerStartParameters(), ct);

            var waitResponse = await _client.Containers.WaitContainerAsync(createResponse.ID, ct);

            if (waitResponse.StatusCode != 0)
            {
                throw new InvalidOperationException(
                    $"Volume import failed with exit code {waitResponse.StatusCode}");
            }

            _logger.Information("Imported {TarPath} to volume {VolumeName}", tarPath, volumeName);
        }
        finally
        {
            try
            {
                await _client.Containers.RemoveContainerAsync(containerName,
                    new ContainerRemoveParameters { Force = true },
                    ct);
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Failed to remove import container {ContainerName}", containerName);
            }
        }
    }
}