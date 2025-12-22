using System.ComponentModel;

using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Infrastructure.Persistence;

using Microsoft.SemanticKernel;

using Serilog;

namespace FableCraft.Application.NarrativeEngine.Plugins;

internal sealed class CharacterStatePlugin
{
    private readonly IEnumerable<CharacterContext> _context;
    private readonly ILogger _logger;

    public CharacterStatePlugin(IEnumerable<CharacterContext> context, ILogger logger)
    {
        _context = context;
        _logger = logger;
    }

    [KernelFunction("get_state")]
    [Description(
        "Get a current character (NOT MAIN CHARACTER) state information - about their physical state and their skills and abilities.")]
    public string GetState(
        [Description("The name of the character")]
        string targetCharacterName)
    {
        _logger.Information("Getting state for {CharacterName}", targetCharacterName);

        var characterContext = _context.SingleOrDefault(x => string.Compare(x.Name, targetCharacterName, StringComparison.InvariantCultureIgnoreCase) == 0);

        if (characterContext?.CharacterTracker == null)
        {
            return $"No state found for '{targetCharacterName}'. Available characters: {string.Join(", ", _context.Select(c => c.Name))}";
        }

        _logger.Information("Returning state for {CharacterName}", targetCharacterName);
        return characterContext.CharacterTracker.ToJsonString();
    }
}