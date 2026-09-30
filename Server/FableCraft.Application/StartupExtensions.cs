#pragma warning disable SKEXP0110 // Experimental Semantic Kernel agents

using FableCraft.Application.AdventureGeneration;
using FableCraft.Application.Chat;
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
        services.AddScoped<ISceneImageService, SceneImageService>();
        services.AddScoped<IChatService, ChatService>();
        services
            .AddScoped<IGenerationContextBuilder, GenerationContextBuilder>()
            .AddScoped<CoLocationMaintenanceService>()
            .AddScoped<SceneGenerationOrchestrator>()
            .AddScoped<IProcessor, ScenePipeline>()
            .AddScoped<IProcessor, WriterAgent>()
            .AddScoped<IProcessor, LoreAgent>()
            .AddScoped<LoreAgent>()
            .AddScoped<IProcessor, SaveSceneWithoutEnrichment>()
            .AddScoped<IProcessor, SaveSceneEnrichment>()
            .AddScoped<IProcessor, ContextGatherer>()
            .AddScoped<IProcessor, CoLocationAgent>()
            .AddScoped<CoLocationAgent>()
            .AddScoped<ContentGenerationService>()
            .AddScoped<ManualContentService>()
            .AddScoped<MainCharacterTrackerAgent>()
            .AddScoped<ProgressionAgent>()
            .AddScoped<InventoryTrackerAgent>()
            .AddScoped<InitMainCharacterTrackerAgent>()
            .AddScoped<SceneTrackerAgent>()
            .AddScoped<PartialProfileCrafter>()
            .AddScoped<LoreCrafter>()
            .AddScoped<ItemCrafter>()
            .AddScoped<LocationCrafter>()
            .AddScoped<MainCharacterEmulatorAgent>()
            .AddScoped<ChroniclerAgent>()
            .AddScoped<NarrativeCatalystAgent>()
            .AddScoped<CharacterContextGatherer>()
            .AddScoped<DispatchService>()
            .AddScoped<StorySummaryAgent>()
            .AddScoped<ImagePromptAgent>()
            .AddScoped<QualityAssuranceAgent>()
        .AddScoped<TrackerDeBloaterAgent>()
            .AddScoped<IProcessor, SceneTrackerProcessor>();

        // Plugin factory and plugins
        services.AddScoped<IPluginFactory, PluginFactory>();
        services.AddTransient<WorldKnowledgePlugin>();
        services.AddTransient<MainCharacterNarrativePlugin>();
        services.AddTransient<CharacterNarrativePlugin>();
        services.AddTransient<CharacterStatePlugin>();
        services.AddTransient<CharacterRelationshipPlugin>();
        services.AddTransient<CharacterDescriptionPlugin>();
        services.AddTransient<CharacterSimulationToolsPlugin>();
        services.AddTransient<ChatCharacterPlugin>();

        services.AddMessageHandler<AddAdventureToKnowledgeGraphCommand, AddAdventureToKnowledgeGraphCommandHandler>();
        services.AddMessageHandler<SceneGeneratedEvent, SceneGeneratedEventHandler>();
        services.AddMessageHandler<IndexWorldbookCommand, IndexWorldbookCommandHandler>();
        services.AddMessageHandler<RecoverGraphRagCommand, RecoverGraphRagCommandHandler>();

        return services;
    }
}