using FableCraft.ProjectManagement.Services;

using FluentValidation;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FableCraft.ProjectManagement;

public static class StartupExtensions
{
    public static IServiceCollection AddProjectManagementServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddValidatorsFromAssemblyContaining<Models.ProjectDto>();

        services.Configure<ProjectManagementSettings>(configuration.GetSection(ProjectManagementSettings.SectionName));

        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<IProjectFileService, ProjectFileService>();
        services.AddScoped<IProjectChatService, ProjectChatService>();

        services.AddTransient<Plugins.ProjectFilePlugin>();
        services.AddTransient<Plugins.ProjectSearchPlugin>();

        return services;
    }
}