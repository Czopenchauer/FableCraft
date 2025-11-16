using System.Net;
using System.Text.Json;
using System.Threading.RateLimiting;

using FableCraft.Infrastructure.Llm;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities;

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

internal sealed class ContextProcessor : ITextProcessorHandler
{
    private const int BatchSize = 10;
    private readonly ApplicationDbContext _dbContext;
    private readonly IKernelBuilder _kernelBuilder;
    private readonly ILogger _logger;
    private readonly ResiliencePipeline _resiliencePipeline;

    public ContextProcessor(IKernelBuilder kernelBuilder, ILogger logger, ApplicationDbContext dbContext)
    {
        _kernelBuilder = kernelBuilder;
        _logger = logger;
        _dbContext = dbContext;
        _resiliencePipeline = new ResiliencePipelineBuilder()
            .AddRateLimiter(new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 5,
                Window = TimeSpan.FromSeconds(1),
                QueueLimit = 1000
            }))
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder()
                    .Handle<InvalidCastException>()
                    .Handle<HttpRequestException>(e => e.StatusCode == HttpStatusCode.TooManyRequests)
                    .Handle<LlmEmptyResponseException>()
                    .Handle<HttpOperationException>(e => e.StatusCode == HttpStatusCode.TooManyRequests),
                MaxRetryAttempts = 20,
                Delay = TimeSpan.FromSeconds(5),
                BackoffType = DelayBackoffType.Constant,
                UseJitter = true
            })
            .Build();
    }

    public async Task ProcessChunkAsync<TEntity>(Context<TEntity> context, CancellationToken cancellationToken) where TEntity : IKnowledgeGraphEntity
    {
        foreach (var processingContext in context.Chunks.Where(x => x.Chunks.Count == 0).Chunk(BatchSize))
        {
            var contextualizedChunkTask = processingContext.SelectMany(x => x.Chunks).Select(arg => ContextualizeChunkAsync(
                    arg.RawChunk,
                    processingContext.Single(x => x.Entity.Id == arg.EntityId).Entity.GetContent().Text,
                    cancellationToken).ContinueWith(x =>
                    {
                        arg.ContextualizedChunk = x.Result;
                    },
                    cancellationToken)
            ).ToList();
            await Task.WhenAll(contextualizedChunkTask);
            _dbContext.Chunks.UpdateRange(processingContext.SelectMany(x => x.Chunks).ToList());
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<string> ContextualizeChunkAsync(string chunk, string fullText, CancellationToken cancellationToken)
    {
        Kernel kernel = _kernelBuilder.WithBase().Build();
        var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
        var chatHistory = new ChatHistory();

        chatHistory.AddUserMessage($"""
                                    <text>
                                    {fullText}
                                    </text>
                                    """);

        chatHistory.AddUserMessage($"""
                                    Here is the chunk we want to situate within
                                    the whole text:
                                    <chunk>
                                    {chunk}
                                    </chunk>
                                    Please give a short succinct context to situate
                                    this chunk within the overall text for the
                                    purposes of improving search retrieval of the
                                    chunk. If the text has a publication date,
                                    please include the date in your context. Answer
                                    only with the succinct context and nothing else.
                                    """);

        var promptExecutionSettings = new OpenAIPromptExecutionSettings
        {
            MaxTokens = 150000
        };

        var contextualizedText = await _resiliencePipeline.ExecuteAsync(async token =>
            {
                ChatMessageContent result = await chatCompletionService.GetChatMessageContentAsync(
                    chatHistory,
                    promptExecutionSettings,
                    kernel,
                    token);

                var replyInnerContent = result.InnerContent as ChatCompletion;
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
}