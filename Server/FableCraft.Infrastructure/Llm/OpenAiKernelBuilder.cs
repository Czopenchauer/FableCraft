using System.Diagnostics.CodeAnalysis;

using FableCraft.Infrastructure.Persistence.Entities;
using FableCraft.ServiceDefaults;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace FableCraft.Infrastructure.Llm;

internal class OpenAiKernelBuilder : IKernelBuilder
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly Serilog.ILogger _logger;
    private readonly LlmPreset _preset;

    public OpenAiKernelBuilder(LlmPreset preset, ILoggerFactory loggerFactory, Serilog.ILogger logger)
    {
        _loggerFactory = loggerFactory;
        _logger = logger;
        _preset = preset;
    }

    [Experimental("EXTEXP0001")]
    public Microsoft.SemanticKernel.IKernelBuilder Create()
    {
        var builder = Kernel
            .CreateBuilder()
            .AddOpenAIChatCompletion(_preset.Model, new Uri(_preset.BaseUrl!), _preset.ApiKey);

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
                .RemoveAllResilienceHandlers()
                .AddDefaultLlmResiliencePolicies();
        });

        return builder;
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
