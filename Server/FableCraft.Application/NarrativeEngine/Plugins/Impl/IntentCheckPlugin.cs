using System.ComponentModel;

using FableCraft.Application.NarrativeEngine.Agents;
using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Infrastructure.Clients;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;

using Microsoft.SemanticKernel;

namespace FableCraft.Application.NarrativeEngine.Plugins.Impl;

/// <summary>
///     Plugin that exposes character intent checking as a Semantic Kernel function.
///     Used by SimulationPlanner to understand what characters intend to do.
/// </summary>
internal sealed class IntentCheckPlugin : PluginBase
{
    private readonly IntentCheckAgent _intentCheckAgent;
    private string[] _arcImportantCharacters = [];
    private string? _worldEvents;

    public IntentCheckPlugin(IntentCheckAgent intentCheckAgent)
    {
        _intentCheckAgent = intentCheckAgent;
    }

    public override Task SetupAsync(GenerationContext context, CallerContext callerContext)
    {
        base.SetupAsync(context, callerContext);

        _arcImportantCharacters = context.Characters
            .Where(c => c.Importance == CharacterImportance.ArcImportance)
            .Select(c => c.Name)
            .ToArray();

        var previousState = context.SceneContext?
            .OrderByDescending(x => x.SequenceNumber)
            .FirstOrDefault()?.Metadata.ChroniclerState;

        _worldEvents = previousState?.WorldMomentum?.ToJsonString();
        return Task.CompletedTask;
    }

    [KernelFunction("get_character_intent")]
    [Description(
        "Get a character's intentions for the upcoming simulation period. Returns what they plan to do, "
        + "who they're seeking out, who they're avoiding, and what their primary focus is.")]
    public async Task<string> GetCharacterIntentAsync(
        [Description("The name of the character to check intentions for. Must match exactly.")]
        string characterName,
        [Description("Optional: Previous intentions if this character was simulated before.")]
        string? previousIntentions = null,
        CancellationToken cancellationToken = default)
    {
        var character = Context!.Characters.FirstOrDefault(c =>
            string.Equals(c.Name, characterName, StringComparison.OrdinalIgnoreCase));

        if (character == null)
        {
            return $"Character '{characterName}' not found. Available characters: {string.Join(", ", Context.Characters.Select(c => c.Name))}";
        }

        var input = new IntentCheckInput
        {
            Character = character,
            ArcImportantCharacters = _arcImportantCharacters,
            WorldEvents = _worldEvents,
            PreviousIntentions = previousIntentions
        };

        var result = await _intentCheckAgent.Invoke(Context, input, cancellationToken);

        return result.ToJsonString();
    }
}