using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;

using Docker.DotNet;

using FableCraft.Infrastructure.Clients;
using FableCraft.Infrastructure.ComfyUI;
using FableCraft.Infrastructure.Docker;
using FableCraft.Infrastructure.Docker.Configuration;
using FableCraft.Infrastructure.Images;
using FableCraft.Infrastructure.Llm;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Queue;
using FableCraft.Infrastructure.SwarmUI;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;

namespace FableCraft.Infrastructure;

public static class StartupExtensions
{
    extension(IServiceCollection services)
    {
        [Experimental("EXTEXP0001")]
        public IServiceCollection AddInfrastructureServices(IConfiguration configuration)
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
            connectionString += ";Include Error Detail=true";
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

            services.AddHttpClient(RagClientFactory.HttpClientName, client =>
                {
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

            services.AddSingleton<IRagClientFactory, RagClientFactory>();
            services.AddSingleton<GraphContainerRegistry>();
            services.AddSingleton<IContainerMonitor>(sp => sp.GetRequiredService<GraphContainerRegistry>());
            services.AddSingleton<IVisualizationProvider>(sp => sp.GetRequiredService<GraphContainerRegistry>());
            services.AddSingleton<IAdventureRagManager, AdventureRagManager>();
            services.AddSingleton<IWorldbookRagManager, AdventureRagManager>();

            services.AddSingleton<KernelBuilderFactory>();
            services.AddTransient<IAgentKernel, AgentKernel>();
            services.AddMessageHandler<ResponseReceivedEvent, ResponseReceivedEventHandler>();

            services.Configure<DockerSettings>(configuration.GetSection(DockerSettings.SectionName));
            services.Configure<GraphServiceSettings>(configuration.GetSection(GraphServiceSettings.SectionName));
            services.AddHostedService<AdventureLoader>();

            services.AddSingleton<VolumeManager>();
            services.AddHttpClient<ContainerManager>()
                .AddStandardResilienceHandler(options =>
                {
                    options.Retry.MaxRetryAttempts = 20;
                    options.Retry.Delay = TimeSpan.FromMilliseconds(500);
                    options.Retry.BackoffType = Polly.DelayBackoffType.Linear;
                    options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(5);
                    options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(60);
                });

            // Image generation services (provider selected via ImageGeneration:Provider)
            var imageGenSection = configuration.GetSection(ImageGenerationOptions.SectionName);
            services.Configure<ImageGenerationOptions>(imageGenSection);
            services.AddSingleton<SceneImageStorage>();

            var provider = imageGenSection.GetValue<ImageGenerationProvider>("Provider");
            if (provider == ImageGenerationProvider.SwarmUI)
            {
                services.AddOptions<SwarmUISettings>()
                    .Bind(imageGenSection.GetSection("SwarmUI"))
                    .ValidateOnStart();
                services.AddSingleton<IValidateOptions<SwarmUISettings>, SwarmUISettingsValidator>();
                services.AddHttpClient<IImageGenerationClient, SwarmUIClient>()
                    .AddStandardResilienceHandler(ConfigureImageGenerationResilience);
            }
            else
            {
                services.Configure<ComfyUISettings>(imageGenSection.GetSection("ComfyUI"));
                services.AddHttpClient<IImageGenerationClient, ComfyUIClient>()
                    .AddStandardResilienceHandler(ConfigureImageGenerationResilience);
            }

            return services;
        }

        public IServiceCollection AddMessageHandler<TMessage, THandler>()
            where TMessage : IMessage
            where THandler : class, IMessageHandler<TMessage> =>
            services.AddTransient<IMessageHandler<TMessage>, THandler>();
    }

    private static void ConfigureImageGenerationResilience(HttpStandardResilienceOptions options)
    {
        options.AttemptTimeout.Timeout = TimeSpan.FromMinutes(5);
        options.TotalRequestTimeout.Timeout = TimeSpan.FromMinutes(10);
        options.Retry.MaxRetryAttempts = 3;
        options.Retry.Delay = TimeSpan.FromSeconds(2);
        // SamplingDuration must be at least double the AttemptTimeout
        options.CircuitBreaker.SamplingDuration = TimeSpan.FromMinutes(11);
    }
}