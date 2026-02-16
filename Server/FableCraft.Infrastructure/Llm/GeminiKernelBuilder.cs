using System.Diagnostics.CodeAnalysis;

using FableCraft.Infrastructure.Persistence.Entities;
using FableCraft.ServiceDefaults;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Google;

namespace FableCraft.Infrastructure.Llm;

internal class GeminiKernelBuilder : IKernelBuilder
{
    private static readonly GeminiSafetySetting[] DefaultSafetySettings =
    [
        new(GeminiSafetyCategory.Harassment, new GeminiSafetyThreshold("OFF")),
        new(GeminiSafetyCategory.SexuallyExplicit, new GeminiSafetyThreshold("OFF")),
        new(GeminiSafetyCategory.DangerousContent, new GeminiSafetyThreshold("OFF")),
        new(new GeminiSafetyCategory("HARM_CATEGORY_CIVIC_INTEGRITY"), new GeminiSafetyThreshold("OFF"))
    ];

    private readonly ILoggerFactory _loggerFactory;
    private readonly Serilog.ILogger _logger;
    private readonly LlmPreset _preset;

    public GeminiKernelBuilder(LlmPreset preset, ILoggerFactory loggerFactory, Serilog.ILogger logger)
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
            .AddGoogleAIGeminiChatCompletion(_preset.Model, _preset.ApiKey);

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
        new GeminiPromptExecutionSettings
        {
            MaxTokens = _preset.MaxTokens,
            Temperature = _preset.Temperature,
            TopP = _preset.TopP,
            TopK = _preset.TopK,
            SafetySettings = DefaultSafetySettings,
            ThinkingConfig = new GeminiThinkingConfig()
            {
                IncludeThoughts = true,
                ThinkingBudget = 24576
            }
        };

    public PromptExecutionSettings GetDefaultFunctionPromptExecutionSettings() =>
        new GeminiPromptExecutionSettings
        {
            MaxTokens = _preset.MaxTokens,
            Temperature = _preset.Temperature,
            TopP = _preset.TopP,
            TopK = _preset.TopK,
            ToolCallBehavior = GeminiToolCallBehavior.AutoInvokeKernelFunctions,
            SafetySettings = DefaultSafetySettings,
            ThinkingConfig = new GeminiThinkingConfig()
            {
                IncludeThoughts = true,
                ThinkingBudget = 24576
            }
        };
}
