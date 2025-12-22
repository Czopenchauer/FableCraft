using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;

using Npgsql;

using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

using Serilog;

namespace FableCraft.ServiceDefaults;

// Adds common .NET Aspire services: service discovery, resilience, health checks, and OpenTelemetry.
// This project should be referenced by each service project in your solution.
// To learn more about using this project, see https://aka.ms/dotnet/aspire/service-defaults
public static class Extensions
{
    private const string HealthEndpointPath = "/health";
    private const string AlivenessEndpointPath = "/alive";

    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.ConfigureOpenTelemetry();

        builder.AddDefaultHealthChecks();

        builder.Services.AddServiceDiscovery();

        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            // Turn on resilience by default
            http.AddStandardResilienceHandler(options =>
            {
                options.AttemptTimeout = new HttpTimeoutStrategyOptions
                {
                    Timeout = TimeSpan.FromSeconds(20)
                };

                options.TotalRequestTimeout = new HttpTimeoutStrategyOptions
                {
                    Timeout = TimeSpan.FromMinutes(5)
                };

                options.Retry.MaxRetryAttempts = 3;
                options.Retry.Delay = TimeSpan.FromSeconds(2);
                options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(60);
            });

            // Turn on service discovery by default
            http.AddServiceDiscovery();
        });
        
        var seqLogUrl = builder.Configuration["FABLECRAFT_EXPORTER_SEQ_LOG_ENDPOINT"] 
                        ?? throw new InvalidOperationException("FABLECRAFT_EXPORTER_SEQ_LOG_ENDPOINT is not set");
        builder.Services.AddSerilog(config => config
            .MinimumLevel.Information()
            .Enrich.FromLogContext()
            .Enrich.WithProperty("ApplicationName", "FableCraft.Server")
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3} {CorrelationId}] {Message:lj}{NewLine}{Exception}")
            // Aspire dashboard
            .WriteTo.OpenTelemetry()
            //.WriteTo.Seq(seqLogUrl)
            .WriteTo.File(
                path: Environment.GetEnvironmentVariable("FABLECRAFT_LOG_PATH") ?? "./logs/log-.txt",
                rollingInterval: RollingInterval.Hour));

        // Uncomment the following to restrict the allowed schemes for service discovery.
        // builder.Services.Configure<ServiceDiscoveryOptions>(options =>
        // {
        //     options.AllowedSchemes = ["https"];
        // });

        return builder;
    }

    public static IHttpStandardResiliencePipelineBuilder AddDefaultLlmResiliencePolicies(this IHttpClientBuilder builder)
    {
        // Configure default resilience policies for LLM HTTP calls
        return builder.AddStandardResilienceHandler(options =>
        {
            options.AttemptTimeout = new HttpTimeoutStrategyOptions
            {
                Timeout = TimeSpan.FromMinutes(10)
            };

            options.TotalRequestTimeout = new HttpTimeoutStrategyOptions
            {
                Timeout = TimeSpan.FromMinutes(20)
            };

            options.Retry.MaxRetryAttempts = 5;
            options.Retry.Delay = TimeSpan.FromSeconds(5);
            options.CircuitBreaker.SamplingDuration = TimeSpan.FromMinutes(20);
        });
    }

    public static TBuilder ConfigureOpenTelemetry<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        // builder.Logging.AddOpenTelemetry(logging =>
        // {
        //     logging.IncludeFormattedMessage = true;
        //     logging.IncludeScopes = true;
        // });

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation()
                    .AddNpgsqlInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddMeter("Microsoft.SemanticKernel*")
                    // .AddOtlpExporter(opt =>
                    // {
                    //     opt.Endpoint = new Uri(builder.Configuration["FABLECRAFT_EXPORTER_SEQ_TRACE_ENDPOINT"]!);
                    //     opt.Protocol = OtlpExportProtocol.HttpProtobuf;
                    // })
                    // Aspire dashboard exporter
                    .AddOtlpExporter(opt =>
                    {
                        opt.Endpoint = new Uri(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]!);
                        opt.Protocol = OtlpExportProtocol.Grpc;
                    })
                    .AddRuntimeInstrumentation();
            })
            .WithTracing(tracing =>
            {
                tracing.AddSource(builder.Environment.ApplicationName)
                    .AddAspNetCoreInstrumentation(t =>
                        // Exclude health check requests from tracing
                        t.Filter = context =>
                            !context.Request.Path.StartsWithSegments(HealthEndpointPath)
                            && !context.Request.Path.StartsWithSegments(AlivenessEndpointPath)
                    )
                    // Uncomment the following line to enable gRPC instrumentation (requires the OpenTelemetry.Instrumentation.GrpcNetClient package)
                    //.AddGrpcClientInstrumentation()
                    .AddSource("Microsoft.SemanticKernel*")
                    .AddSource("LlmCall")
                    .AddNpgsql()
                    // SEQ trace exporter
                    // .AddOtlpExporter(opt =>
                    // {
                    //     opt.Endpoint = new Uri(builder.Configuration["FABLECRAFT_EXPORTER_SEQ_TRACE_ENDPOINT"]!);
                    //     opt.Protocol = OtlpExportProtocol.HttpProtobuf;
                    // })
                    // Aspire dashboard exporter
                    .AddOtlpExporter(opt =>
                    {
                        opt.Endpoint = new Uri(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]!);
                        opt.Protocol = OtlpExportProtocol.Grpc;
                    })
                    .AddHttpClientInstrumentation();
            });

        builder.AddOpenTelemetryExporters();

        return builder;
    }

    private static TBuilder AddOpenTelemetryExporters<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        // Signal-specific AddOtlpExporter methods and the cross-cutting UseOtlpExporter method being invoked on the same IServiceCollection is not supported.
        // var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);
        //
        // if (useOtlpExporter)
        // {
        //     builder.Services.AddOpenTelemetry().UseOtlpExporter();
        // }
        
        // var seqExporter = builder.Configuration["FABLECRAFT_EXPORTER_SEQ_OTLP_ENDPOINT"];
        // if (!string.IsNullOrEmpty(seqExporter))
        // {
        //     builder.Services.AddLogging(logging => logging.AddOpenTelemetry(openTelemetryLoggerOptions =>  
        //     {  
        //         openTelemetryLoggerOptions.IncludeScopes = true;
        //         openTelemetryLoggerOptions.IncludeFormattedMessage = true;
        //
        //         openTelemetryLoggerOptions.AddOtlpExporter(exporter =>
        //         {
        //             exporter.Endpoint = new Uri(seqExporter);
        //             exporter.Protocol = OtlpExportProtocol.HttpProtobuf;
        //         });
        //     }));
        // }

        // Uncomment the following lines to enable the Azure Monitor exporter (requires the Azure.Monitor.OpenTelemetry.AspNetCore package)
        //if (!string.IsNullOrEmpty(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
        //{
        //    builder.Services.AddOpenTelemetry()
        //       .UseAzureMonitor();
        //}

        return builder;
    }

    public static TBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddHealthChecks()
            // Add a default liveness check to ensure app is responsive
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        return builder;
    }

    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        // Adding health checks endpoints to applications in non-development environments has security implications.
        // See https://aka.ms/dotnet/aspire/healthchecks for details before enabling these endpoints in non-development environments.
        if (app.Environment.IsDevelopment())
        {
            // All health checks must pass for app to be considered ready to accept traffic after starting
            app.MapHealthChecks(HealthEndpointPath);

            // Only health checks tagged with the "live" tag must pass for app to be considered alive
            app.MapHealthChecks(AlivenessEndpointPath,
                new HealthCheckOptions { Predicate = r => r.Tags.Contains("live") });
        }

        return app;
    }
}