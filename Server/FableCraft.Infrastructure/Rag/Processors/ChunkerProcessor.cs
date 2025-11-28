using System.Text.Json;

using FableCraft.Infrastructure.Llm;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities;
using FableCraft.Infrastructure.Rag.Exception;

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

using OpenAI.Chat;

using Polly;
using Polly.Retry;

using Serilog;

using ChatMessageContent = Microsoft.SemanticKernel.ChatMessageContent;
using IKernelBuilder = FableCraft.Infrastructure.Llm.IKernelBuilder;

namespace FableCraft.Infrastructure.Rag.Processors;

internal sealed class ChunkerProcessor : ITextProcessorHandler
{
    private const int BatchSize = 10;
    private readonly ApplicationDbContext _dbContext;
    private readonly IKernelBuilder _kernelBuilder;
    private readonly ILogger _logger;

    public ChunkerProcessor(IKernelBuilder kernelBuilder, ILogger logger, ApplicationDbContext dbContext)
    {
        _kernelBuilder = kernelBuilder;
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task ProcessChunkAsync<TEntity>(Context<TEntity> context, CancellationToken cancellationToken) where TEntity : IKnowledgeGraphEntity
    {
        foreach (var processingContext in context.Chunks.Where(x => x.Chunks.Count == 0).Chunk(BatchSize))
        {
            var chunks = processingContext.Select(arg => ChunkTextAsync(
                    arg.Entity.GetContent().Text,
                    context.ProcessingOptions.MaxChunkSize,
                    cancellationToken).ContinueWith(x =>
                    {
                        var chunks = x.Result.Select((text, idx) => new Chunk
                        {
                            RawChunk = text,
                            EntityId = arg.Entity.Id,
                            ProcessingStatus = ProcessingStatus.Pending,
                            Name = arg.Entity.GetContent()
                                .Description,
                            Order = idx,
                            ContentType = arg.Entity.GetContent()
                                .ContentType,
                            ReferenceTime = default
                        });
                        arg.Chunks.AddRange(chunks);
                    },
                    cancellationToken)
            ).ToArray();

            await Task.WhenAll(chunks);

            var emptyChunk = processingContext.Where(x => x.Chunks.Count == 0).Select(x => x.Entity.Id);
            if (emptyChunk.Any())
            {
                throw new FailedChunkingException(emptyChunk);
            }

            await _dbContext.Chunks.AddRangeAsync(processingContext.SelectMany(x => x.Chunks).ToArray(), cancellationToken);
            await _dbContext.SaveChangesAsync(CancellationToken.None);
        }
    }

    private async Task<List<string>> ChunkTextAsync(string text, int chunkSize, CancellationToken cancellationToken)
    {
        Kernel kernel = _kernelBuilder.WithBase().Build();
        var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
        var chatHistory = new ChatHistory();

        chatHistory.AddUserMessage($"""
                                    You are a text chunking specialist. Your task is to split the provided text into logical, meaningful chunks while preserving context and readability.

                                    # Instructions
                                    1. Analyze the input text structure and identify natural boundaries (paragraphs, sections, topic shifts)
                                    2. Split the text into chunks that:
                                       - Maintain semantic coherence (each chunk covers a complete thought or topic)
                                       - Stay within the specified size limit of up to {chunkSize * 3} characters
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
            MaxTokens = 150000
        };

        ResiliencePipeline pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder()
                    .Handle<JsonException>()
                    .Handle<LlmEmptyResponseException>(),
                MaxRetryAttempts = 5,
                Delay = TimeSpan.FromSeconds(5)
            })
            .Build();

        var chunkedText = await pipeline.ExecuteAsync(async token =>
            {
                ChatMessageContent result = await chatCompletionService.GetChatMessageContentAsync(
                    chatHistory,
                    promptExecutionSettings,
                    kernel,
                    token);

                var replyInnerContent = result.InnerContent as ChatCompletion;
                _logger.Information("ChunkText - Input: {usage}, Output: {output}, Total: {total}",
                    replyInnerContent?.Usage.InputTokenCount,
                    replyInnerContent?.Usage.OutputTokenCount,
                    replyInnerContent?.Usage.TotalTokenCount);

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
}