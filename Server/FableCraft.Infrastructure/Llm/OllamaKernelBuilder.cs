using System.Diagnostics.CodeAnalysis;

using FableCraft.Infrastructure.Persistence.Entities;
using FableCraft.ServiceDefaults;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Ollama;

namespace FableCraft.Infrastructure.Llm;

internal sealed class OllamaKernelBuilder : IKernelBuilder
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly Serilog.ILogger _logger;
    private readonly LlmPreset _preset;

    public OllamaKernelBuilder(LlmPreset preset, ILoggerFactory loggerFactory, Serilog.ILogger logger)
    {
        _loggerFactory = loggerFactory;
        _logger = logger;
        _preset = preset;
    }

    [Experimental("SKEXP0070")]
    public Microsoft.SemanticKernel.IKernelBuilder Create()
    {
        var builder = Kernel
            .CreateBuilder()
            .AddOllamaChatCompletion(_preset.Model, new Uri(_preset.BaseUrl ?? "http://host.docker.internal:11434"));

        builder.Services.AddSingleton(_loggerFactory);
        builder.Services.AddSingleton(_logger);
        builder.Services.AddTransient<HttpLoggingHandler>();

        builder.Services.ConfigureHttpClientDefaults(hp =>
        {
            hp.ConfigureHttpClient((_, c) =>
                {
                    c.Timeout = TimeSpan.FromMinutes(10);
                })
                .AddHttpMessageHandler<HttpLoggingHandler>()
                .AddHttpMessageHandler<NanoGptRequestTransformer>()
                .RemoveAllResilienceHandlers()
                .AddDefaultLlmResiliencePolicies();
        });

        return builder;
    }

    public PromptExecutionSettings GetDefaultPromptExecutionSettings() =>
        new OllamaPromptExecutionSettings
        {
            NumPredict = _preset.MaxTokens,
            Temperature = (float?)_preset.Temperature,
            TopP = (float?)_preset.TopP,
            TopK = _preset.TopK,
            FunctionChoiceBehavior = FunctionChoiceBehavior.None()
        };

    public PromptExecutionSettings GetDefaultFunctionPromptExecutionSettings() =>
        new OllamaPromptExecutionSettings
        {
            NumPredict = _preset.MaxTokens,
            Temperature = (float?)_preset.Temperature,
            TopP = (float?)_preset.TopP,
            TopK = _preset.TopK,
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(options: new FunctionChoiceBehaviorOptions
            {
                AllowConcurrentInvocation = true,
                AllowParallelCalls = true
            })
        };
}