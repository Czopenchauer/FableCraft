using FableCraft.Infrastructure.Docker;

namespace FableCraft.Infrastructure.Clients;

internal sealed class MonitoredRagClient(
    RagClient inner,
    IContainerMonitor monitor,
    ContainerKey key)
    : IRagSearch, IRagBuilder
{
    public async Task<SearchResult[]> SearchAsync(
        CallerContext context,
        IEnumerable<string> datasets,
        string[] query,
        SearchType? searchType = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            monitor.Increment(key);
            return await inner.SearchAsync(context, datasets, query, searchType, cancellationToken);
        }
        finally
        {
            monitor.Decrement(key);
        }
    }

    public async Task<Dictionary<string, Dictionary<string, string>>> AddDataAsync(
        List<string> content,
        List<string> datasets,
        CancellationToken cancellationToken = default)
    {
        try
        {
            monitor.Increment(key);
            return await inner.AddDataAsync(content, datasets, cancellationToken);
        }
        finally
        {
            monitor.Decrement(key);
        }
    }

    public async Task CognifyAsync(string[] datasets, bool temporal = false, CancellationToken cancellationToken = default)
    {
        try
        {
            monitor.Increment(key);
            await inner.CognifyAsync(datasets, temporal, cancellationToken);
        }
        finally
        {
            monitor.Decrement(key);
        }
    }

    public async Task MemifyAsync(List<string> datasets, CancellationToken cancellationToken = default)
    {
        try
        {
            monitor.Increment(key);
            await inner.MemifyAsync(datasets, cancellationToken);
        }
        finally
        {
            monitor.Decrement(key);
        }
    }

    public async Task<List<DatasetData>> GetDatasetsAsync(string dataset, CancellationToken cancellationToken = default)
    {
        try
        {
            monitor.Increment(key);
            return await inner.GetDatasetsAsync(dataset, cancellationToken);
        }
        finally
        {
            monitor.Decrement(key);
        }
    }

    public async Task UpdateDataAsync(string dataset, string dataId, string content, CancellationToken cancellationToken = default)
    {
        try
        {
            monitor.Increment(key);
            await inner.UpdateDataAsync(dataset, dataId, content, cancellationToken);
        }
        finally
        {
            monitor.Decrement(key);
        }
    }

    public async Task DeleteNodeAsync(string datasetName, string dataId, CancellationToken cancellationToken = default)
    {
        try
        {
            monitor.Increment(key);
            await inner.DeleteNodeAsync(datasetName, dataId, cancellationToken);
        }
        finally
        {
            monitor.Decrement(key);
        }
    }

    public async Task DeleteDatasetAsync(string dataset, CancellationToken cancellationToken = default)
    {
        try
        {
            monitor.Increment(key);
            await inner.DeleteDatasetAsync(dataset, cancellationToken);
        }
        finally
        {
            monitor.Decrement(key);
        }
    }

    public async Task CleanAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            monitor.Increment(key);
            await inner.CleanAsync(cancellationToken);
        }
        finally
        {
            monitor.Decrement(key);
        }
    }
}