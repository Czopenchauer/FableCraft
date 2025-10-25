using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FableCraft.Application;

public static class StartupExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        var graphApiBaseUrl = configuration.GetConnectionString("graph-rag-api")
                              ?? configuration["services:graph-rag-api:graphRagApi:0"];
        ArgumentException.ThrowIfNullOrEmpty(graphApiBaseUrl);
        services.AddHttpClient<Clients.RagClient>(client =>
        {
            client.BaseAddress = new Uri(graphApiBaseUrl);
            client.Timeout = TimeSpan.FromMinutes(10);
        }).AddServiceDiscovery();
        return services;
    }
}