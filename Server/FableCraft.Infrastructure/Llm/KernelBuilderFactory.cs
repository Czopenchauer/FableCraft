using FableCraft.Infrastructure.Persistence.Entities;

using Microsoft.Extensions.Logging;

namespace FableCraft.Infrastructure.Llm;

public sealed class KernelBuilderFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly Serilog.ILogger _logger;

    public KernelBuilderFactory(ILoggerFactory loggerFactory, Serilog.ILogger logger)
    {
        _loggerFactory = loggerFactory;
        _logger = logger;
    }

    public IKernelBuilder Create(LlmPreset preset)
    {
        var provider = LlmProvider.FromString(preset.Provider);

        return provider switch
        {
            _ when provider == LlmProvider.Gemini => new GeminiKernelBuilder(preset, _loggerFactory, _logger),
            _ when provider == LlmProvider.NanoGpt => new NanoGptKernelBuilder(preset, _loggerFactory, _logger),
            _ when provider == LlmProvider.Ollama => new OllamaKernelBuilder(preset, _loggerFactory, _logger),
            _ => new OpenAiKernelBuilder(preset, _loggerFactory, _logger)
        };
    }
}
