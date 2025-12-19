using System.IO.Hashing;
using System.Net;
using System.Text;
using System.Threading.RateLimiting;

using FableCraft.Infrastructure.Clients;
using FableCraft.Infrastructure.Persistence.Entities;

using Polly;
using Polly.Retry;

namespace FableCraft.Infrastructure;

public record FileToWrite(Chunk Chunk, string Content, string[] Datasets);

public interface IRagChunkService
{
    Chunk CreateChunk(
        Guid entityId,
        Guid adventureId,
        string content,
        ContentType contentType);

    Task WriteFilesAsync(IEnumerable<FileToWrite> files, CancellationToken cancellationToken);

    Task CommitChunksToRagAsync(
        List<FileToWrite> files,
        CancellationToken cancellationToken);

    Task UpdateExistingChunksAsync(
        List<FileToWrite> files,
        CancellationToken cancellationToken);

    Task CognifyDatasetsAsync(
        IEnumerable<string> datasets,
        bool temporal = false,
        CancellationToken cancellationToken = default);

    void EnsureDirectoryExists(Guid adventureId);

    ulong ComputeHash(string content);

    string GetChunkPath(Guid adventureId, ulong hash, ContentType contentType);
}

internal sealed class RagChunkService : IRagChunkService
{
    public static string DataDirectory => Environment.GetEnvironmentVariable("FABLECRAFT_DATA_STORE")!;

    private readonly IRagBuilder _ragBuilder;

    private readonly ResiliencePipeline _httpResiliencePipeline;
    private readonly ResiliencePipeline _ioResiliencePipeline;

    public RagChunkService(IRagBuilder ragBuilder)
    {
        _ragBuilder = ragBuilder;

        _httpResiliencePipeline = new ResiliencePipelineBuilder()
            .AddRateLimiter(new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 5,
                Window = TimeSpan.FromSeconds(1),
                QueueLimit = 1000
            }))
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(2),
                BackoffType = DelayBackoffType.Exponential,
                ShouldHandle = new PredicateBuilder().Handle<HttpRequestException>(ex =>
                    ex.StatusCode is HttpStatusCode.InternalServerError or HttpStatusCode.TooManyRequests)
            })
            .Build();

        _ioResiliencePipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromMilliseconds(500),
                BackoffType = DelayBackoffType.Exponential,
                ShouldHandle = new PredicateBuilder()
                    .Handle<IOException>()
            })
            .Build();
    }

    public void EnsureDirectoryExists(Guid adventureId)
    {
        var path = Path.Combine(DataDirectory, adventureId.ToString());
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }

    public ulong ComputeHash(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        return XxHash64.HashToUInt64(bytes);
    }

    public string GetChunkPath(Guid adventureId, ulong hash, ContentType contentType)
    {
        var name = $"{hash:x16}";
        return Path.Combine(DataDirectory, adventureId.ToString(), $"{name}.{contentType}");
    }

    public Chunk CreateChunk(
        Guid entityId,
        Guid adventureId,
        string content,
        ContentType contentType)
    {
        var hash = ComputeHash(content);
        var name = $"{hash:x16}";
        var path = GetChunkPath(adventureId, hash, contentType);

        return new Chunk
        {
            EntityId = entityId,
            Name = name,
            Path = path,
            ContentType = contentType.ToString(),
            ContentHash = hash,
            AdventureId = adventureId,
            ChunkLocation = null
        };
    }

    public async Task WriteFilesAsync(IEnumerable<FileToWrite> files, CancellationToken cancellationToken)
    {
        await _ioResiliencePipeline.ExecuteAsync(async ct =>
            {
                await Task.WhenAll(files.Select(x =>
                    File.WriteAllTextAsync(x.Chunk.Path, x.Content, ct)).ToList());
            },
            cancellationToken);
    }

    public async Task UpdateExistingChunksAsync(
        List<FileToWrite> files,
        CancellationToken cancellationToken)
    {
        var updateTasks = files
            .Where(file => file.Chunk.ChunkLocation != null)
            .SelectMany(file => file.Datasets.Select(dataset => (File: file, Dataset: dataset)))
            .Select(async item =>
            {
                var chunkLocation = item.File.Chunk.ChunkLocation!.FirstOrDefault(x => x.DatasetName == item.Dataset);
                if (string.IsNullOrEmpty(chunkLocation?.KnowledgeGraphNodeId))
                {
                    return;
                }

                try
                {
                    await _httpResiliencePipeline.ExecuteAsync(async ct =>
                            await _ragBuilder.UpdateDataAsync(item.Dataset, chunkLocation.KnowledgeGraphNodeId, item.File.Chunk.Path, ct),
                        cancellationToken);
                }
                catch (HttpRequestException e) when (e.StatusCode == HttpStatusCode.NotFound)
                {
                    item.File.Chunk.ChunkLocation!.Remove(chunkLocation);
                }
            });

        await Task.WhenAll(updateTasks);
    }

    public async Task CommitChunksToRagAsync(
        List<FileToWrite> files,
        CancellationToken cancellationToken)
    {
        var filesByDatasets = files
            .SelectMany(f => f.Datasets.Select(d => (File: f, Dataset: d)))
            .GroupBy(x => x.Dataset);

        foreach (var datasetGroup in filesByDatasets)
        {
            var datasetName = datasetGroup.Key;
            var filePaths = datasetGroup.Select(x => x.File.Chunk.Path).Distinct().ToList();

            var addResult = await _httpResiliencePipeline.ExecuteAsync(async ct =>
                    await _ragBuilder.AddDataAsync(filePaths, [datasetName], ct),
                cancellationToken);

            foreach (var item in datasetGroup)
            {
                var chunk = item.File.Chunk;
                var newLocation = new ChunkLocation
                {
                    DatasetName = datasetName,
                    KnowledgeGraphNodeId = addResult[datasetName][chunk.Name]
                };

                chunk.ChunkLocation ??= [];

                var existingLocation = chunk.ChunkLocation.FirstOrDefault(x => x.DatasetName == datasetName);
                if (existingLocation != null)
                {
                    existingLocation.KnowledgeGraphNodeId = newLocation.KnowledgeGraphNodeId;
                }
                else
                {
                    chunk.ChunkLocation.Add(newLocation);
                }
            }
        }
    }

    public async Task CognifyDatasetsAsync(
        IEnumerable<string> datasets,
        bool temporal = false,
        CancellationToken cancellationToken = default)
    {
        foreach (var dataset in datasets.Distinct())
        {
            await _httpResiliencePipeline.ExecuteAsync(async ct =>
                    await _ragBuilder.CognifyAsync([dataset], temporal, ct),
                cancellationToken);
        }
    }
}