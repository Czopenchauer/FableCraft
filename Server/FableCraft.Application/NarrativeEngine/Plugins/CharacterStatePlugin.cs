using System.ComponentModel;
using System.Text.Json;

using FableCraft.Application.NarrativeEngine.Models;

using Microsoft.SemanticKernel;

using Serilog;

namespace FableCraft.Application.NarrativeEngine.Plugins;

internal sealed class CharacterStatePlugin
{
    private readonly Dictionary<string, CharacterContext> _charactersByName;
    private readonly JsonSerializerOptions _jsonOptions;

    public CharacterStatePlugin(IEnumerable<CharacterContext> characters, ILogger logger)
    {
        _charactersByName = characters.ToDictionary(
            c => c.Name,
            c => c,
            StringComparer.OrdinalIgnoreCase);

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true
        };
    }

    [KernelFunction("get_character_state")]
    [Description(
        "Retrieve the current state of a character by their name. Returns detailed information about the character including their description, stats, tracker, and development tracker.")]
    public string GetCharacterState(
        [Description("The name of the character to look up. Use exactly the same name as defined in the character context.")]
        string characterName)
    {
        if (!_charactersByName.TryGetValue(characterName, out CharacterContext? character))
        {
            return $"Character '{characterName}' not found. Available characters: {string.Join(", ", _charactersByName.Keys)}";
        }

        var result = new
        {
            character.Name,
            character.Description,
            CharacterState = character.CharacterState,
            CharacterTracker = character.CharacterTracker,
            DevelopmentTracker = character.DevelopmentTracker
        };

        return JsonSerializer.Serialize(result, _jsonOptions);
    }
}

