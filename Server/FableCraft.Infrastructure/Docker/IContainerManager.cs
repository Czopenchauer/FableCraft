namespace FableCraft.Infrastructure.Docker;

/// <summary>
/// Container status information.
/// </summary>
public sealed record ContainerStatus(
    string Id,
    string Name,
    string State,
    bool IsRunning,
    DateTimeOffset? StartedAt);

/// <summary>
/// Configuration for creating a container.
/// </summary>
public sealed class ContainerConfig
{
    /// <summary>
    /// Container name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Docker image to use.
    /// </summary>
    public required string Image { get; init; }

    /// <summary>
    /// Environment variables.
    /// </summary>
    public IDictionary<string, string> Environment { get; init; } = new Dictionary<string, string>();

    /// <summary>
    /// Volume bindings in format "volume_name:/container/path".
    /// </summary>
    public IList<string> Volumes { get; init; } = [];

    /// <summary>
    /// Port mappings in format "host:container".
    /// </summary>
    public IList<string> Ports { get; init; } = [];

    /// <summary>
    /// Docker network to attach to.
    /// </summary>
    public string? NetworkName { get; init; }

    /// <summary>
    /// Labels to apply to the container.
    /// </summary>
    public IDictionary<string, string> Labels { get; init; } = new Dictionary<string, string>();
}

/// <summary>
/// Manages Docker containers for knowledge graph service.
/// </summary>
public interface IContainerManager
{
    /// <summary>
    /// Recreates a container with new configuration, stopping and removing existing if present.
    /// </summary>
    /// <param name="config">Container configuration.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Container ID.</returns>
    Task<string> RecreateAsync(ContainerConfig config, CancellationToken ct = default);

    /// <summary>
    /// Stops a running container.
    /// </summary>
    /// <param name="containerName">Container name or ID.</param>
    /// <param name="waitTimeout">Time to wait for graceful stop.</param>
    /// <param name="ct">Cancellation token.</param>
    Task StopAsync(string containerName, TimeSpan? waitTimeout = null, CancellationToken ct = default);

    /// <summary>
    /// Removes a container.
    /// </summary>
    /// <param name="containerName">Container name or ID.</param>
    /// <param name="force">Force removal even if running.</param>
    /// <param name="ct">Cancellation token.</param>
    Task RemoveAsync(string containerName, bool force = false, CancellationToken ct = default);

    /// <summary>
    /// Waits for a container to become healthy by polling an HTTP endpoint.
    /// </summary>
    /// <param name="containerName">Container name for logging.</param>
    /// <param name="healthEndpoint">Full URL to health endpoint.</param>
    /// <param name="timeout">Maximum time to wait.</param>
    /// <param name="ct">Cancellation token.</param>
    Task WaitForHealthyAsync(string containerName, string healthEndpoint, TimeSpan timeout, CancellationToken ct = default);

    /// <summary>
    /// Gets the status of a container.
    /// </summary>
    /// <param name="containerName">Container name or ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Container status, or null if not found.</returns>
    Task<ContainerStatus?> GetStatusAsync(string containerName, CancellationToken ct = default);

    /// <summary>
    /// Checks if a container exists.
    /// </summary>
    /// <param name="containerName">Container name or ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the container exists.</returns>
    Task<bool> ExistsAsync(string containerName, CancellationToken ct = default);
}
