using FableCraft.Infrastructure.Docker;

namespace FableCraft.Infrastructure.Clients;

internal sealed class MonitoredRagClient : IRagSearch, IRagBuilder
{
    private readonly RagClient _inner;
    private readonly IContainerMonitor _monitor;
    private readonly Guid? _adventureId;

    public MonitoredRagClient(RagClient inner, IContainerMonitor monitor, Guid? adventureId)
    {
        _inner = inner;
        _monitor = monitor;
        _adventureId = adventureId;
    }

    public async Task<SearchResult[]> SearchAsync(
        CallerContext context,
        IEnumerable<string> datasets,
        string[] query,
        SearchType? searchType = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _monitor.Increment(_adventureId);
            return await _inner.SearchAsync(context, datasets, query, searchType, cancellationToken);
        }
        finally
        {
            _monitor.Decrement(_adventureId);
        }
    }

    public async Task<Dictionary<string, Dictionary<string, string>>> AddDataAsync(
        List<string> content,
        List<string> datasets,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _monitor.Increment(_adventureId);
            return await _inner.AddDataAsync(content, datasets, cancellationToken);
        }
        finally
        {
            _monitor.Decrement(_adventureId);
        }
    }

    public async Task CognifyAsync(string[] datasets, bool temporal = false, CancellationToken cancellationToken = default)
    {
        try
        {
            _monitor.Increment(_adventureId);
            await _inner.CognifyAsync(datasets, temporal, cancellationToken);
        }
        finally
        {
            _monitor.Decrement(_adventureId);
        }
    }

    public async Task MemifyAsync(List<string> datasets, CancellationToken cancellationToken = default)
    {
        try
        {
            _monitor.Increment(_adventureId);
            await _inner.MemifyAsync(datasets, cancellationToken);
        }
        finally
        {
            _monitor.Decrement(_adventureId);
        }
    }

    public async Task<List<DatasetData>> GetDatasetsAsync(string dataset, CancellationToken cancellationToken = default)
    {
        try
        {
            _monitor.Increment(_adventureId);
            return await _inner.GetDatasetsAsync(dataset, cancellationToken);
        }
        finally
        {
            _monitor.Decrement(_adventureId);
        }
    }

    public async Task UpdateDataAsync(string dataset, string dataId, string content, CancellationToken cancellationToken = default)
    {
        try
        {
            _monitor.Increment(_adventureId);
            await _inner.UpdateDataAsync(dataset, dataId, content, cancellationToken);
        }
        finally
        {
            _monitor.Decrement(_adventureId);
        }
    }

    public async Task DeleteNodeAsync(string datasetName, string dataId, CancellationToken cancellationToken = default)
    {
        try
        {
            _monitor.Increment(_adventureId);
            await _inner.DeleteNodeAsync(datasetName, dataId, cancellationToken);
        }
        finally
        {
            _monitor.Decrement(_adventureId);
        }
    }

    public async Task DeleteDatasetAsync(string dataset, CancellationToken cancellationToken = default)
    {
        try
        {
            _monitor.Increment(_adventureId);
            await _inner.DeleteDatasetAsync(dataset, cancellationToken);
        }
        finally
        {
            _monitor.Decrement(_adventureId);
        }
    }

    public async Task CleanAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _monitor.Increment(_adventureId);
            await _inner.CleanAsync(cancellationToken);
        }
        finally
        {
            _monitor.Decrement(_adventureId);
        }
    }
}
