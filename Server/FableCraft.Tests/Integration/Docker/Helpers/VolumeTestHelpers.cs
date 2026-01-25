using Docker.DotNet;
using Docker.DotNet.Models;

namespace FableCraft.Tests.Integration.Docker.Helpers;

public static class VolumeTestHelpers
{
    public static async Task WriteFileToVolume(
        DockerClient client,
        string utilityImage,
        string volumeName,
        string filePath,
        string content)
    {
        var containerName = $"fablecraft-test-write-{Guid.NewGuid():N}";
        var escapedContent = content.Replace("'", "'\\''");

        var response = await client.Containers.CreateContainerAsync(new CreateContainerParameters
        {
            Image = utilityImage,
            Name = containerName,
            Cmd = ["sh", "-c", $"mkdir -p $(dirname /data/{filePath}) && echo '{escapedContent}' > /data/{filePath}"],
            HostConfig = new HostConfig
            {
                Binds = [$"{volumeName}:/data"],
                AutoRemove = false
            }
        });

        await client.Containers.StartContainerAsync(response.ID, new ContainerStartParameters());
        await client.Containers.WaitContainerAsync(response.ID);
        await client.Containers.RemoveContainerAsync(containerName, new ContainerRemoveParameters { Force = true });
    }

    public static async Task<string> ReadFileFromVolume(
        DockerClient client,
        string utilityImage,
        string volumeName,
        string filePath)
    {
        var containerName = $"fablecraft-test-read-{Guid.NewGuid():N}";

        var response = await client.Containers.CreateContainerAsync(new CreateContainerParameters
        {
            Image = utilityImage,
            Name = containerName,
            Cmd = ["cat", $"/data/{filePath}"],
            HostConfig = new HostConfig
            {
                Binds = [$"{volumeName}:/data:ro"],
                AutoRemove = false
            }
        });

        await client.Containers.StartContainerAsync(response.ID, new ContainerStartParameters());
        await client.Containers.WaitContainerAsync(response.ID);

        var logs = await client.Containers.GetContainerLogsAsync(response.ID,
            new ContainerLogsParameters { ShowStdout = true });

        // MultiplexedStream needs special handling
        var memoryStream = new MemoryStream();
        await logs.CopyOutputToAsync(null, memoryStream, memoryStream, CancellationToken.None);
        memoryStream.Position = 0;
        using var reader = new StreamReader(memoryStream);
        var content = await reader.ReadToEndAsync();

        await client.Containers.RemoveContainerAsync(containerName, new ContainerRemoveParameters { Force = true });

        // Strip Docker log header bytes if present (8 bytes for each log line)
        return StripDockerLogHeaders(content).Trim();
    }

    /// <summary>
    /// Docker multiplexed stream format has 8-byte headers before each frame.
    /// Format: [stream_type(1), 0, 0, 0, size(4)] followed by payload.
    /// </summary>
    private static string StripDockerLogHeaders(string content)
    {
        if (string.IsNullOrEmpty(content))
            return content;

        // If content starts with stream type byte (0x01 for stdout, 0x02 for stderr)
        // followed by 3 zero bytes, it's a multiplexed stream
        var bytes = System.Text.Encoding.UTF8.GetBytes(content);
        if (bytes.Length >= 8 && bytes[1] == 0 && bytes[2] == 0 && bytes[3] == 0)
        {
            // Skip the 8-byte header
            return System.Text.Encoding.UTF8.GetString(bytes, 8, bytes.Length - 8);
        }

        return content;
    }
}
