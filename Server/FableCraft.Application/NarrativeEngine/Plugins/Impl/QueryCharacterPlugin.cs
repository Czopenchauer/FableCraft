using System.ComponentModel;

using FableCraft.Application.NarrativeEngine.Agents;
using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Infrastructure;
using FableCraft.Infrastructure.Clients;

using Microsoft.SemanticKernel;

using Serilog;

namespace FableCraft.Application.NarrativeEngine.Plugins.Impl;

/// <summary>
///     Plugin for the SimulationModerator to query characters during cohort simulation.
///     Maintains in-memory sessions per character with accumulated ChatHistory.
/// </summary>
internal sealed class QueryCharacterPlugin : PluginBase
{
    private readonly CharacterSimulationAgent _characterAgent;
    private readonly ILogger _logger;

    /// <summary>
    ///     Sessions indexed by character name (case-insensitive).
    /// </summary>
    private readonly Dictionary<string, CharacterSimulationSession> _sessions =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    ///     Cohort simulation input context.
    /// </summary>
    private CohortSimulationInput _cohortInput = null!;

    public QueryCharacterPlugin(CharacterSimulationAgent characterAgent, ILogger logger)
    {
        _characterAgent = characterAgent;
        _logger = logger;
    }

    /// <summary>
    ///     Get all sessions for state persistence.
    /// </summary>
    public Dictionary<string, CharacterSimulationSession> GetAllSessions()
    {
        return new Dictionary<string, CharacterSimulationSession>(_sessions, StringComparer.OrdinalIgnoreCase);
    }

    public Task SetupAsync(GenerationContext context, CallerContext callerContext, CohortSimulationInput cohortInput)
    {
        base.SetupAsync(context, callerContext);
        _cohortInput = cohortInput;

        foreach (var member in cohortInput.CohortMembers)
        {
            var character = context.Characters.FirstOrDefault(c => c.Name == member.Name);
            if (character == null)
            {
                _logger.Warning("Cohort member {MemberName} not found in context characters", member.Name);
                continue;
            }

            _sessions[member.Name] = new CharacterSimulationSession
            {
                CharacterName = member.Name,
                CharacterId = character.CharacterId
            };

            _logger.Debug("Initialized session for cohort member {MemberName}", member.Name);
        }

        return Task.CompletedTask;
    }

    [KernelFunction("query_character")]
    [Description(
        "Query a character for their response to a situation. Returns the character's prose response. "
        + "Use query_type 'intention' to ask what they plan to do, 'response' for reactions to situations.")]
    public async Task<string> QueryCharacterAsync(
        [Description("Character name (exact match from cohort members)")]
        string character,
        [Description("Query type: 'intention' or 'response'")]
        string queryType,
        [Description("What's happening that they're responding to")]
        string stimulus,
        [Description("What you're asking them")]
        string query,
        CancellationToken cancellationToken = default)
    {
        ProcessExecutionContext.SceneId.Value = CallerContext!.SceneId;
        ProcessExecutionContext.AdventureId.Value = CallerContext.AdventureId;
        if (!_sessions.TryGetValue(character, out var session))
        {
            var availableCharacters = string.Join(", ", _sessions.Keys);
            _logger.Warning("Character '{Character}' not found in cohort sessions", character);
            return $"Error: Character '{character}' not found in cohort. Available characters: {availableCharacters}";
        }

        if (!Enum.TryParse(queryType, true, out CharacterQueryType parsedType) || parsedType == CharacterQueryType.Reflection)
        {
            return $"Error: Invalid query_type '{queryType}'. Must be 'intention' or 'response'.";
        }

        var characterContext = Context!.Characters.FirstOrDefault(c => c.Name == character);
        if (characterContext == null)
        {
            return $"Error: Character '{character}' context not found.";
        }

        _logger.Information(
            "Querying character {Character} with {QueryType}: {Query}",
            character,
            parsedType,
            query);

        try
        {
            var response = await _characterAgent.InvokeQuery(
                Context!,
                characterContext,
                parsedType,
                stimulus,
                query,
                session.ChatHistory,
                _cohortInput,
                cancellationToken);

            return response.ProseResponse;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error querying character {Character}", character);
            return $"Error: Failed to query character '{character}': {ex.Message}";
        }
    }
}