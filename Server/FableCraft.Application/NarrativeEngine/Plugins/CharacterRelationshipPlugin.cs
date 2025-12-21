using System.ComponentModel;

using FableCraft.Application.NarrativeEngine.Models;

using Microsoft.SemanticKernel;

using Serilog;

namespace FableCraft.Application.NarrativeEngine.Plugins;

internal sealed class CharacterRelationshipPlugin
{
    private readonly CharacterContext _context;
    private readonly ILogger _logger;

    public CharacterRelationshipPlugin(CharacterContext context, ILogger logger)
    {
        _context = context;
        _logger = logger;
    }

    [KernelFunction("get_relationship")]
    [Description(
        "Get a character's current view of their relationship with another character. Returns the relationship data including trust, affection, impression, and other details.")]
    public string GetRelationship(
        [Description("The name of the other character (the target of the relationship)")]
        string targetCharacterName)
    {
        _logger.Information("Getting relationship for {CharacterName} -> {TargetCharacterName}",
            _context.Name, targetCharacterName);

        var relationship = _context.Relationships.SingleOrDefault(x => string.Compare(x.TargetCharacterName, targetCharacterName, StringComparison.InvariantCultureIgnoreCase) == 0);

        if (relationship == null)
        {
            return $"No relationship found between '{_context.Name}' and '{targetCharacterName}'.";
        }

        return $"""
                {_context.Name}'s view of {targetCharacterName}:
                {relationship.Data}
                (Last updated: at {relationship.StoryTracker?.Time ?? string.Empty} in {relationship.StoryTracker?.Location ?? string.Empty})
                """;
    }
}

