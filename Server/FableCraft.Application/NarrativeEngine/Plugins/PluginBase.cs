using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Infrastructure.Clients;

namespace FableCraft.Application.NarrativeEngine.Plugins;

internal abstract class PluginBase
{
    protected GenerationContext? Context { get; private set; }

    protected CallerContext? CallerContext { get; private set; }

    public virtual Task SetupAsync(GenerationContext context, CallerContext callerContext)
    {
        Context = context;
        CallerContext = callerContext;
        return Task.CompletedTask;
    }
}

/// <summary>
///     Base class for plugins that need a specific character ID in addition to context.
///     E.g. CharacterNarrativePlugin and CharacterRelationshipPlugin.
/// </summary>
internal abstract class CharacterPluginBase : PluginBase
{
    protected Guid CharacterId { get; private set; }

    public virtual Task SetupAsync(GenerationContext context, CallerContext callerContext, Guid characterId)
    {
        CharacterId = characterId;
        return base.SetupAsync(context, callerContext);
    }
}