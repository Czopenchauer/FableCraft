using System.Net;
using System.Threading.RateLimiting;

using Docker.DotNet;
using Docker.DotNet.Models;

using Polly;
using Polly.Retry;

using Serilog;

namespace FableCraft.Infrastructure.Docker;

internal sealed class ContainerManager : IContainerManager
{
    private readonly DockerClient _client;
    private readonly ILogger _logger;
    private readonly HttpClient _healthClient;
    private readonly ResiliencePipeline _httpResiliencePipeline = 
        new ResiliencePipelineBuilder()
    .AddRetry(new RetryStrategyOptions
    {
        MaxRetryAttempts = 10,
        Delay = TimeSpan.FromSeconds(5),
        BackoffType = DelayBackoffType.Linear,
        ShouldHandle = new PredicateBuilder().Handle<HttpRequestException>(ex =>
            ex.StatusCode is HttpStatusCode.InternalServerError or HttpStatusCode.TooManyRequests).Handle<TimeoutException>()
    })
    .Build();

    public ContainerManager(
        DockerClient client,
        ILogger logger,
        IHttpClientFactory httpClientFactory)
    {
        _client = client;
        _logger = logger;
        _healthClient = httpClientFactory.CreateClient("DockerHealthCheck");
    }

    public async Task<string> RecreateAsync(ContainerConfig config, CancellationToken ct = default)
    {
        _logger.Information("Recreating container {ContainerName} with image {Image}",
            config.Name, config.Image);

        if (await ExistsAsync(config.Name, ct))
        {
            await StopAsync(config.Name, TimeSpan.FromSeconds(10), ct);
            await RemoveAsync(config.Name, force: true, ct);
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
                portBindings[containerPortKey] = new List<PortBinding>
                {
                    new() { HostPort = hostPort }
                };
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
                RestartPolicy = new RestartPolicy
                {
                    Name = RestartPolicyKind.UnlessStopped
                }
            }
        };

        var createResponse = await _client.Containers.CreateContainerAsync(createParams, ct);
        _logger.Debug("Created container {ContainerId}", createResponse.ID);

        if (!string.IsNullOrEmpty(config.NetworkName))
        {
            try
            {
                await _client.Networks.ConnectNetworkAsync(config.NetworkName,
                    new NetworkConnectParameters
                    {
                        Container = createResponse.ID
                    }, ct);
            }
            catch (DockerApiException ex) when (ex.Message.Contains("already exists"))
            {
                _logger.Debug("Container already connected to network {NetworkName}", config.NetworkName);
            }
        }

        await _client.Containers.StartContainerAsync(createResponse.ID, new ContainerStartParameters(), ct);

        _logger.Information("Started container {ContainerName} ({ContainerId})",
            config.Name, createResponse.ID);

        return createResponse.ID;
    }

    public async Task StopAsync(string containerName, TimeSpan? waitTimeout = null, CancellationToken ct = default)
    {
        _logger.Information("Stopping container {ContainerName}", containerName);

        try
        {
            var timeoutSeconds = (uint)(waitTimeout?.TotalSeconds ?? 10);
            await _client.Containers.StopContainerAsync(containerName,
                new ContainerStopParameters { WaitBeforeKillSeconds = timeoutSeconds }, ct);

            _logger.Information("Stopped container {ContainerName}", containerName);
        }
        catch (DockerApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.Debug("Container {ContainerName} not found, nothing to stop", containerName);
        }
        catch (DockerApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotModified)
        {
            _logger.Debug("Container {ContainerName} already stopped", containerName);
        }
    }

    public async Task RemoveAsync(string containerName, bool force = false, CancellationToken ct = default)
    {
        _logger.Information("Removing container {ContainerName} (force={Force})", containerName, force);

        try
        {
            await _client.Containers.RemoveContainerAsync(containerName,
                new ContainerRemoveParameters { Force = force, RemoveVolumes = false }, ct);

            _logger.Information("Removed container {ContainerName}", containerName);
        }
        catch (DockerApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.Debug("Container {ContainerName} not found, nothing to remove", containerName);
        }
    }

    public async Task WaitForHealthyAsync(string containerName, string healthEndpoint, TimeSpan timeout, CancellationToken ct = default)
    {
        _logger.Information("Waiting for container {ContainerName} to become healthy at {HealthEndpoint}",
            containerName, healthEndpoint);

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(timeout);

        var attempt = 0;
        var delay = TimeSpan.FromMilliseconds(500);
        var maxDelay = TimeSpan.FromSeconds(5);

        while (!timeoutCts.Token.IsCancellationRequested)
        {
            attempt++;

            try
            {
                var response = await _httpResiliencePipeline.ExecuteAsync(async c => await _healthClient.GetAsync(healthEndpoint, c), timeoutCts.Token);
                if (response.IsSuccessStatusCode)
                {
                    _logger.Information("Container {ContainerName} is healthy after {Attempts} attempts",
                        containerName, attempt);
                    return;
                }

                _logger.Debug("Health check attempt {Attempt} returned {StatusCode}",
                    attempt, response.StatusCode);
            }
            catch (HttpRequestException ex)
            {
                _logger.Debug("Health check attempt {Attempt} failed: {Message}", attempt, ex.Message);
            }
            catch (TaskCanceledException) when (timeoutCts.Token.IsCancellationRequested)
            {
                break;
            }

            await Task.Delay(delay, timeoutCts.Token);

            delay = TimeSpan.FromMilliseconds(Math.Min(delay.TotalMilliseconds * 1.5, maxDelay.TotalMilliseconds));
        }

        throw new TimeoutException(
            $"Container {containerName} did not become healthy within {timeout.TotalSeconds} seconds");
    }

    public async Task<ContainerStatus?> GetStatusAsync(string containerName, CancellationToken ct = default)
    {
        try
        {
            var response = await _client.Containers.InspectContainerAsync(containerName, ct);

            return new ContainerStatus(
                response.ID,
                response.Name.TrimStart('/'),
                response.State.Status,
                response.State.Running,
                DateTimeOffset.TryParse(response.State.StartedAt, out var started) ? started : null);
        }
        catch (DockerApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<bool> ExistsAsync(string containerName, CancellationToken ct = default)
    {
        return await GetStatusAsync(containerName, ct) is not null;
    }
}
