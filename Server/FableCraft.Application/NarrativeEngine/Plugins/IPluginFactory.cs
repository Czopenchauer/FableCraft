using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Infrastructure.Clients;

namespace FableCraft.Application.NarrativeEngine.Plugins;

/// <summary>
/// Factory for creating and adding Semantic Kernel plugins.
/// Uses DI to resolve plugin dependencies and SetupAsync for runtime context.
/// </summary>
internal interface IPluginFactory
{
    /// <summary>
    /// Creates a plugin instance with DI-resolved dependencies and calls SetupAsync.
    /// </summary>
    Task<T> CreatePluginAsync<T>(GenerationContext context, CallerContext callerContext)
        where T : PluginBase;

    /// <summary>
    /// Creates a plugin and adds it to the kernel builder's plugin collection.
    /// </summary>
    Task AddPluginAsync<T>(Microsoft.SemanticKernel.IKernelBuilder kernel, GenerationContext context, CallerContext callerContext)
        where T : PluginBase;

    /// <summary>
    /// Creates a character-specific plugin with DI-resolved dependencies and calls SetupAsync with characterId.
    /// </summary>
    Task<T> CreateCharacterPluginAsync<T>(GenerationContext context, CallerContext callerContext, Guid characterId)
        where T : CharacterPluginBase;

    /// <summary>
    /// Creates a character-specific plugin and adds it to the kernel builder's plugin collection.
    /// </summary>
    Task AddCharacterPluginAsync<T>(Microsoft.SemanticKernel.IKernelBuilder kernel, GenerationContext context, CallerContext callerContext, Guid characterId)
        where T : CharacterPluginBase;
}
