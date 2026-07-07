using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using FableCraft.Infrastructure.Queue;

using Serilog;

namespace FableCraft.Infrastructure.Llm;

internal sealed class HttpLoggingHandler(ILogger logger, IMessageDispatcher messageDispatcher) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var requestId = Guid.NewGuid().ToString("N")[..8];
        var stopwatch = Stopwatch.StartNew();
        var requestBody = await ReadRequestBodyAsync(request);

        if (requestBody is not null)
        {
            logger.Information("[{RequestId}] Request Body: {Body}", requestId, requestBody);
        }
        else
        {
            logger.Information("[{RequestId}] Request: {Method} {Uri} (no body)",
                requestId,
                request.Method,
                request.RequestUri);
        }

        var response = await base.SendAsync(request, cancellationToken);

        if (response.Content is { } content)
        {
            var originalStream = await content.ReadAsStreamAsync(cancellationToken);
            var captureStream = new ResponseCaptureStream(originalStream, requestId, logger, messageDispatcher, stopwatch, requestBody);
            response.Content = new StreamContent(captureStream);

            foreach (var header in content.Headers)
            {
                response.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        return response;
    }

    private static async Task<string?> ReadRequestBodyAsync(HttpRequestMessage request)
    {
        if (request.Content is null)
        {
            return null;
        }

        return await request.Content.ReadAsStringAsync();
    }

    /// <summary>
    /// Wraps the underlying response stream so the raw bytes are captured as Semantic Kernel reads them.
    /// The captured content is logged and published once the stream is fully consumed or disposed.
    /// </summary>
    private sealed class ResponseCaptureStream : Stream
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private readonly Stream _inner;
        private readonly StringBuilder _responseBuilder = new();
        private readonly string _requestId;
        private readonly ILogger _logger;
        private readonly IMessageDispatcher _messageDispatcher;
        private readonly Stopwatch _stopwatch;
        private readonly string? _requestBody;
        private bool _logged;

        public ResponseCaptureStream(
            Stream inner,
            string requestId,
            ILogger logger,
            IMessageDispatcher messageDispatcher,
            Stopwatch stopwatch,
            string? requestBody)
        {
            _inner = inner;
            _requestId = requestId;
            _logger = logger;
            _messageDispatcher = messageDispatcher;
            _stopwatch = stopwatch;
            _requestBody = requestBody;
        }

        public override bool CanRead => _inner.CanRead;
        public override bool CanSeek => _inner.CanSeek;
        public override bool CanWrite => _inner.CanWrite;
        public override long Length => _inner.Length;
        public override long Position
        {
            get => _inner.Position;
            set => _inner.Position = value;
        }

        public override void Flush() => _inner.Flush();
        public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);
        public override void SetLength(long value) => _inner.SetLength(value);
        public override void Write(byte[] buffer, int offset, int count) => _inner.Write(buffer, offset, count);

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                LogResponse();
            }

            _inner.Dispose();
            base.Dispose(disposing);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var read = _inner.Read(buffer, offset, count);
            if (read > 0)
            {
                _responseBuilder.Append(Encoding.UTF8.GetString(buffer, offset, read));
            }

            if (read == 0)
            {
                LogResponse();
            }

            return read;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var read = await _inner.ReadAsync(buffer, offset, count, cancellationToken);
            if (read > 0)
            {
                _responseBuilder.Append(Encoding.UTF8.GetString(buffer, offset, read));
            }

            if (read == 0)
            {
                LogResponse();
            }

            return read;
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            var read = await _inner.ReadAsync(buffer, cancellationToken);
            if (read > 0)
            {
                _responseBuilder.Append(Encoding.UTF8.GetString(buffer.Span.Slice(0, read)));
            }

            if (read == 0)
            {
                LogResponse();
            }

            return read;
        }

        private void LogResponse()
        {
            if (_logged)
            {
                return;
            }

            _logged = true;

            var rawResponse = _responseBuilder.ToString();

            if (TryParseOllamaResponse(rawResponse, out var ollamaThinking, out var ollamaResponse, out var ollamaUsage))
            {
                if (!string.IsNullOrEmpty(ollamaThinking))
                {
                    _logger.Information("[{RequestId}] Generated thinking: {Thinking}", _requestId, ollamaThinking);
                }

                _logger.Information("[{RequestId}] Generated response: {Response}", _requestId, ollamaResponse);
                PublishEvent(ollamaResponse, ollamaThinking, ollamaUsage);
                return;
            }

            if (TryParseOpenAiResponse(rawResponse, out var openAiReasoning, out var openAiResponse, out var openAiUsage))
            {
                if (!string.IsNullOrEmpty(openAiReasoning))
                {
                    _logger.Information("[{RequestId}] Generated reasoning: {Reasoning}", _requestId, openAiReasoning);
                }

                _logger.Information("[{RequestId}] Generated response: {Response}", _requestId, openAiResponse);
                PublishEvent(openAiResponse, openAiReasoning, openAiUsage);
                return;
            }

            _logger.Information("[{RequestId}] Generated response: {Response}", _requestId, rawResponse);
            PublishEvent(rawResponse, reasoning: null, usage: null);
        }

        private void PublishEvent(string response, string? reasoning, TokenUsage? usage)
        {
            var requestContent = _requestBody ?? (ProcessExecutionContext.OperationName.Value is not null
                ? $"Operation: {ProcessExecutionContext.OperationName.Value}"
                : string.Empty);

            _ = Task.Run(async () =>
            {
                try
                {
                    await _messageDispatcher.PublishAsync(new ResponseReceivedEvent
                        {
                            AdventureId = ProcessExecutionContext.AdventureId.Value ?? Guid.Empty,
                            SceneId = ProcessExecutionContext.SceneId.Value,
                            CallerName = ProcessExecutionContext.OperationName.Value ?? "Unknown",
                            RequestContent = requestContent,
                            ResponseContent = response,
                            ReasoningContent = reasoning,
                            InputToken = usage?.InputTokens,
                            OutputToken = usage?.OutputTokens,
                            TotalToken = usage?.TotalTokens,
                            CachedToken = usage?.CachedTokens,
                            Duration = _stopwatch.ElapsedMilliseconds
                        },
                        CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "[{RequestId}] Failed to publish ResponseReceivedEvent", _requestId);
                }
            });
        }

        private static bool TryParseOllamaResponse(string rawResponse, out string thinking, out string response, out TokenUsage? usage)
        {
            thinking = string.Empty;
            response = string.Empty;
            usage = null;

            if (string.IsNullOrWhiteSpace(rawResponse))
            {
                return false;
            }

            var thinkingBuilder = new StringBuilder();
            var responseBuilder = new StringBuilder();
            var isOllama = false;

            foreach (var line in rawResponse.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (!line.StartsWith('{'))
                {
                    continue;
                }

                try
                {
                    var chunk = JsonSerializer.Deserialize<OllamaChatResponseChunk>(line, JsonOptions);
                    if (chunk?.Message is not { } message)
                    {
                        continue;
                    }

                    isOllama = true;
                    thinkingBuilder.Append(message.Thinking);
                    responseBuilder.Append(message.Content);

                    if (chunk.Done && chunk.PromptEvalCount is not null && chunk.EvalCount is not null)
                    {
                        usage = new TokenUsage(chunk.PromptEvalCount, chunk.EvalCount, chunk.PromptEvalCount + chunk.EvalCount, CachedTokens: null);
                    }
                }
                catch (JsonException)
                {
                    // Ignore malformed lines.
                }
            }

            if (!isOllama)
            {
                return false;
            }

            thinking = thinkingBuilder.ToString();
            response = responseBuilder.ToString();
            return true;
        }

        private static bool TryParseOpenAiResponse(string rawResponse, out string reasoning, out string response, out TokenUsage? usage)
        {
            reasoning = string.Empty;
            response = string.Empty;
            usage = null;

            if (string.IsNullOrWhiteSpace(rawResponse))
            {
                return false;
            }

            var reasoningBuilder = new StringBuilder();
            var responseBuilder = new StringBuilder();
            var isOpenAi = false;

            foreach (var rawLine in rawResponse.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var line = rawLine;
                if (line.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                {
                    line = line["data:".Length..].Trim();
                }

                if (line.Equals("[done]", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!line.StartsWith('{'))
                {
                    continue;
                }

                try
                {
                    var chunk = JsonSerializer.Deserialize<OpenAiChatResponseChunk>(line, JsonOptions);
                    if (chunk?.Choices is not { Length: > 0 } choices)
                    {
                        // Could be a usage-only chunk.
                        if (chunk?.Usage is { } usageOnly)
                        {
                            usage = new TokenUsage(usageOnly.PromptTokens, usageOnly.CompletionTokens, usageOnly.TotalTokens, usageOnly.PromptTokensDetails?.CachedTokens);
                        }

                        continue;
                    }

                    isOpenAi = true;
                    var delta = choices[0].Delta;
                    reasoningBuilder.Append(delta?.ReasoningContent);
                    responseBuilder.Append(delta?.Content);

                    if (chunk.Usage is { } chunkUsage)
                    {
                        usage = new TokenUsage(chunkUsage.PromptTokens, chunkUsage.CompletionTokens, chunkUsage.TotalTokens, chunkUsage.PromptTokensDetails?.CachedTokens);
                    }
                }
                catch (JsonException)
                {
                    // Ignore malformed lines.
                }
            }

            if (!isOpenAi)
            {
                return false;
            }

            reasoning = reasoningBuilder.ToString();
            response = responseBuilder.ToString();
            return true;
        }
    }

    private sealed class OllamaChatResponseChunk
    {
        [JsonPropertyName("model")]
        public string? Model { get; init; }

        [JsonPropertyName("message")]
        public OllamaMessage? Message { get; init; }

        [JsonPropertyName("done")]
        public bool Done { get; init; }

        [JsonPropertyName("prompt_eval_count")]
        public int? PromptEvalCount { get; init; }

        [JsonPropertyName("eval_count")]
        public int? EvalCount { get; init; }
    }

    private sealed class OllamaMessage
    {
        [JsonPropertyName("role")]
        public string? Role { get; init; }

        [JsonPropertyName("content")]
        public string? Content { get; init; }

        [JsonPropertyName("thinking")]
        public string? Thinking { get; init; }
    }

    private sealed class OpenAiChatResponseChunk
    {
        [JsonPropertyName("id")]
        public string? Id { get; init; }

        [JsonPropertyName("object")]
        public string? Object { get; init; }

        [JsonPropertyName("choices")]
        public OpenAiChoice[]? Choices { get; init; }

        [JsonPropertyName("usage")]
        public OpenAiUsage? Usage { get; init; }
    }

    private sealed class OpenAiChoice
    {
        [JsonPropertyName("index")]
        public int Index { get; init; }

        [JsonPropertyName("delta")]
        public OpenAiDelta? Delta { get; init; }
    }

    private sealed class OpenAiDelta
    {
        [JsonPropertyName("role")]
        public string? Role { get; init; }

        [JsonPropertyName("content")]
        public string? Content { get; init; }

        [JsonPropertyName("reasoning_content")]
        public string? ReasoningContent { get; init; }
    }

    private sealed class OpenAiUsage
    {
        [JsonPropertyName("prompt_tokens")]
        public int? PromptTokens { get; init; }

        [JsonPropertyName("completion_tokens")]
        public int? CompletionTokens { get; init; }

        [JsonPropertyName("total_tokens")]
        public int? TotalTokens { get; init; }

        [JsonPropertyName("prompt_tokens_details")]
        public OpenAiTokenDetails? PromptTokensDetails { get; init; }
    }

    private sealed class OpenAiTokenDetails
    {
        [JsonPropertyName("cached_tokens")]
        public int? CachedTokens { get; init; }
    }
}

internal sealed record TokenUsage(int? InputTokens, int? OutputTokens, int? TotalTokens, int? CachedTokens);
