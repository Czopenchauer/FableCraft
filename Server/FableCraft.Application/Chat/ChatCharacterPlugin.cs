using System.ComponentModel;

using FableCraft.Application.NarrativeEngine.Plugins;
using FableCraft.Infrastructure;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;

using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;

using Serilog;

namespace FableCraft.Application.Chat;

internal sealed class ChatCharacterPlugin : PluginBase
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
    private readonly ILogger _logger;

    public ChatCharacterPlugin(IDbContextFactory<ApplicationDbContext> dbContextFactory, ILogger logger)
    {
        _dbContextFactory = dbContextFactory;
        _logger = logger;
    }

    [KernelFunction("get_character")]
    [Description(
        "Fetches the description and details of a character by name. Returns name, description, appearance, and current state information.")]
    public async Task<string> GetCharacterAsync(
        [Description("The name of the character to look up. Always use full name.")]
        string characterName)
    {
        ProcessExecutionContext.SceneId.Value = CallerContext!.SceneId;
        ProcessExecutionContext.AdventureId.Value = CallerContext.AdventureId;

        _logger.Information("ChatCharacterPlugin: Fetching character {CharacterName} for adventure {AdventureId}",
            characterName, CallerContext.AdventureId);

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        var backgroundCharacter = await dbContext.BackgroundCharacters
            .Where(bc =>
                bc.AdventureId == CallerContext.AdventureId &&
                bc.Name.ToLower() == characterName.ToLower())
            .OrderByDescending(bc => bc.Version)
            .FirstOrDefaultAsync();

        if (backgroundCharacter != null)
        {
            return $"""
                    Character: {backgroundCharacter.Name}

                    <description>
                    {backgroundCharacter.Description}
                    </description>

                    <last_known>
                    Location: {backgroundCharacter.LastLocation}
                    Last Seen: {backgroundCharacter.LastSeenTime}
                    </last_known>
                    """;
        }

        return $"Character '{characterName}' not found in the adventure. Try a different name or check spelling.";
    }
}