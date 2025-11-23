using FableCraft.Application.AdventureGeneration;
using FableCraft.Application.NarrativeEngine;
using FableCraft.Application.NarrativeEngine.Agents;
using FableCraft.Application.NarrativeEngine.Orchestration;
using FableCraft.Application.NarrativeEngine.WelcomeScene;
using FableCraft.Infrastructure;

using FluentValidation;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FableCraft.Application;

public static class StartupExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddValidatorsFromAssemblyContaining<AdventureCreationService>();

        services.Configure<AdventureCreationConfig>(
            configuration.GetSection("FableCraft:AdventureCreationConfig"));

        services.AddScoped<IAdventureCreationService, AdventureCreationService>();
        services.AddScoped<IGameService, GameService>();
        services
            .AddScoped<SceneGenerationOrchestrator>()
            .AddScoped<WriterAgent>()
            .AddScoped<TrackerAgent>()
            .AddScoped<CharacterCrafter>()
            .AddScoped<LoreCrafter>()
            .AddScoped<CharacterStateTracker>()
            .AddScoped<ContextGatherer>()
            .AddScoped<NarrativeDirectorAgent>();
        services.AddMessageHandler<AddAdventureToKnowledgeGraphCommand, AddAdventureToKnowledgeGraphCommandHandler>();
        services.AddMessageHandler<AdventureCreatedEvent, AdventureCreatedEventHandler>();

        return services;
    }
}