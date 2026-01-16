using System.ComponentModel;

using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Infrastructure;
using FableCraft.Infrastructure.Clients;

using Microsoft.SemanticKernel;

using Serilog;

namespace FableCraft.Application.NarrativeEngine.Plugins.Impl;

internal sealed class CharacterRelationshipPlugin : CharacterPluginBase
{
    private readonly ILogger _logger;
    private CharacterContext? _characterContext;

    public CharacterRelationshipPlugin(ILogger logger)
    {
        _logger = logger;
    }

    public override Task SetupAsync(GenerationContext context, CallerContext callerContext, Guid characterId)
    {
        _characterContext = context.Characters.FirstOrDefault(c => c.CharacterId == characterId);
        return base.SetupAsync(context, callerContext, characterId);
    }

    [KernelFunction("get_relationship")]
    [Description(
        "Get a character's current view of their relationship with another character. Returns the relationship data including stance, trust, desire, intimacy, power, dynamic, and unspoken.")]
    public string GetRelationship(
        [Description("The name of the other character (the target of the relationship)")]
        string targetCharacterName)
    {
        ProcessExecutionContext.SceneId.Value = CallerContext!.SceneId;
        ProcessExecutionContext.AdventureId.Value = CallerContext.AdventureId;
        if (_characterContext == null)
        {
            return $"Character with ID '{CharacterId}' not found.";
        }

        _logger.Information("Getting relationship for {CharacterName} -> {TargetCharacterName}",
            _characterContext.Name,
            targetCharacterName);

        var relationship = _characterContext.Relationships.SingleOrDefault(x =>
            string.Compare(x.TargetCharacterName, targetCharacterName, StringComparison.InvariantCultureIgnoreCase) == 0);

        if (relationship == null)
        {
            return $"No relationship found between '{_characterContext.Name}' and '{targetCharacterName}'.";
        }

        return $"""
                {_characterContext.Name}'s view of {targetCharacterName}:
                {relationship.Data}
                (Last updated: at {relationship.UpdateTime ?? string.Empty})
                """;
    }
}