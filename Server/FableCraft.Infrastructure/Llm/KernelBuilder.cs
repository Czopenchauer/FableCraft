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

internal static class LlmProviders
{
    public const string OpenAi = "openai";
    public const string Gemini = "gemini";
}

public sealed class KernelBuilderFactory
{
    private readonly ILoggerFactory _loggerFactory;

    public KernelBuilderFactory(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
    }

    public IKernelBuilder Create(LlmPreset preset)
    {
        return preset.Provider.ToLowerInvariant() switch
               {
                   LlmProviders.Gemini => new GeminiKernelBuilder(preset, _loggerFactory),
                   // ReSharper disable once RedundantSwitchExpressionArms
                   LlmProviders.OpenAi => new OpenAiKernelBuilder(preset, _loggerFactory),
                   _ => new OpenAiKernelBuilder(preset, _loggerFactory)
               };
    }
}

internal class OpenAiKernelBuilder : IKernelBuilder
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly LlmPreset _preset;

    public OpenAiKernelBuilder(LlmPreset preset, ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
        _preset = preset;
    }

    public Microsoft.SemanticKernel.IKernelBuilder Create()
    {
        Microsoft.SemanticKernel.IKernelBuilder builder = Kernel
            .CreateBuilder()
            .AddOpenAIChatCompletion(_preset.Model, new Uri(_preset.BaseUrl!), _preset.ApiKey);

        builder.Services.AddSingleton(_loggerFactory);

        builder.Services.ConfigureHttpClientDefaults(hp =>
        {
            hp.ConfigureHttpClient((_, c) =>
            {
                c.Timeout = TimeSpan.FromMinutes(10);
            });
            hp.AddDefaultLlmResiliencePolicies();
        });

        return builder;
    }

    public PromptExecutionSettings GetDefaultPromptExecutionSettings()
    {
        return new OpenAIPromptExecutionSettings
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
    }

    public PromptExecutionSettings GetDefaultFunctionPromptExecutionSettings()
    {
        return new OpenAIPromptExecutionSettings
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
}

internal class GeminiKernelBuilder : IKernelBuilder
{
    private readonly static GeminiSafetySetting[] DefaultSafetySettings =
    [
        new(GeminiSafetyCategory.Harassment, new GeminiSafetyThreshold("OFF")),
        new(GeminiSafetyCategory.SexuallyExplicit, new GeminiSafetyThreshold("OFF")),
        new(GeminiSafetyCategory.DangerousContent, new GeminiSafetyThreshold("OFF")),
        new(new GeminiSafetyCategory("HARM_CATEGORY_CIVIC_INTEGRITY"), new GeminiSafetyThreshold("OFF"))
    ];

    private readonly ILoggerFactory _loggerFactory;
    private readonly LlmPreset _preset;

    public GeminiKernelBuilder(LlmPreset preset, ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
        _preset = preset;
    }

    public Microsoft.SemanticKernel.IKernelBuilder Create()
    {
        Microsoft.SemanticKernel.IKernelBuilder builder = Kernel
            .CreateBuilder()
            .AddGoogleAIGeminiChatCompletion(_preset.Model, _preset.ApiKey);

        builder.Services.AddSingleton(_loggerFactory);

        builder.Services.ConfigureHttpClientDefaults(hp =>
        {
            hp.ConfigureHttpClient((_, c) =>
            {
                c.Timeout = TimeSpan.FromMinutes(10);
            });
            hp.AddDefaultLlmResiliencePolicies();
        });

        return builder;
    }

    public PromptExecutionSettings GetDefaultPromptExecutionSettings()
    {
        return new GeminiPromptExecutionSettings
        {
            MaxTokens = _preset.MaxTokens,
            Temperature = _preset.Temperature,
            TopP = _preset.TopP,
            TopK = _preset.TopK,
            SafetySettings = DefaultSafetySettings
        };
    }

    public PromptExecutionSettings GetDefaultFunctionPromptExecutionSettings()
    {
        return new GeminiPromptExecutionSettings
        {
            MaxTokens = _preset.MaxTokens,
            Temperature = _preset.Temperature,
            TopP = _preset.TopP,
            TopK = _preset.TopK,
            ToolCallBehavior = GeminiToolCallBehavior.AutoInvokeKernelFunctions,
            SafetySettings = DefaultSafetySettings
        };
    }
}