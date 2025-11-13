using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;

namespace FableCraft.Infrastructure.Llm;

public interface IKernelBuilder
{
    Microsoft.SemanticKernel.IKernelBuilder WithBase(string? model = null);
}

internal class OpenAiKernelBuilder : IKernelBuilder
{
    private readonly IOptions<LlmConfiguration> _configuration;
    private readonly ILoggerFactory _loggerFactory;

    public OpenAiKernelBuilder(IOptions<LlmConfiguration> configuration, ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
        _configuration = configuration;
    }

    public Microsoft.SemanticKernel.IKernelBuilder WithBase(string? model = null)
    {
        Microsoft.SemanticKernel.IKernelBuilder builder = Kernel
            .CreateBuilder()
            .AddOpenAIChatCompletion(model ?? _configuration.Value.Model, new Uri(_configuration.Value.BaseUrl), _configuration.Value.ApiKey);

        builder.Services.AddSingleton(_loggerFactory);

        builder.Services.ConfigureHttpClientDefaults(hp =>
        {
            hp.ConfigureHttpClient((sp, c) =>
            {
                c.Timeout = TimeSpan.FromMinutes(10);
            });
        });

        return builder;
    }
}