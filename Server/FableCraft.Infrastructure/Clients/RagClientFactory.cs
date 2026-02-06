using FableCraft.Infrastructure.Docker;
using FableCraft.Infrastructure.Queue;

using Serilog;

namespace FableCraft.Infrastructure.Clients;

public interface IRagClientFactory
{
    Task<IRagSearch> CreateSearchClientForAdventure(Guid adventureId, CancellationToken cancellationToken);

    Task<IRagBuilder> CreateBuildClientForAdventure(Guid adventureId, CancellationToken cancellationToken);
    
    Task<IRagBuilder> CreateBuildClientForWorldbook(Guid worldbookId, CancellationToken cancellationToken);
}

internal sealed class RagClientFactory : IRagClientFactory
{
    public const string HttpClientName = "GraphRagClient";
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly GraphContainerRegistry _graphContainerRegistry;
    private readonly IContainerMonitor _containerMonitor;
    private readonly ILogger _logger;
    private readonly IMessageDispatcher _messageDispatcher;

    public RagClientFactory(
        IHttpClientFactory httpClientFactory,
        GraphContainerRegistry graphContainerRegistry,
        IContainerMonitor containerMonitor,
        IMessageDispatcher messageDispatcher,
        ILogger logger)
    {
        _httpClientFactory = httpClientFactory;
        _graphContainerRegistry = graphContainerRegistry;
        _containerMonitor = containerMonitor;
        _messageDispatcher = messageDispatcher;
        _logger = logger;
    }

    public async Task<IRagSearch> CreateSearchClientForAdventure(Guid adventureId, CancellationToken cancellationToken)
    {
        var baseUrl = await _graphContainerRegistry.EnsureAdventureContainerRunningAsync(adventureId, cancellationToken);
        var httpClient = _httpClientFactory.CreateClient(HttpClientName);
        var inner = new RagClient(httpClient, baseUrl, _messageDispatcher, _logger);
        return new MonitoredRagClient(inner, _containerMonitor, adventureId);
    }

    public async Task<IRagBuilder> CreateBuildClientForAdventure(Guid adventureId, CancellationToken cancellationToken)
    {
        var baseUrl = await _graphContainerRegistry.EnsureAdventureContainerRunningAsync(adventureId, cancellationToken);
        var httpClient = _httpClientFactory.CreateClient(HttpClientName);
        var inner = new RagClient(httpClient, baseUrl, _messageDispatcher, _logger);
        return new MonitoredRagClient(inner, _containerMonitor, adventureId);
    }

    public async Task<IRagBuilder> CreateBuildClientForWorldbook(Guid worldbookId, CancellationToken cancellationToken)
    {
        var baseUrl = await _graphContainerRegistry.EnsureWorldbookContainerRunningAsync(worldbookId, cancellationToken);
        var httpClient = _httpClientFactory.CreateClient(HttpClientName);
        var inner = new RagClient(httpClient, baseUrl, _messageDispatcher, _logger);
        return new MonitoredRagClient(inner, _containerMonitor, worldbookId);
    }
}