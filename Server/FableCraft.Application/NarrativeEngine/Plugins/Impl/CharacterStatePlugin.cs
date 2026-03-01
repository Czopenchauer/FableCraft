using System.ComponentModel;

using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Infrastructure;
using FableCraft.Infrastructure.Persistence;

using Microsoft.SemanticKernel;

using Serilog;

namespace FableCraft.Application.NarrativeEngine.Plugins.Impl;

internal sealed class CharacterStatePlugin : PluginBase
{
    private readonly ILogger _logger;

    public CharacterStatePlugin(ILogger logger)
    {
        _logger = logger;
    }

    private IEnumerable<CharacterContext> Characters => Context!.Characters;

    [KernelFunction("get_state")]
    [Description(
        "Get a current character (NOT MAIN CHARACTER) state information - about their physical state and their skills and abilities.")]
    public string GetState(
        [Description("The name of the character")]
        string targetCharacterName)
    {
        ProcessExecutionContext.SceneId.Value = CallerContext!.SceneId;
        ProcessExecutionContext.AdventureId.Value = CallerContext.AdventureId;
        _logger.Information("Getting state for {CharacterName}", targetCharacterName);

        if (Context!.MainCharacter.Name == targetCharacterName)
        {
            return Context.LatestTracker()?.MainCharacter?.MainCharacter.ToJsonString() ?? string.Empty;
        }

        var characterContext =
            Characters.SingleOrDefault(x => string.Compare(x.Name, targetCharacterName, StringComparison.InvariantCultureIgnoreCase) == 0)?.CharacterTracker;

        if (characterContext == null)
        {
            return $"No state found for '{targetCharacterName}'. Available characters: {string.Join(", ", Characters.Select(c => c.Name))}";
        }

        _logger.Information("Returning state for {CharacterName}", targetCharacterName);
        return characterContext.ToJsonString();
    }
}