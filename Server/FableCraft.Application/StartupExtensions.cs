#pragma warning disable SKEXP0110 // Experimental Semantic Kernel agents

using FableCraft.Application.AdventureGeneration;
using FableCraft.Application.NarrativeEngine;
using FableCraft.Application.NarrativeEngine.Agents;
using FableCraft.Application.NarrativeEngine.Plugins;
using FableCraft.Application.NarrativeEngine.Plugins.Impl;
using FableCraft.Application.NarrativeEngine.Workflow;
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

        services.AddHostedService<UnlockChunks>();
        services.AddScoped<IAdventureCreationService, AdventureCreationService>();
        services.AddScoped<IGameService, GameService>();
        services
            .AddScoped<SceneGenerationOrchestrator>()
            .AddScoped<IProcessor, WriterAgent>()
            .AddScoped<IProcessor, ContentGenerator>()
            .AddScoped<IProcessor, CharacterTrackersProcessor>()
            .AddScoped<IProcessor, SaveSceneWithoutEnrichment>()
            .AddScoped<IProcessor, SaveSceneEnrichment>()
            .AddScoped<IProcessor, ContextGatherer>()
            .AddScoped<IProcessor, ResolutionAgent>()
            .AddScoped<ContentGenerationService>()
            .AddScoped<MainCharacterTrackerAgent>()
            .AddScoped<InitMainCharacterTrackerAgent>()
            .AddScoped<SceneTrackerAgent>()
            .AddScoped<CharacterCrafter>()
            .AddScoped<PartialProfileCrafter>()
            .AddScoped<LoreCrafter>()
            .AddScoped<ItemCrafter>()
            .AddScoped<CharacterTrackerAgent>()
            .AddScoped<CharacterReflectionAgent>()
            .AddScoped<LocationCrafter>()
            .AddScoped<MainCharacterEmulatorAgent>()
            .AddScoped<ChroniclerAgent>()
            .AddScoped<SimulationPlannerAgent>()
            .AddScoped<StandaloneSimulationAgent>()
            .AddScoped<SimulationModeratorAgent>()
            .AddScoped<CharacterSimulationAgent>()
            .AddScoped<OffscreenInferenceAgent>()
            .AddScoped<IntentCheckAgent>()
            .AddScoped<IProcessor, SceneTrackerProcessor>()
            .AddScoped<IProcessor, SimulationOrchestrator>();

        // Plugin factory and plugins
        services.AddScoped<IPluginFactory, PluginFactory>();
        services.AddTransient<CharacterAgent>();
        services.AddTransient<WorldKnowledgePlugin>();
        services.AddTransient<MainCharacterNarrativePlugin>();
        services.AddTransient<CharacterNarrativePlugin>();
        services.AddTransient<CharacterStatePlugin>();
        services.AddTransient<CharacterRelationshipPlugin>();
        services.AddTransient<CharacterEmulationPlugin>();
        services.AddTransient<IntentCheckPlugin>();
        services.AddTransient<CharacterSimulationToolsPlugin>();

        services.AddMessageHandler<AddAdventureToKnowledgeGraphCommand, AddAdventureToKnowledgeGraphCommandHandler>();
        services.AddMessageHandler<SceneGeneratedEvent, SceneGeneratedEventHandler>();

        return services;
    }
}