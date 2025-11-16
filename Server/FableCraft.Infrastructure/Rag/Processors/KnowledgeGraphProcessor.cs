using System.Net;

using FableCraft.Infrastructure.Clients;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities;

using Microsoft.EntityFrameworkCore;

using Polly;
using Polly.Retry;

using Serilog;

using TaskStatus = FableCraft.Infrastructure.Clients.TaskStatus;

namespace FableCraft.Infrastructure.Rag.Processors;

internal sealed class KnowledgeGraphProcessor : ITextProcessorHandler
{
    private const int MaxPollingAttempts = 600;
    private const int PollingIntervalSeconds = 5;
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger _logger;
    private readonly ResiliencePipeline<TaskStatusResponse> _pollingPipeline;
    private readonly IRagBuilder _ragBuilder;

    public KnowledgeGraphProcessor(ApplicationDbContext dbContext, IRagBuilder ragBuilder, ILogger logger)
    {
        _dbContext = dbContext;
        _ragBuilder = ragBuilder;
        _logger = logger;
        _pollingPipeline = new ResiliencePipelineBuilder<TaskStatusResponse>()
            .AddRetry(new RetryStrategyOptions<TaskStatusResponse>
            {
                MaxRetryAttempts = MaxPollingAttempts,
                Delay = TimeSpan.FromSeconds(PollingIntervalSeconds),
                BackoffType = DelayBackoffType.Constant,
                ShouldHandle = new PredicateBuilder<TaskStatusResponse>()
                    .HandleResult(response => response.Status == TaskStatus.Processing)
            })
            .AddTimeout(TimeSpan.FromSeconds(MaxPollingAttempts * PollingIntervalSeconds))
            .Build();
    }

    public async Task ProcessChunkAsync<TEntity>(Context<TEntity> context, CancellationToken cancellationToken) where TEntity : IKnowledgeGraphEntity
    {
        foreach (var processingContext in context.Chunks.OrderBy(x => x.Entity.Id))
        {
            foreach (Chunk chunk in processingContext.Chunks.OrderBy(x => x.Order))
            {
                switch (chunk.ProcessingStatus)
                {
                    case ProcessingStatus.Completed:
                        continue;

                    case ProcessingStatus.InProgress:
                        try
                        {
                            TaskStatusResponse statusResponse = await WaitForTaskCompletionAsync(chunk.Id.ToString(), cancellationToken);
                            await SetAsProcessedAsync(chunk, statusResponse.EpisodeId, cancellationToken);

                            _logger.Debug("Successfully added {EntityType} {EntityId} to knowledge graph with ID {KnowledgeGraphId}",
                                nameof(Chunk),
                                chunk.Id,
                                statusResponse.EpisodeId);
                        }
                        catch (HttpRequestException e) when (e.StatusCode == HttpStatusCode.NotFound)
                        {
                            // Task not found, will be reprocessed below
                        }
                        catch (System.Exception e)
                        {
                            _logger.Error(e,
                                "Failed to resume processing of {EntityType} {EntityId} which was InProgress",
                                nameof(Chunk),
                                chunk.Id);
                            await SetAsFailedAsync(chunk, cancellationToken);
                            throw;
                        }

                        break;

                    case ProcessingStatus.Failed:
                        throw new InvalidOperationException(
                            $"{nameof(Chunk)} {chunk.Id} is in Failed state. Retry the operation to reprocess.");
                }

                try
                {
                    _ = await _ragBuilder.AddDataAsync(new AddDataRequest
                    {
                        Content = chunk.GetContent(),
                        EpisodeType = chunk.ContentType,
                        Description = chunk.Description,
                        GroupId = context.AdventureId.ToString(),
                        TaskId = chunk.Id.ToString(),
                        ReferenceTime = chunk.ReferenceTime
                    });

                    await SetAsInProgressAsync(chunk, CancellationToken.None);
                    TaskStatusResponse statusResponse = await WaitForTaskCompletionAsync(chunk.Id.ToString(), cancellationToken);
                    await SetAsProcessedAsync(chunk, statusResponse.EpisodeId, cancellationToken);
                }
                catch (System.Exception)
                {
                    await SetAsFailedAsync(chunk, cancellationToken);
                    throw;
                }
            }
        }

        var taskId = context.AdventureId.ToString();
        await _ragBuilder.BuildCommunitiesAsync(context.AdventureId.ToString(), taskId, cancellationToken);
        await WaitForTaskCompletionAsync(taskId, cancellationToken);
    }

    private async Task<TaskStatusResponse> WaitForTaskCompletionAsync(string taskId, CancellationToken cancellationToken)
    {
        return await _pollingPipeline.ExecuteAsync(
            async token =>
            {
                TaskStatusResponse status = await _ragBuilder.GetTaskStatusAsync(taskId, token);

                if (status.Status == TaskStatus.Failed)
                {
                    throw new InvalidOperationException($"Task {taskId} failed: {status.Error}");
                }

                return status;
            },
            cancellationToken);
    }

    private async Task SetAsInProgressAsync(Chunk chunk, CancellationToken cancellationToken)
    {
        var dbSet = _dbContext.Set<Chunk>();
        try
        {
            await dbSet.Where(e => e.Id == chunk.Id)
                .ExecuteUpdateAsync(
                    x => x.SetProperty(e => e.ProcessingStatus, ProcessingStatus.InProgress),
                    cancellationToken);
        }
        catch (System.Exception ex)
        {
            _logger.Error(ex,
                "Failed to update {EntityType} {EntityId} to InProgress",
                nameof(Chunk),
                chunk.Id);
            throw;
        }
    }

    private async Task SetAsProcessedAsync(
        Chunk chunk,
        string knowledgeGraphNode,
        CancellationToken cancellationToken)
    {
        var dbSet = _dbContext.Set<Chunk>();
        try
        {
            await dbSet.Where(e => e.Id == chunk.Id)
                .ExecuteUpdateAsync(
                    x => x.SetProperty(e => e.ProcessingStatus, ProcessingStatus.Completed)
                        .SetProperty(e => e.KnowledgeGraphNodeId, knowledgeGraphNode),
                    cancellationToken);
        }
        catch (System.Exception ex)
        {
            _logger.Error(ex,
                "Failed to update {EntityType} {EntityId} with knowledge graph node {KnowledgeGraphNode}",
                nameof(Chunk),
                chunk.Id,
                knowledgeGraphNode);
            throw;
        }
    }

    private async Task SetAsFailedAsync(Chunk chunk, CancellationToken cancellationToken)
    {
        var dbSet = _dbContext.Set<Chunk>();
        try
        {
            await dbSet.Where(e => e.Id == chunk.Id)
                .ExecuteUpdateAsync(
                    x => x.SetProperty(e => e.ProcessingStatus, ProcessingStatus.Failed),
                    cancellationToken);
        }
        catch (System.Exception ex)
        {
            _logger.Error(ex,
                "Failed to update {EntityType} {EntityId} as failed",
                nameof(Chunk),
                chunk.Id);
            throw;
        }
    }
}