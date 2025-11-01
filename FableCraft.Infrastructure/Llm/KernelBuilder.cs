using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;

using Serilog;

namespace FableCraft.Infrastructure.Llm;

public interface IKernelBuilder
{
    Microsoft.SemanticKernel.IKernelBuilder WithBase(string? model = null);
}

internal class OpenAiKernelBuilder : IKernelBuilder
{
    private readonly IOptions<LlmConfiguration> _configuration;
    private readonly IConfiguration _config;

    public OpenAiKernelBuilder(IOptions<LlmConfiguration> configuration, IConfiguration config)
    {
        _config = config;
        _configuration = configuration;
    }

    public Microsoft.SemanticKernel.IKernelBuilder WithBase(string? model = null)
    {
        var builder = Kernel
            .CreateBuilder()
            .AddOpenAIChatCompletion(model ?? _configuration.Value.Model, new Uri(_configuration.Value.BaseUrl), _configuration.Value.ApiKey);

        builder.Services.AddLogging(c =>
            c.Services.AddSerilog(config =>
            {
                config.ReadFrom.Configuration(_config);
            }));
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