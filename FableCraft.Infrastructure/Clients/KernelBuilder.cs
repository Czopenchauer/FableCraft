using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;

using Serilog;

namespace FableCraft.Infrastructure.Clients;

public interface IKernelBuilder
{
    Microsoft.SemanticKernel.IKernelBuilder WithBase(string? model = null);
}

internal class OpenAiKernelBuilder : IKernelBuilder
{
    private readonly LlmConfiguration _configuration;
    private readonly IConfiguration _config;

    public OpenAiKernelBuilder(IOptions<LlmConfiguration> configuration, IConfiguration config)
    {
        _config = config;
        _configuration = configuration.Value;
    }

    public Microsoft.SemanticKernel.IKernelBuilder WithBase(string? model = null)
    {
        var builder = Kernel
            .CreateBuilder()
            .AddOpenAIChatCompletion(model ?? _configuration.Model, new Uri(_configuration.BaseUrl), _configuration.ApiKey);

        builder.Services.AddLogging(c =>
            c.Services.AddSerilog(config =>
            {
                config.ReadFrom.Configuration(_config).Enrich.FromLogContext();
            }));
        return builder;
    }
}