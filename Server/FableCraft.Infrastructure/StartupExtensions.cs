using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;

using Docker.DotNet;

using FableCraft.Infrastructure.Clients;
using FableCraft.Infrastructure.Docker;
using FableCraft.Infrastructure.Docker.Configuration;
using FableCraft.Infrastructure.Llm;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Queue;
using FableCraft.ServiceDefaults;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;

namespace FableCraft.Infrastructure;

public static class StartupExtensions
{
    [Experimental("EXTEXP0001")]
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var channel = Channel.CreateBounded<MessageWithContext>(new BoundedChannelOptions(10_000)
        {
            SingleWriter = false,
            SingleReader = false,
            AllowSynchronousContinuations = true,
            FullMode = BoundedChannelFullMode.Wait
        });
        services.AddSingleton(channel);
        services.AddScoped<IRagChunkService, RagChunkService>();
        services.AddHostedService<InMemoryMessageReader>();
        services.AddSingleton<IMessageDispatcher, InMemoryMessageDispatcher>();

        var connectionString = configuration.GetConnectionString("fablecraftdb");
        ArgumentException.ThrowIfNullOrEmpty(connectionString);
        services.AddDbContextPool<ApplicationDbContext>(options => options.UseNpgsql(connectionString,
                sqlOptions =>
                {
                    sqlOptions.EnableRetryOnFailure(10, TimeSpan.FromSeconds(10), null);
                }))
            .AddPooledDbContextFactory<ApplicationDbContext>(options => options.UseNpgsql(connectionString,
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
                client.Timeout = TimeSpan.FromMinutes(120);
            })
            .RemoveAllResilienceHandlers()
            .AddStandardResilienceHandler(options =>
            {
                options.AttemptTimeout = new HttpTimeoutStrategyOptions
                {
                    Timeout = TimeSpan.FromMinutes(120)
                };

                options.TotalRequestTimeout = new HttpTimeoutStrategyOptions
                {
                    Timeout = TimeSpan.FromMinutes(240)
                };

                options.Retry.MaxRetryAttempts = 5;
                options.Retry.Delay = TimeSpan.FromSeconds(5);
                options.CircuitBreaker.SamplingDuration = TimeSpan.FromMinutes(240);
            });

        services.AddHttpClient<IRagSearch, RagClient>(client =>
            {
                client.BaseAddress = new Uri(graphApiBaseUrl);

                client.Timeout = TimeSpan.FromMinutes(10);
            })
            .RemoveAllResilienceHandlers()
            .AddDefaultLlmResiliencePolicies();

        services.AddSingleton<KernelBuilderFactory>();
        services.AddTransient<IAgentKernel, AgentKernel>();
        services.AddMessageHandler<ResponseReceivedEvent, ResponseReceivedEventHandler>();

        // Docker services for volume-based knowledge graph isolation
        services.Configure<DockerSettings>(configuration.GetSection(DockerSettings.SectionName));
        services.Configure<GraphServiceSettings>(configuration.GetSection(GraphServiceSettings.SectionName));

        services.AddSingleton<DockerClient>(sp =>
        {
            var settings = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<DockerSettings>>().Value;
            return new DockerClientConfiguration(new Uri(settings.SocketPath), defaultTimeout: TimeSpan.FromSeconds(120)).CreateClient();
        });

        services.AddSingleton<IVolumeManager, VolumeManager>();
        services.AddSingleton<IContainerManager, ContainerManager>();

        // HTTP client for Docker health checks (internal container health checks)
        services.AddHttpClient("DockerHealthCheck", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(5);
        });

        return services;
    }

    public static IServiceCollection AddMessageHandler<TMessage, THandler>(this IServiceCollection services)
        where TMessage : IMessage
        where THandler : class, IMessageHandler<TMessage> =>
        services.AddTransient<IMessageHandler<TMessage>, THandler>();
}