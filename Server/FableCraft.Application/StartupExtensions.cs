using FableCraft.Application.AdventureGeneration;
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
        services.AddHostedService<ValidateLorebookPrompt>();

        services.Configure<AdventureCreationConfig>(
            configuration.GetSection("FableCraft:AdventureCreationConfig"));

        services.AddScoped<IAdventureCreationService, AdventureCreationService>();
        services.AddMessageHandler<AddAdventureToKnowledgeGraphCommand, AddAdventureToKnowledgeGraphCommandHandler>();

        return services;
    }
}