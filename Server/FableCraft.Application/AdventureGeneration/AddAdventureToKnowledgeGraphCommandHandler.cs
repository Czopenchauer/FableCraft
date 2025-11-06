using System.Diagnostics;
using System.Net;
using System.Text.Json;
using System.Threading.RateLimiting;

using FableCraft.Infrastructure.Clients;
using FableCraft.Infrastructure.Llm;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities;
using FableCraft.Infrastructure.Queue;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

using Polly;
using Polly.Retry;

using Serilog;

using IKernelBuilder = FableCraft.Infrastructure.Llm.IKernelBuilder;

namespace FableCraft.Application.AdventureGeneration;

public class AddAdventureToKnowledgeGraphCommand : IMessage
{
    public Guid AdventureId { get; init; }
}

internal class AddAdventureToKnowledgeGraphCommandHandler(
    IMessageDispatcher messageDispatcher,
    ApplicationDbContext dbContext,
    IRagBuilder ragBuilder,
    IKernelBuilder kernelBuilder,
    IOptions<AdventureCreationConfig> config,
    ILogger logger)
    : IMessageHandler<AddAdventureToKnowledgeGraphCommand>
{
    private const int MaxRetryAttempts = 3;

    private readonly ResiliencePipeline _resiliencePipeline = new ResiliencePipelineBuilder()
        .AddConcurrencyLimiter(new ConcurrencyLimiterOptions
        {
            PermitLimit = 20,
            QueueLimit = 200,
        })
        .AddRetry(new RetryStrategyOptions
        {
            ShouldHandle = new PredicateBuilder()
                .Handle<InvalidCastException>()
                .Handle<HttpRequestException>(e => e.StatusCode == HttpStatusCode.TooManyRequests)
                .Handle<LlmEmptyResponseException>(),
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromSeconds(5)
        })
        .Build();

    public async Task HandleAsync(AddAdventureToKnowledgeGraphCommand message, CancellationToken cancellationToken)
    {
        var adventure = await dbContext.Adventures
            .Include(x => x.Character)
            .Include(x => x.Lorebook)
            .SingleAsync(x => x.Id == message.AdventureId, cancellationToken: cancellationToken);

        // Reset any previously failed chunks to pending to retry processing
        var existingCharacterChunks = await dbContext.Chunks.Where(x => x.EntityId == adventure.CharacterId).ToListAsync(cancellationToken: cancellationToken);
        var existingLorebooksChunks = await dbContext.Chunks
            .Where(x => adventure.Lorebook.Select(y => y.Id)
                .Contains(x.EntityId))
            .ToListAsync(cancellationToken: cancellationToken);

        await ResetFailedToPendingAsync(existingCharacterChunks, cancellationToken);
        await ResetFailedToPendingAsync(existingLorebooksChunks, cancellationToken);

        // Apply chunking
        var lorebookToProcess = adventure.Lorebook.Where(x => existingLorebooksChunks.All(y => y.EntityId != x.Id)).ToList();
        foreach (var lorebook in lorebookToProcess)
        {
            var chunkedText = await ChunkText(lorebook.Content, cancellationToken);
            var lorebookChunks = chunkedText.Select(text => new Chunk
            {
                RawChunk = text,
                EntityId = lorebook.Id,
                ProcessingStatus = ProcessingStatus.Pending,
                Name = lorebook.Category,
                Description = lorebook.Description,
            });
            dbContext.Chunks.AddRange(lorebookChunks);
            await dbContext.SaveChangesAsync(CancellationToken.None);
            existingLorebooksChunks.AddRange(lorebookChunks);
        }

        Debug.Assert(existingLorebooksChunks.All(x => x.Id != Guid.Empty));
        var lorebookChunksGrouped = existingLorebooksChunks
            .Join(adventure.Lorebook,
                chunk => chunk.EntityId,
                lorebook => lorebook.Id,
                (chunk, lorebook) => new
                {
                    LorebookChunk = lorebook,
                    Chunk = chunk,
                });
        foreach (var chunk in lorebookChunksGrouped.Where(x => string.IsNullOrEmpty(x.Chunk.ContextualizedChunk)))
        {
            var contextualizeChunk = await ContextualizeChunk(chunk.Chunk.RawChunk, chunk.LorebookChunk.Content, cancellationToken);
            chunk.Chunk.ContextualizedChunk = contextualizeChunk;
            await dbContext.Chunks
                .Where(c => c.Id == chunk.Chunk.Id)
                .ExecuteUpdateAsync(
                    x => x.SetProperty(c => c.ContextualizedChunk, contextualizeChunk),
                    cancellationToken);
        }

        var entireCharacterText = $"""
                                   Main Character Description: {adventure.Character.Description}

                                   Main Character Background: {adventure.Character.Background}
                                   """;
        if (!existingCharacterChunks.Any())
        {
            var chunkedText = await ChunkText(entireCharacterText,
                cancellationToken);
            var characterChunks = chunkedText.Select(text => new Chunk
            {
                RawChunk = text,
                EntityId = adventure.CharacterId,
                ProcessingStatus = ProcessingStatus.Pending,
                Name = $"Main Character, {adventure.Character.Name}",
                Description = $"Main Character, {adventure.Character.Name}, Description",
            });
            dbContext.Chunks.AddRange(characterChunks);
            await dbContext.SaveChangesAsync(CancellationToken.None);
            existingCharacterChunks.AddRange(characterChunks);
        }

        Debug.Assert(existingCharacterChunks.All(x => x.Id != Guid.Empty));
        foreach (var characterChunk in existingCharacterChunks.Where(x => string.IsNullOrEmpty(x.ContextualizedChunk)))
        {
            var contextualizeChunk = await ContextualizeChunk(characterChunk.RawChunk, entireCharacterText, cancellationToken);
            characterChunk.ContextualizedChunk = contextualizeChunk;
            await dbContext.Chunks
                .Where(c => c.Id == characterChunk.Id)
                .ExecuteUpdateAsync(
                    x => x.SetProperty(c => c.ContextualizedChunk, contextualizeChunk),
                    cancellationToken);
        }

        await ProcessChunksAsync(adventure.Id, existingCharacterChunks, cancellationToken);
        await ProcessChunksAsync(adventure.Id, lorebookChunksGrouped.Select(x => x.Chunk).ToList(), cancellationToken);

        await messageDispatcher.PublishAsync(new AdventureCreatedEvent
            {
                AdventureId = adventure.Id
            },
            cancellationToken);
    }

    private async Task<List<string>> ChunkText(string text, CancellationToken cancellationToken)
    {
        const int maxChunkSize = 250;
        var kernel = kernelBuilder.WithBase(config.Value.LlmModel).Build();
        var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
        var chatHistory = new ChatHistory();
        chatHistory.AddUserMessage($"""
                                    You are a text chunking specialist. Your task is to split the provided text into logical, meaningful chunks while preserving context and readability.

                                    # Instructions
                                    1. Analyze the input text structure and identify natural boundaries (paragraphs, sections, topic shifts)
                                    2. Split the text into chunks that:
                                       - Maintain semantic coherence (each chunk covers a complete thought or topic)
                                       - Stay within the specified size limit of {maxChunkSize} tokens
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
            Temperature = config.Value.Temperature,
            PresencePenalty = config.Value.PresencePenalty,
            FrequencyPenalty = config.Value.FrequencyPenalty,
            MaxTokens = config.Value.MaxTokens,
            TopP = config.Value.TopP,
        };
        ResiliencePipeline pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<JsonException>().Handle<LlmEmptyResponseException>(),
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(5)
            })
            .Build();
        var chunkedText = await pipeline.ExecuteAsync(async token =>
            {
                var result = await chatCompletionService.GetChatMessageContentAsync(chatHistory, promptExecutionSettings, kernel, token);
                var replyInnerContent = result.InnerContent as OpenAI.Chat.ChatCompletion;
                logger.Information("Input usage: {usage}, output usage {output}, total usage {total}",
                    replyInnerContent?.Usage.InputTokenCount,
                    replyInnerContent?.Usage.OutputTokenCount,
                    replyInnerContent?.Usage.TotalTokenCount);
                logger.Debug("Generated response: {response}", JsonSerializer.Serialize(result));
                var sanitized = result.Content?.RemoveThinkingBlock().Replace("```json", "").Replace("```", "").Trim();
                if (string.IsNullOrEmpty(sanitized))
                {
                    throw new LlmEmptyResponseException();
                }

                return JsonSerializer.Deserialize<List<string>>(sanitized);
            },
            cancellationToken);

        return chunkedText!;
    }

    private async Task<string> ContextualizeChunk(string chunk, string text, CancellationToken cancellationToken)
    {
        var kernel = kernelBuilder.WithBase(config.Value.LlmModel).Build();
        var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
        var chatHistory = new ChatHistory();
        chatHistory.AddUserMessage($"""
                                    <document>
                                    {text}
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
            Temperature = config.Value.Temperature,
            PresencePenalty = config.Value.PresencePenalty,
            FrequencyPenalty = config.Value.FrequencyPenalty,
            MaxTokens = config.Value.MaxTokens,
            TopP = config.Value.TopP,
        };

        var chunkedText = await _resiliencePipeline.ExecuteAsync(async token =>
            {
                var result = await chatCompletionService.GetChatMessageContentAsync(chatHistory, promptExecutionSettings, kernel, token);
                var replyInnerContent = result.InnerContent as OpenAI.Chat.ChatCompletion;
                logger.Information("Input usage: {usage}, output usage {output}, total usage {total}",
                    replyInnerContent?.Usage.InputTokenCount,
                    replyInnerContent?.Usage.OutputTokenCount,
                    replyInnerContent?.Usage.TotalTokenCount);
                var sanitized = result.Content?.RemoveThinkingBlock();
                if (string.IsNullOrEmpty(sanitized))
                {
                    throw new LlmEmptyResponseException();
                }

                logger.Debug("Generated response: {response}", JsonSerializer.Serialize(result));
                return sanitized;
            },
            cancellationToken);

        return chunkedText;
    }

    private async Task ResetFailedToPendingAsync(List<Chunk> chunks, CancellationToken cancellationToken)
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
                EpisodeId = Task.Run(() => ragBuilder.GetEpisodeAsync(x.KnowledgeGraphNodeId!.ToString(), cancellationToken), cancellationToken)
            });

        foreach (var chunk in committedChunks)
        {
            var dbSet = dbContext.Chunks.OfType<Chunk>();
            try
            {
                var chunkEpisodeId = await chunk.EpisodeId;
                await dbSet.Where(e => e.Id == chunk.Chunk.Id)
                    .ExecuteUpdateAsync(
                        x => x.SetProperty(e => e.ProcessingStatus, ProcessingStatus.Completed)
                            .SetProperty(e => e.KnowledgeGraphNodeId, chunkEpisodeId.Uuid),
                        cancellationToken);
                logger.Debug("Successfully retrieved episode for Chunk {chunkId}", chunk.Chunk.Id);
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

    private async Task ProcessChunksAsync(
        Guid adventureId,
        List<Chunk> entities,
        CancellationToken cancellationToken)
    {
        foreach (var entity in entities)
        {
            switch (entity.ProcessingStatus)
            {
                case ProcessingStatus.Completed:
                    return;
                case ProcessingStatus.InProgress:
                    try
                    {
                        var knowledgeGraphId = await WaitForTaskCompletionAsync(entity.Id.ToString(), cancellationToken);

                        await SetAsProcessed(entity, knowledgeGraphId, cancellationToken);
                        logger.Debug("Successfully added {EntityType} {EntityId} to knowledge graph with ID {KnowledgeGraphId}",
                            nameof(Chunk),
                            entity.Id,
                            knowledgeGraphId);
                    }
                    catch (HttpRequestException e) when (e.StatusCode == HttpStatusCode.NotFound)
                    {
                        return;
                    }
                    catch (Exception e)
                    {
                        logger.Error(e,
                            "Failed to resume processing of {EntityType} {EntityId} which was InProgress",
                            nameof(Chunk),
                            entity.Id);
                        await SetAsFailed(entity, cancellationToken);
                        throw;
                    }

                    break;
                case ProcessingStatus.Failed:
                    throw new InvalidOperationException(
                        $"{nameof(Chunk)} {entity.Id} is in Failed state. Retry the operation to reprocess.");
            }

            try
            {
                // Task cancellation is not supported in upstream API
                _ = await ragBuilder.AddDataAsync(new AddDataRequest
                {
                    Content = $"""
                               {entity.ContextualizedChunk}
                               {entity.RawChunk}
                               """,
                    EpisodeType = nameof(DataType.Text),
                    Description = entity.Description,
                    GroupId = adventureId.ToString(),
                    TaskId = entity.Id.ToString(),
                    ReferenceTime = DateTime.UtcNow
                });
                logger.Debug("Task {TaskId} queued for {EntityType} {EntityId}", entity.Id, nameof(Chunk), entity.Id);

                await SetAsInProgress(entity, CancellationToken.None);

                var knowledgeGraphId = await WaitForTaskCompletionAsync(entity.Id.ToString(), cancellationToken);

                await SetAsProcessed(entity, knowledgeGraphId, cancellationToken);
                logger.Debug("Successfully added {EntityType} {EntityId} to knowledge graph with ID {KnowledgeGraphId}",
                    nameof(Chunk),
                    entity.Id,
                    knowledgeGraphId);
            }
            catch (Exception ex)
            {
                logger.Error(ex,
                    "Failed to add {EntityType} {EntityId} to knowledge graph after {MaxRetryAttempts} attempts",
                    nameof(Chunk),
                    entity.Id,
                    MaxRetryAttempts);
                await SetAsFailed(entity, cancellationToken);
                throw;
            }
        }
    }

    private async Task<string> WaitForTaskCompletionAsync(string taskId, CancellationToken cancellationToken)
    {
        const int maxPollingAttempts = 600;
        const int pollingIntervalSeconds = 5;

        for (int attempt = 0; attempt < maxPollingAttempts; attempt++)
        {
            var status = await ragBuilder.GetTaskStatusAsync(taskId, cancellationToken);

            switch (status.Status)
            {
                case Infrastructure.Clients.TaskStatus.Completed:
                    return status.EpisodeId;

                case Infrastructure.Clients.TaskStatus.Failed:
                    throw new InvalidOperationException($"Task {taskId} failed: {status.Error}");

                case Infrastructure.Clients.TaskStatus.Pending:
                case Infrastructure.Clients.TaskStatus.Processing:
                    await Task.Delay(TimeSpan.FromSeconds(pollingIntervalSeconds), cancellationToken);
                    break;
            }
        }

        throw new TimeoutException($"Task {taskId} did not complete within the expected time");
    }

    private async Task SetAsInProgress(
        Chunk entity,
        CancellationToken cancellationToken)
    {
        var dbSet = dbContext.Set<Chunk>();
        try
        {
            await dbSet.Where(e => e.Id == entity.Id)
                .ExecuteUpdateAsync(
                    x => x.SetProperty(e => e.ProcessingStatus, ProcessingStatus.InProgress),
                    cancellationToken);
        }
        catch (Exception ex)
        {
            logger.Error(ex,
                "Failed to update {EntityType} {EntityId} to InProgress",
                nameof(Chunk),
                entity.Id);
            throw;
        }
    }

    private async Task SetAsProcessed(
        Chunk entity,
        string knowledgeGraphNode,
        CancellationToken cancellationToken)
    {
        var dbSet = dbContext.Set<Chunk>();
        try
        {
            await dbSet.Where(e => e.Id == entity.Id)
                .ExecuteUpdateAsync(
                    x => x.SetProperty(e => e.ProcessingStatus, ProcessingStatus.Completed)
                        .SetProperty(e => e.KnowledgeGraphNodeId, knowledgeGraphNode),
                    cancellationToken);
        }
        catch (Exception ex)
        {
            logger.Error(ex,
                "Failed to update {EntityType} {EntityId} with knowledge graph node {KnowledgeGraphNode}",
                nameof(Chunk),
                entity.Id,
                knowledgeGraphNode);
            throw;
        }
    }

    private async Task SetAsFailed(
        Chunk entity,
        CancellationToken cancellationToken)
    {
        var dbSet = dbContext.Set<Chunk>();
        try
        {
            await dbSet.Where(e => e.Id == entity.Id)
                .ExecuteUpdateAsync(
                    x => x.SetProperty(e => e.ProcessingStatus, ProcessingStatus.Failed),
                    cancellationToken);
        }
        catch (Exception ex)
        {
            logger.Error(ex,
                "Failed to update {EntityType} {EntityId} as failed",
                nameof(Chunk),
                entity.Id);
            throw;
        }
    }
}