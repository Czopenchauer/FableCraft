using System.Diagnostics;
using System.Net;

using Docker.DotNet;
using Docker.DotNet.Models;

using Serilog;

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
    /// Health endpoint.
    /// </summary>
    public required string HealthEndpoint { get; init; }

    /// <summary>
    /// Docker network to attach to.
    /// </summary>
    public string? NetworkName { get; init; }

    /// <summary>
    /// Labels to apply to the container.
    /// </summary>
    public IDictionary<string, string> Labels { get; init; } = new Dictionary<string, string>();
}

internal sealed class ContainerManager
{
    private readonly DockerClient _client;
    private readonly ILogger _logger;
    private readonly HttpClient _healthClient;

    public ContainerManager(
        DockerClient client,
        ILogger logger,
        HttpClient httpClient)
    {
        _client = client;
        _logger = logger;
        _healthClient = httpClient;
    }

    public async Task StartAsync(ContainerConfig config, CancellationToken cancellationToken = default)
    {
        _logger.Information("Starting container {ContainerName} with image {Image}",
            config.Name,
            config.Image);

        var status = await GetStatusAsync(config.Name, cancellationToken);
        if (status is not null)
        {
            if (status.IsRunning)
            {
                return;
            }

            await _client.Containers.StartContainerAsync(status.Id, new ContainerStartParameters(), cancellationToken);
            await WaitForHealthyAsync(config.Name, config.HealthEndpoint, cancellationToken: cancellationToken);
            return;
        }

        var envList = config.Environment
            .Select(kvp => $"{kvp.Key}={kvp.Value}")
            .ToList();

        var exposedPorts = new Dictionary<string, EmptyStruct>();
        var portBindings = new Dictionary<string, IList<PortBinding>>();

        foreach (var portMapping in config.Ports)
        {
            var parts = portMapping.Split(':');
            if (parts.Length == 2)
            {
                var hostPort = parts[0];
                var containerPort = parts[1];
                var containerPortKey = $"{containerPort}/tcp";

                exposedPorts[containerPortKey] = default;
                portBindings[containerPortKey] = new List<PortBinding> { new() { HostPort = hostPort } };
            }
        }

        var createParams = new CreateContainerParameters
        {
            Image = config.Image,
            Name = config.Name,
            Env = envList,
            ExposedPorts = exposedPorts,
            Labels = config.Labels.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
            HostConfig = new HostConfig
            {
                Binds = config.Volumes.ToList(),
                PortBindings = portBindings,
                NetworkMode = config.NetworkName,
                RestartPolicy = new RestartPolicy { Name = RestartPolicyKind.UnlessStopped }
            }
        };

        var createResponse = await _client.Containers.CreateContainerAsync(createParams, cancellationToken);
        _logger.Information("Created container {ContainerId}", createResponse.ID);

        try
        {
            await _client.Networks.ConnectNetworkAsync(config.NetworkName,
                new NetworkConnectParameters { Container = createResponse.ID },
                cancellationToken);
        }
        catch (DockerApiException ex) when (ex.Message.Contains("already exists"))
        {
            _logger.Information("Container already connected to network {NetworkName}", config.NetworkName);
        }

        await _client.Containers.StartContainerAsync(createResponse.ID, new ContainerStartParameters(), cancellationToken);
        await WaitForHealthyAsync(config.Name, config.HealthEndpoint, cancellationToken: cancellationToken);
        _logger.Information("Started container {ContainerName} ({ContainerId})",
            config.Name,
            createResponse.ID);
    }

    public async Task StopAsync(string containerName, TimeSpan? waitTimeout = null, CancellationToken cancellationToken = default)
    {
        _logger.Information("Stopping container {ContainerName}", containerName);

        try
        {
            var timeoutSeconds = (uint)(waitTimeout?.TotalSeconds ?? 10);
            await _client.Containers.StopContainerAsync(containerName,
                new ContainerStopParameters { WaitBeforeKillSeconds = timeoutSeconds },
                cancellationToken);

            _logger.Information("Stopped container {ContainerName}", containerName);
        }
        catch (DockerApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.Information("Container {ContainerName} not found, nothing to stop", containerName);
        }
        catch (DockerApiException ex) when (ex.StatusCode == HttpStatusCode.NotModified)
        {
            _logger.Information("Container {ContainerName} already stopped", containerName);
        }
    }

    public async Task RemoveAsync(string containerName, bool force = false, CancellationToken cancellationToken = default)
    {
        _logger.Information("Removing container {ContainerName} (force={Force})", containerName, force);

        try
        {
            await _client.Containers.RemoveContainerAsync(containerName,
                new ContainerRemoveParameters
                {
                    Force = force,
                    RemoveVolumes = false
                },
                cancellationToken);

            _logger.Information("Removed container {ContainerName}", containerName);
        }
        catch (DockerApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.Debug("Container {ContainerName} not found, nothing to remove", containerName);
        }
    }

    private async Task WaitForHealthyAsync(string containerName, string healthEndpoint, CancellationToken cancellationToken = default)
    {
        _logger.Information("Waiting for container {ContainerName} to become healthy at {HealthEndpoint}",
            containerName,
            healthEndpoint);

        var stopwatch = Stopwatch.StartNew();
        var response = await _healthClient.GetAsync(healthEndpoint, cancellationToken);
        response.EnsureSuccessStatusCode();
        _logger.Information("Container {ContainerName} is healthy after {ElapsedMs} ms",
            containerName,
            stopwatch.ElapsedMilliseconds);
    }

    public async Task<ContainerStatus?> GetStatusAsync(string containerName, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _client.Containers.InspectContainerAsync(containerName, cancellationToken);

            return new ContainerStatus(
                response.ID,
                response.Name.TrimStart('/'),
                response.State.Status,
                response.State.Running,
                DateTimeOffset.TryParse(response.State.StartedAt, out var started) ? started : null);
        }
        catch (DockerApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }
}