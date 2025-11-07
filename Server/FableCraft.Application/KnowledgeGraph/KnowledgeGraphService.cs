using System.Net;
using System.Text.Json;
using System.Threading.RateLimiting;

using FableCraft.Application.AdventureGeneration;
using FableCraft.Infrastructure.Clients;
using FableCraft.Infrastructure.Llm;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

using Polly;
using Polly.Retry;

using Serilog;

using IKernelBuilder = FableCraft.Infrastructure.Llm.IKernelBuilder;

namespace FableCraft.Application.KnowledgeGraph;

internal class KnowledgeGraphService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IRagBuilder _ragBuilder;
    private readonly IKernelBuilder _kernelBuilder;
    private readonly AdventureCreationConfig _config;
    private readonly ILogger _logger;

    private readonly ResiliencePipeline _resiliencePipeline;
    private const int MaxRetryAttempts = 3;
    private const int MaxPollingAttempts = 600;
    private const int PollingIntervalSeconds = 5;

    public KnowledgeGraphService(
        ApplicationDbContext dbContext,
        IRagBuilder ragBuilder,
        IKernelBuilder kernelBuilder,
        IOptions<AdventureCreationConfig> config,
        ILogger logger)
    {
        _dbContext = dbContext;
        _ragBuilder = ragBuilder;
        _kernelBuilder = kernelBuilder;
        _config = config.Value;
        _logger = logger;

        _resiliencePipeline = new ResiliencePipelineBuilder()
            .AddConcurrencyLimiter(new ConcurrencyLimiterOptions
            {
                PermitLimit = 10,
                QueueLimit = 200,
            })
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder()
                    .Handle<InvalidCastException>()
                    .Handle<HttpRequestException>(e => e.StatusCode == HttpStatusCode.TooManyRequests)
                    .Handle<LlmEmptyResponseException>(),
                MaxRetryAttempts = MaxRetryAttempts,
                Delay = TimeSpan.FromSeconds(5)
            })
            .Build();
    }

    public async Task<List<string>> ChunkTextAsync(string text, int maxChunkSize, CancellationToken cancellationToken)
    {
        var kernel = _kernelBuilder.WithBase(_config.LlmModel).Build();
        var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
        var chatHistory = new ChatHistory();

        chatHistory.AddUserMessage($"""
                                    You are a text chunking specialist. Your task is to split the provided text into logical, meaningful chunks while preserving context and readability.

                                    # Instructions
                                    1. Analyze the input text structure and identify natural boundaries (paragraphs, sections, topic shifts)
                                    2. Split the text into chunks that:
                                       - Maintain semantic coherence (each chunk covers a complete thought or topic)
                                       - Stay within the specified size limit of {maxChunkSize} characters
                                       - Preserve context by avoiding mid-sentence breaks when possible
                                       - Keep related information together
                                       - avoid modifying text where possible
                                       - split entire text

                                    3. For each chunk, ensure:
                                       - It can stand alone with minimal context loss
                                       - Transitions between chunks are clear
                                       - Important entities or concepts introduced in earlier chunks are referenced if needed

                                    # Output Format

                                    Return the chunks as a array in json format. Respond only with json array and nothing else.
                                    """);

        chatHistory.AddUserMessage(text);

        var promptExecutionSettings = new OpenAIPromptExecutionSettings
        {
            Temperature = _config.Temperature,
            PresencePenalty = _config.PresencePenalty,
            FrequencyPenalty = _config.FrequencyPenalty,
            MaxTokens = _config.MaxTokens,
            TopP = _config.TopP,
        };

        var pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder()
                    .Handle<JsonException>()
                    .Handle<LlmEmptyResponseException>(),
                MaxRetryAttempts = MaxRetryAttempts,
                Delay = TimeSpan.FromSeconds(5)
            })
            .Build();

        var chunkedText = await pipeline.ExecuteAsync(async token =>
            {
                var result = await chatCompletionService.GetChatMessageContentAsync(
                    chatHistory,
                    promptExecutionSettings,
                    kernel,
                    token);

                var replyInnerContent = result.InnerContent as OpenAI.Chat.ChatCompletion;
                _logger.Information("ChunkText - Input: {usage}, Output: {output}, Total: {total}",
                    replyInnerContent?.Usage.InputTokenCount,
                    replyInnerContent?.Usage.OutputTokenCount,
                    replyInnerContent?.Usage.TotalTokenCount);

                _logger.Debug("ChunkText response: {response}", JsonSerializer.Serialize(result));

                var sanitized = result.Content?.RemoveThinkingBlock()
                    .Replace("```json", "")
                    .Replace("```", "")
                    .Trim();

                if (string.IsNullOrEmpty(sanitized))
                {
                    throw new LlmEmptyResponseException();
                }

                return JsonSerializer.Deserialize<List<string>>(sanitized);
            },
            cancellationToken);

        return chunkedText!;
    }

    public async Task<string> ContextualizeChunkAsync(string chunk, string fullText, CancellationToken cancellationToken)
    {
        var kernel = _kernelBuilder.WithBase(_config.LlmModel).Build();
        var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
        var chatHistory = new ChatHistory();

        chatHistory.AddUserMessage($"""
                                    <document>
                                    {fullText}
                                    </document>
                                    """);

        chatHistory.AddUserMessage($"""
                                    Here is the chunk we want to situate within
                                    the whole document:
                                    <chunk>
                                    {chunk}
                                    </chunk>
                                    Please give a short succinct context to situate
                                    this chunk within the overall document for the
                                    purposes of improving search retrieval of the
                                    chunk. If the document has a publication date,
                                    please include the date in your context. Answer
                                    only with the succinct context and nothing else.
                                    """);

        var promptExecutionSettings = new OpenAIPromptExecutionSettings
        {
            Temperature = _config.Temperature,
            PresencePenalty = _config.PresencePenalty,
            FrequencyPenalty = _config.FrequencyPenalty,
            MaxTokens = _config.MaxTokens,
            TopP = _config.TopP,
        };

        var contextualizedText = await _resiliencePipeline.ExecuteAsync(async token =>
            {
                var result = await chatCompletionService.GetChatMessageContentAsync(
                    chatHistory,
                    promptExecutionSettings,
                    kernel,
                    token);

                var replyInnerContent = result.InnerContent as OpenAI.Chat.ChatCompletion;
                _logger.Information("ContextualizeChunk - Input: {usage}, Output: {output}, Total: {total}",
                    replyInnerContent?.Usage.InputTokenCount,
                    replyInnerContent?.Usage.OutputTokenCount,
                    replyInnerContent?.Usage.TotalTokenCount);

                var sanitized = result.Content?.RemoveThinkingBlock();

                if (string.IsNullOrEmpty(sanitized))
                {
                    throw new LlmEmptyResponseException();
                }

                _logger.Debug("ContextualizeChunk response: {response}", JsonSerializer.Serialize(result));
                return sanitized;
            },
            cancellationToken);

        return contextualizedText;
    }

    public async Task ProcessChunksAsync(
        Guid groupId,
        List<Chunk> chunks,
        CancellationToken cancellationToken)
    {
        foreach (var chunk in chunks.OrderBy(x => x.Order))
        {
            switch (chunk.ProcessingStatus)
            {
                case ProcessingStatus.Completed:
                    continue;

                case ProcessingStatus.InProgress:
                    try
                    {
                        var statusResponse = await WaitForTaskCompletionAsync(chunk.Id.ToString(), cancellationToken);
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
                    catch (Exception e)
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
                    Content = $"{chunk.ContextualizedChunk}\n{chunk.RawChunk}",
                    EpisodeType = nameof(DataType.Text),
                    Description = chunk.Description,
                    GroupId = groupId.ToString(),
                    TaskId = chunk.Id.ToString(),
                    ReferenceTime = DateTime.UtcNow
                });

                _logger.Debug("Task {TaskId} queued for {EntityType} {EntityId}",
                    chunk.Id, nameof(Chunk), chunk.Id);

                await SetAsInProgressAsync(chunk, CancellationToken.None);
                var statusResponse = await WaitForTaskCompletionAsync(chunk.Id.ToString(), cancellationToken);
                await SetAsProcessedAsync(chunk, statusResponse.EpisodeId, cancellationToken);

                _logger.Debug("Successfully added {EntityType} {EntityId} to knowledge graph with ID {KnowledgeGraphId}",
                    nameof(Chunk),
                    chunk.Id,
                    statusResponse.EpisodeId);
            }
            catch (Exception ex)
            {
                _logger.Error(ex,
                    "Failed to add {EntityType} {EntityId} to knowledge graph after {MaxRetryAttempts} attempts",
                    nameof(Chunk),
                    chunk.Id,
                    MaxRetryAttempts);
                await SetAsFailedAsync(chunk, cancellationToken);
                throw;
            }
        }
    }

    public async Task ResetFailedChunksAsync(List<Chunk> chunks, CancellationToken cancellationToken)
    {
        var failedChunks = chunks.Where(x => x.ProcessingStatus == ProcessingStatus.Failed).ToList();
        if (!failedChunks.Any())
        {
            return;
        }

        var committedChunks = failedChunks
            .Where(x => !string.IsNullOrEmpty(x.KnowledgeGraphNodeId))
            .Select(x => new
            {
                Chunk = x,
                EpisodeId = Task.Run(() => _ragBuilder.GetEpisodeAsync(x.KnowledgeGraphNodeId!.ToString(), cancellationToken),
                    cancellationToken)
            });

        foreach (var chunk in committedChunks)
        {
            var dbSet = _dbContext.Chunks.OfType<Chunk>();
            try
            {
                var chunkEpisodeId = await chunk.EpisodeId;
                await dbSet.Where(e => e.Id == chunk.Chunk.Id)
                    .ExecuteUpdateAsync(
                        x => x.SetProperty(e => e.ProcessingStatus, ProcessingStatus.Completed)
                            .SetProperty(e => e.KnowledgeGraphNodeId, chunkEpisodeId.Uuid),
                        cancellationToken);

                _logger.Debug("Successfully retrieved episode for Chunk {chunkId}", chunk.Chunk.Id);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                await dbSet.Where(e => e.Id == chunk.Chunk.Id)
                    .ExecuteUpdateAsync(
                        x => x.SetProperty(e => e.ProcessingStatus, ProcessingStatus.Pending),
                        cancellationToken);
            }
        }
    }

    private async Task<TaskStatusResponse> WaitForTaskCompletionAsync(string taskId, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < MaxPollingAttempts; attempt++)
        {
            var status = await _ragBuilder.GetTaskStatusAsync(taskId, cancellationToken);

            switch (status.Status)
            {
                case Infrastructure.Clients.TaskStatus.Completed:
                    return status;

                case Infrastructure.Clients.TaskStatus.Failed:
                    throw new InvalidOperationException($"Task {taskId} failed: {status.Error}");

                case Infrastructure.Clients.TaskStatus.Pending:
                case Infrastructure.Clients.TaskStatus.Processing:
                    await Task.Delay(TimeSpan.FromSeconds(PollingIntervalSeconds), cancellationToken);
                    break;
            }
        }

        throw new TimeoutException($"Task {taskId} did not complete within the expected time");
    }

    public async Task BuildCommunitiesAsync(string groupId, CancellationToken cancellationToken)
    {
        await _ragBuilder.BuildCommunitiesAsync(groupId, groupId, cancellationToken);
        await WaitForTaskCompletionAsync(groupId, cancellationToken);
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
        catch (Exception ex)
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
        catch (Exception ex)
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
        catch (Exception ex)
        {
            _logger.Error(ex,
                "Failed to update {EntityType} {EntityId} as failed",
                nameof(Chunk),
                chunk.Id);
            throw;
        }
    }
}
