using System.IO.Hashing;
using System.Net;
using System.Text;
using System.Threading.RateLimiting;

using FableCraft.Infrastructure.Clients;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities;

using Microsoft.EntityFrameworkCore;

using Polly;
using Polly.Retry;

using Serilog;

namespace FableCraft.Infrastructure;

public record ChunkCreationRequest(Guid EntityId, string Content, ContentType ContentType, string[] DatasetName);

public interface IRagChunkService
{
    Task<List<Chunk>> CreateChunk(
        IEnumerable<ChunkCreationRequest> request,
        Guid adventureId,
        CancellationToken cancellationToken);

    Task CommitChunksToRagAsync(
        IRagBuilder ragBuilder,
        List<Chunk> chunks,
        CancellationToken cancellationToken);

    Task CognifyDatasetsAsync(
        IRagBuilder ragBuilder,
        string[] datasets,
        CancellationToken cancellationToken = default);

    Task DeleteNodes(
        IRagBuilder ragBuilder,
        Chunk[] chunks,
        CancellationToken cancellationToken = default);
}

internal sealed class RagChunkService : IRagChunkService
{
    private readonly ResiliencePipeline _httpResiliencePipeline;
    private readonly ResiliencePipeline _ioResiliencePipeline;

    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger _logger;

    public RagChunkService(ApplicationDbContext dbContext, ILogger logger)
    {
        _dbContext = dbContext;
        _logger = logger;

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
                MaxRetryAttempts = 2,
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

    private static string DataDirectory => Environment.GetEnvironmentVariable("FABLECRAFT_DATA_STORE")!;

    readonly struct ChunkKey(Guid entityId, string datasetName)
    {
        public Guid EntityId { get; } = entityId;

        public string DatasetName { get; } = datasetName;
    }

    public async Task<List<Chunk>> CreateChunk(
        IEnumerable<ChunkCreationRequest> request,
        Guid adventureId,
        CancellationToken cancellationToken)
    {
        if (!Directory.Exists(Path.Combine(DataDirectory, adventureId.ToString())))
        {
            Directory.CreateDirectory(Path.Combine(DataDirectory, adventureId.ToString()));
        }

        var existingChunks = await _dbContext.Chunks
            .AsNoTracking()
            .Where(x => request.Select(z => z.EntityId).Contains(x.EntityId))
            .ToDictionaryAsync(x => new ChunkKey(x.EntityId, x.DatasetName), cancellationToken: cancellationToken);

        var chunks = new List<Chunk>();
        foreach (var creationRequest in request)
        {
            foreach (var datasetName in creationRequest.DatasetName)
            {
                if (!existingChunks.ContainsKey(new ChunkKey(creationRequest.EntityId, datasetName)))
                {
                    var bytes = Encoding.UTF8.GetBytes(creationRequest.Content);
                    var hash = XxHash64.HashToUInt64(bytes);
                    var name = $"{hash:x16}";
                    var path = Path.Combine(DataDirectory, adventureId.ToString(), $"{name}.{creationRequest.ContentType}");

                    var chunk = new Chunk
                    {
                        EntityId = creationRequest.EntityId,
                        Name = name,
                        Path = path,
                        ContentType = creationRequest.ContentType.ToString(),
                        ContentHash = hash,
                        AdventureId = adventureId,
                        DatasetName = datasetName,
                        Content = creationRequest.Content,
                        KnowledgeGraphNodeId = null,
                    };
                    chunks.Add(chunk);
                }
            }
        }

        await Parallel.ForEachAsync(chunks,
            new ParallelOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount,
                CancellationToken = cancellationToken
            },
            (chunk, ct) =>
            {
                return _ioResiliencePipeline.ExecuteAsync(async c => await File.WriteAllTextAsync(chunk.Path, chunk.Content, Encoding.UTF8, c), ct);
            });

        return chunks;
    }

    public async Task CommitChunksToRagAsync(
        IRagBuilder ragBuilder,
        List<Chunk> chunks,
        CancellationToken cancellationToken)
    {
        var chunksByDataset = chunks.GroupBy(x => x.DatasetName);
        foreach (var datasetGroup in chunksByDataset)
        {
            var datasetName = datasetGroup.Key;
            var filePaths = datasetGroup.Select(x => x.Path).Distinct().ToList();

            var addResult = await _httpResiliencePipeline.ExecuteAsync(async ct =>
                    await ragBuilder.AddDataAsync(filePaths, [datasetName], ct),
                cancellationToken);

            foreach (var item in datasetGroup)
            {
                item.KnowledgeGraphNodeId = addResult[datasetName][item.Name];
            }
        }
    }

    public async Task CognifyDatasetsAsync(
        IRagBuilder ragBuilder,
        string[] datasets,
        CancellationToken cancellationToken = default)
    {
        await _httpResiliencePipeline.ExecuteAsync(async ct =>
                await ragBuilder.CognifyAsync(datasets, temporal: false, ct),
            cancellationToken);
    }

    public async Task DeleteNodes(IRagBuilder ragBuilder, Chunk[] chunks, CancellationToken cancellationToken = default)
    {
        var eligibleChunks = chunks.Where(x => !string.IsNullOrEmpty(x.KnowledgeGraphNodeId)).ToArray();
        if (!eligibleChunks.Any())
        {
            _logger.Information("No eligible chunks found.");
            return;
        }

        _logger.Information("Deleting {count} eligible chunks...", eligibleChunks.Length);
        await Parallel.ForEachAsync(eligibleChunks,
            new ParallelOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount,
                CancellationToken = cancellationToken
            },
            (chunk, ct) =>
            {
                return _httpResiliencePipeline.ExecuteAsync(async c => await ragBuilder.DeleteNodeAsync(chunk.DatasetName, chunk.KnowledgeGraphNodeId!, c), ct);
            });
    }
}