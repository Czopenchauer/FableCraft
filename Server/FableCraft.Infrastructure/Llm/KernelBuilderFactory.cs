using FableCraft.Infrastructure.Persistence.Entities;
using FableCraft.Infrastructure.Queue;

using Microsoft.Extensions.Logging;

namespace FableCraft.Infrastructure.Llm;

public sealed class KernelBuilderFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly Serilog.ILogger _logger;
    private readonly IMessageDispatcher _messageDispatcher;

    public KernelBuilderFactory(ILoggerFactory loggerFactory, Serilog.ILogger logger, IMessageDispatcher messageDispatcher)
    {
        _loggerFactory = loggerFactory;
        _logger = logger;
        _messageDispatcher = messageDispatcher;
    }

    public IKernelBuilder Create(LlmPreset preset)
    {
        var provider = LlmProvider.FromString(preset.Provider);

        return provider switch
        {
            _ when provider == LlmProvider.Gemini => new GeminiKernelBuilder(preset, _loggerFactory, _logger, _messageDispatcher),
            _ when provider == LlmProvider.NanoGpt => new NanoGptKernelBuilder(preset, _loggerFactory, _logger, _messageDispatcher),
            _ when provider == LlmProvider.Ollama => new OllamaKernelBuilder(preset, _loggerFactory, _logger, _messageDispatcher),
            _ => new OpenAiKernelBuilder(preset, _loggerFactory, _logger, _messageDispatcher)
        };
    }
}
