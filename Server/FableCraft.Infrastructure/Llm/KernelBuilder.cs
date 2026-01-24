using System.Diagnostics.CodeAnalysis;

using FableCraft.Infrastructure.Persistence.Entities;
using FableCraft.ServiceDefaults;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Google;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace FableCraft.Infrastructure.Llm;

public interface IKernelBuilder
{
    Microsoft.SemanticKernel.IKernelBuilder Create();

    PromptExecutionSettings GetDefaultPromptExecutionSettings();

    PromptExecutionSettings GetDefaultFunctionPromptExecutionSettings();
}

public readonly struct LlmProvider : IEquatable<LlmProvider>
{
    public readonly static LlmProvider OpenAi = new("openai");
    public static readonly LlmProvider Gemini = new("gemini");
    public readonly static LlmProvider Anthropic = new("anthropic");

    public string Value { get; }

    private LlmProvider(string value)
    {
        Value = value;
    }

    public static LlmProvider FromString(string value)
    {
        return value.ToLowerInvariant() switch
               {
                   "openai" => OpenAi,
                   "gemini" => Gemini,
                   "anthropic" => Anthropic,
                   _ => OpenAi
               };
    }

    public override string ToString() => Value;

    public override int GetHashCode() => Value.GetHashCode();

    public override bool Equals(object? obj) => obj is LlmProvider other && Equals(other);

    public bool Equals(LlmProvider other) => Value == other.Value;

    public static bool operator ==(LlmProvider left, LlmProvider right) => left.Equals(right);

    public static bool operator !=(LlmProvider left, LlmProvider right) => !left.Equals(right);

    public static implicit operator string(LlmProvider provider) => provider.Value;
}

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
                   _ => new OpenAiKernelBuilder(preset, _loggerFactory, _logger)
               };
    }
}

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
            FunctionChoiceBehavior = FunctionChoiceBehavior.None(),
            ExtensionData = new Dictionary<string, object>
            {
                {
                    "thinking", new
                    {
                        type = "enabled"
                    }
                }
            }
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
            }),
            ExtensionData = new Dictionary<string, object>
            {
                {
                    "thinking", new
                    {
                        type = "enabled"
                    }
                }
            }
        };
}

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
            SafetySettings = DefaultSafetySettings
        };

    public PromptExecutionSettings GetDefaultFunctionPromptExecutionSettings() =>
        new GeminiPromptExecutionSettings
        {
            MaxTokens = _preset.MaxTokens,
            Temperature = _preset.Temperature,
            TopP = _preset.TopP,
            TopK = _preset.TopK,
            ToolCallBehavior = GeminiToolCallBehavior.AutoInvokeKernelFunctions,
            SafetySettings = DefaultSafetySettings
        };
}