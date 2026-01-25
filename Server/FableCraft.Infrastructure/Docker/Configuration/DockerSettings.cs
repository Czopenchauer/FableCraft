namespace FableCraft.Infrastructure.Docker.Configuration;

/// <summary>
/// Configuration settings for Docker client connectivity.
/// </summary>
internal sealed class DockerSettings
{
    public const string SectionName = "Docker";

    /// <summary>
    /// Docker socket path. Defaults to platform-appropriate value.
    /// Unix: unix:///var/run/docker.sock
    /// Windows: npipe://./pipe/docker_engine
    /// </summary>
    public string SocketPath { get; set; } = OperatingSystem.IsWindows()
        ? "npipe://./pipe/docker_engine"
        : "unix:///var/run/docker.sock";

    /// <summary>
    /// Utility image used for volume copy operations.
    /// </summary>
    public string UtilityImage { get; set; } = "alpine:latest";
}