using System.Threading.Channels;

using FableCraft.Infrastructure.Clients;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Queue;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Serilog;

namespace FableCraft.Infrastructure;

public static class StartupExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSerilog(config => config.ReadFrom.Configuration(configuration).Enrich.FromLogContext());

        Channel<IMessage> channel = Channel.CreateUnbounded<IMessage>(new UnboundedChannelOptions
        {
            SingleWriter = false,
            SingleReader = false,
            AllowSynchronousContinuations = true
        });
        services.AddSingleton(channel);
        services.AddHostedService<InMemoryMessageReader>();
        services.AddSingleton<IMessageDispatcher, InMemoryMessageDispatcher>();

        string? connectionString = configuration.GetConnectionString("fablecraftdb");
        ArgumentException.ThrowIfNullOrEmpty(connectionString);
        services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString, sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(10, TimeSpan.FromSeconds(10), null);
        }));
        services.AddHostedService<MigratorApplier>();

        string? graphApiBaseUrl = configuration.GetConnectionString("graph-rag-api")
                                  ?? configuration["services:graph-rag-api:graphRagApi:0"];
        ArgumentException.ThrowIfNullOrEmpty(graphApiBaseUrl);
        services.AddHttpClient<IRagBuilder, RagClient>(client =>
        {
            client.BaseAddress = new Uri(graphApiBaseUrl);

            // LLM calls can take a while
            client.Timeout = TimeSpan.FromMinutes(10);
        }).AddServiceDiscovery();

        services.AddHttpClient<IRagSearch, RagClient>(client =>
        {
            client.BaseAddress = new Uri(graphApiBaseUrl);

            // LLM calls can take a while
            client.Timeout = TimeSpan.FromMinutes(10);
        }).AddServiceDiscovery();

        return services;
    }

    public static IServiceCollection AddMessageHandler<TMessage>(this IServiceCollection services)
        where TMessage : IMessage
    {
        return services.AddTransient<IMessageHandler<TMessage>>();
    }
}