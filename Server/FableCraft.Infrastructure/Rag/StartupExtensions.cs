using FableCraft.Infrastructure.Rag.Processors;

using Microsoft.Extensions.DependencyInjection;

namespace FableCraft.Infrastructure.Rag;

internal static class StartupExtensions
{
    public static IServiceCollection AddRag(this IServiceCollection services)
    {
        services
            .AddScoped<IRagProcessor, RagProcessor>()
            .AddScoped<ITextProcessorHandler, ChunkerProcessor>()
            .AddScoped<ITextProcessorHandler, ContextProcessor>()
            .AddScoped<ITextProcessorHandler, KnowledgeGraphProcessor>();

        return services;
    }
}