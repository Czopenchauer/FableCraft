using System.ComponentModel;

using FableCraft.Infrastructure;
using FableCraft.Infrastructure.Persistence;

using Microsoft.SemanticKernel;

using Serilog;

namespace FableCraft.Application.NarrativeEngine.Plugins.Impl;

internal sealed class CharacterDescriptionPlugin : PluginBase
{
    private readonly ILogger _logger;

    public CharacterDescriptionPlugin(ILogger logger)
    {
        _logger = logger;
    }

    [KernelFunction("get_character_details")]
    [Description(
        "Fetches the full details of a character by name. Returns description, appearance, general build, and current state information.")]
    public string FetchCharacterDetails(
        [Description("The name of the character to fetch details for")]
        string characterName)
    {
        ProcessExecutionContext.SceneId.Value = CallerContext!.SceneId;
        ProcessExecutionContext.AdventureId.Value = CallerContext.AdventureId;

        _logger.Information("Fetching character details for {CharacterName}", characterName);

        var character = Context!.Characters.FirstOrDefault(c =>
            string.Equals(c.Name, characterName, StringComparison.InvariantCultureIgnoreCase));

        if (character == null)
        {
            return $"Character '{characterName}' not found.";
        }

        return $"""
                Character: {character.Name}

                <description>
                {character.Description}
                </description>

                <appearance>
                Appearance: {character.CharacterTracker?.Appearance}
                General Build: {character.CharacterTracker?.GeneralBuild}
                </appearance>

                <current_state>
                Location: {character.CharacterTracker?.Location}
                State: {character.CharacterState?.ToJsonString()}
                </current_state>
                """;
    }
}