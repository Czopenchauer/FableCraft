using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Infrastructure.Clients;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;

namespace FableCraft.Application.NarrativeEngine.Plugins;

/// <summary>
///     Factory implementation that resolves plugins from DI and initializes them with runtime context.
/// </summary>
internal sealed class PluginFactory : IPluginFactory
{
    private readonly IServiceProvider _serviceProvider;

    public PluginFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<T> CreatePluginAsync<T>(GenerationContext context, CallerContext callerContext)
        where T : PluginBase
    {
        var plugin = _serviceProvider.GetRequiredService<T>();
        await plugin.SetupAsync(context, callerContext);
        return plugin;
    }

    public async Task AddPluginAsync<T>(IKernelBuilder kernel, GenerationContext context, CallerContext callerContext)
        where T : PluginBase
    {
        var plugin = await CreatePluginAsync<T>(context, callerContext);
        kernel.Plugins.Add(KernelPluginFactory.CreateFromObject(plugin));
    }

    public async Task<T> CreateCharacterPluginAsync<T>(GenerationContext context, CallerContext callerContext, Guid characterId)
        where T : CharacterPluginBase
    {
        var plugin = _serviceProvider.GetRequiredService<T>();
        await plugin.SetupAsync(context, callerContext, characterId);
        return plugin;
    }

    public async Task AddCharacterPluginAsync<T>(IKernelBuilder kernel, GenerationContext context, CallerContext callerContext, Guid characterId)
        where T : CharacterPluginBase
    {
        var plugin = await CreateCharacterPluginAsync<T>(context, callerContext, characterId);
        kernel.Plugins.Add(KernelPluginFactory.CreateFromObject(plugin));
    }
}