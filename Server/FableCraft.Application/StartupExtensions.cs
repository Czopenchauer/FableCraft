using FableCraft.Application.AdventureGeneration;
using FableCraft.Application.NarrativeEngine;
using FableCraft.Application.NarrativeEngine.Agents;
using FableCraft.Application.NarrativeEngine.Workflow;
using FableCraft.Infrastructure;

using FluentValidation;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FableCraft.Application;

public static class StartupExtensions
{
    public const string DataDirectory = @"C:\Disc\Dev\_projects\FableCraft\data";

    public static IServiceCollection AddApplicationServices(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddValidatorsFromAssemblyContaining<AdventureCreationService>();

        services.AddHostedService<UnlockChunks>();
        services.AddScoped<IAdventureCreationService, AdventureCreationService>();
        services.AddScoped<IGameService, GameService>();
        services
            .AddScoped<SceneGenerationOrchestrator>()
            .AddScoped<IProcessor, WriterAgent>()
            .AddScoped<IProcessor, ContentGenerator>()
            .AddScoped<IProcessor, TrackerProcessor>()
            .AddScoped<IProcessor, SaveGeneration>()
            .AddScoped<IProcessor, SaveSceneWithoutEnrichment>()
            .AddScoped<IProcessor, ContextGatherer>()
            .AddScoped<IProcessor, NarrativeDirectorAgent>()
            .AddScoped<MainCharacterTrackerAgent>()
            .AddScoped<MainCharacterDevelopmentAgent>()
            .AddScoped<CharacterDevelopmentAgent>()
            .AddScoped<StoryTrackerAgent>()
            .AddScoped<CharacterCrafter>()
            .AddScoped<LoreCrafter>()
            .AddScoped<ItemCrafter>()
            .AddScoped<CharacterStateAgent>()
            .AddScoped<CharacterTrackerAgent>()
            .AddScoped<LocationCrafter>();
        services.AddMessageHandler<AddAdventureToKnowledgeGraphCommand, AddAdventureToKnowledgeGraphCommandHandler>();
        services.AddMessageHandler<SceneGeneratedEvent, SceneGeneratedEventHandler>();

        return services;
    }
}