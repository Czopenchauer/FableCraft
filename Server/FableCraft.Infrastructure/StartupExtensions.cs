using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;

using FableCraft.Infrastructure.Clients;
using FableCraft.Infrastructure.Llm;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Queue;
using FableCraft.Infrastructure.Rag;
using FableCraft.ServiceDefaults;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Serilog;

namespace FableCraft.Infrastructure;

public static class StartupExtensions
{
    [Experimental("EXTEXP0001")]
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSerilog(config => config.ReadFrom.Configuration(configuration).Enrich.FromLogContext());
        services.Configure<LlmConfiguration>(configuration.GetSection("FableCraft:Server:LLM"));

        var channel = Channel.CreateBounded<IMessage>(new BoundedChannelOptions(10_000)
        {
            SingleWriter = false,
            SingleReader = false,
            AllowSynchronousContinuations = true,
            FullMode = BoundedChannelFullMode.Wait
        });
        services.AddSingleton(channel);
        services.AddHostedService<InMemoryMessageReader>();
        services.AddSingleton<IMessageDispatcher, InMemoryMessageDispatcher>();

        var connectionString = configuration.GetConnectionString("fablecraftdb");
        ArgumentException.ThrowIfNullOrEmpty(connectionString);
        services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString,
            sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(10, TimeSpan.FromSeconds(10), null);
            }));
        services.AddHostedService<MigratorApplier>();

        var graphApiBaseUrl = configuration.GetConnectionString("graph-rag-api")
                              ?? configuration["services:graph-rag-api:graphRagApi:0"];
        ArgumentException.ThrowIfNullOrEmpty(graphApiBaseUrl);
        services.AddHttpClient<IRagBuilder, RagClient>(client =>
            {
                client.BaseAddress = new Uri(graphApiBaseUrl);

                // LLM calls can take a while
                client.Timeout = TimeSpan.FromMinutes(10);
            })
            .RemoveAllResilienceHandlers()
            .AddDefaultLlmResiliencePolicies();

        services.AddHttpClient<IRagSearch, RagClient>(client =>
            {
                client.BaseAddress = new Uri(graphApiBaseUrl);

                // LLM calls can take a while
                client.Timeout = TimeSpan.FromMinutes(10);
            })
            .RemoveAllResilienceHandlers()
            .AddDefaultLlmResiliencePolicies();

        services.AddTransient<IKernelBuilder, OpenAiKernelBuilder>();
        services.AddTransient<IAgentKernel, AgentKernel>();
        services.AddRag();

        return services;
    }

    public static IServiceCollection AddMessageHandler<TMessage, THandler>(this IServiceCollection services)
        where TMessage : IMessage
        where THandler : class, IMessageHandler<TMessage>
    {
        return services.AddTransient<IMessageHandler<TMessage>, THandler>();
    }
}