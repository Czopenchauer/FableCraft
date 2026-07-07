using System.Diagnostics.CodeAnalysis;

using FableCraft.Infrastructure.Persistence.Entities;
using FableCraft.Infrastructure.Queue;
using FableCraft.ServiceDefaults;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace FableCraft.Infrastructure.Llm;

internal class NanoGptKernelBuilder : IKernelBuilder
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly Serilog.ILogger _logger;
    private readonly IMessageDispatcher _messageDispatcher;
    private readonly LlmPreset _preset;

    public NanoGptKernelBuilder(LlmPreset preset, ILoggerFactory loggerFactory, Serilog.ILogger logger, IMessageDispatcher messageDispatcher)
    {
        _loggerFactory = loggerFactory;
        _logger = logger;
        _messageDispatcher = messageDispatcher;
        _preset = preset;
    }

    [Experimental("EXTEXP0001")]
    public Microsoft.SemanticKernel.IKernelBuilder Create()
    {
        var httpClient = CreateHttpClient();

        var builder = Kernel
            .CreateBuilder()
            .AddOpenAIChatCompletion(_preset.Model, new Uri(_preset.BaseUrl!), _preset.ApiKey, httpClient: httpClient);

        builder.Services.AddSingleton(_loggerFactory);
        builder.Services.AddSingleton(_logger);

        return builder;
    }

    private HttpClient CreateHttpClient()
    {
        var innerHandler = new HttpClientHandler();
        var nanoGptHandler = new NanoGptRequestTransformer(_logger) { InnerHandler = innerHandler };
        var loggingHandler = new HttpLoggingHandler(_logger, _messageDispatcher) { InnerHandler = nanoGptHandler };

        return new HttpClient(loggingHandler)
        {
            BaseAddress = new Uri(_preset.BaseUrl!),
            Timeout = TimeSpan.FromMinutes(5)
        };
    }

    public PromptExecutionSettings GetDefaultPromptExecutionSettings() =>
        new OpenAIPromptExecutionSettings
        {
            MaxTokens = _preset.MaxTokens,
            Temperature = _preset.Temperature,
            TopP = _preset.TopP,
            FrequencyPenalty = _preset.FrequencyPenalty,
            PresencePenalty = _preset.PresencePenalty,
            FunctionChoiceBehavior = FunctionChoiceBehavior.None()
        };

    public PromptExecutionSettings GetDefaultFunctionPromptExecutionSettings() =>
        new OpenAIPromptExecutionSettings
        {
            MaxTokens = _preset.MaxTokens,
            Temperature = _preset.Temperature,
            TopP = _preset.TopP,
            FrequencyPenalty = _preset.FrequencyPenalty,
            PresencePenalty = _preset.PresencePenalty,
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(options: new FunctionChoiceBehaviorOptions
            {
                AllowConcurrentInvocation = true,
                AllowParallelCalls = true
            })
        };
}
