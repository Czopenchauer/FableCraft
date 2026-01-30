using System.ComponentModel;

using FableCraft.Application.NarrativeEngine.Agents;
using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Infrastructure;
using FableCraft.Infrastructure.Clients;

using Microsoft.SemanticKernel;

namespace FableCraft.Application.NarrativeEngine.Plugins.Impl;

/// <summary>
///     Plugin that exposes character emulation functionality as a Semantic Kernel function.
///     Wraps CharacterAgent and delegates the emulation work to it.
/// </summary>
internal sealed class CharacterEmulationPlugin : PluginBase
{
    private readonly CharacterAgent _characterAgent;

    public CharacterEmulationPlugin(CharacterAgent characterAgent)
    {
        _characterAgent = characterAgent;
    }

    public override async Task SetupAsync(GenerationContext context, CallerContext callerContext)
    {
        await base.SetupAsync(context, callerContext);
        await _characterAgent.Setup(context);
    }

    [KernelFunction("emulate_character_action")]
    [Description(
        "Emulate a character's action based on the character's personality, motivations, and the current situation. Use this to generate character dialogue, actions, reactions, or decisions that are consistent with their established traits and the narrative context.")]
    public async Task<string> EmulateCharacterActionAsync(
        [Description("The name of the character whose action is to be emulated. Use exactly the same name as defined in the character context.")]
        string characterName,
        [Description("What just happened requiring response")]
        string stimulus,
        [Description("\"What do they do?\" / \"What do they say?\" / \"How do they react?\"")]
        string query
    )
    {
        ProcessExecutionContext.SceneId.Value = CallerContext!.SceneId;
        ProcessExecutionContext.AdventureId.Value = CallerContext.AdventureId;

        var response = await _characterAgent.EmulateCharacterAction(stimulus, query, characterName);

        if (Context != null)
        {
            lock (Context)
            {
                if (!Context.CharacterEmulationOutputs.TryGetValue(characterName, out var outputs))
                {
                    outputs = [];
                    Context.CharacterEmulationOutputs[characterName] = outputs;
                }

                outputs.Add(new CharacterEmulationOutput(characterName, stimulus, query, response, outputs.Count + 1));
            }
        }

        return response;
    }
}