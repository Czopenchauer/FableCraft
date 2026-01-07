using System.ComponentModel;

using FableCraft.Application.NarrativeEngine.Agents;
using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Infrastructure.Clients;

using Microsoft.SemanticKernel;

using Serilog;

namespace FableCraft.Application.NarrativeEngine.Plugins.Impl;

/// <summary>
/// Plugin for the SimulationModerator to query characters during cohort simulation.
/// Maintains in-memory sessions per character with accumulated ChatHistory.
/// </summary>
internal sealed class QueryCharacterPlugin : PluginBase
{
    private readonly CharacterSimulationAgent _characterAgent;
    private readonly ILogger _logger;

    /// <summary>
    /// Sessions indexed by character name.
    /// </summary>
    private readonly Dictionary<string, CharacterSimulationSession> _sessions = new();

    /// <summary>
    /// Cohort simulation input context.
    /// </summary>
    private CohortSimulationInput _cohortInput = null!;

    public QueryCharacterPlugin(CharacterSimulationAgent characterAgent, ILogger logger)
    {
        _characterAgent = characterAgent;
        _logger = logger;
    }

    /// <summary>
    /// Set up the plugin with cohort simulation context.
    /// Initializes sessions for each cohort member.
    /// </summary>
    public Task SetupAsync(GenerationContext context, CallerContext callerContext, CohortSimulationInput cohortInput)
    {
        base.SetupAsync(context, callerContext);
        _cohortInput = cohortInput;

        // Initialize sessions for each cohort member
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
        "Query a character for their response to a situation. Returns the character's prose response. " +
        "Use query_type 'intention' to ask what they plan to do, 'response' for reactions to situations, " +
        "'reflection' for final simulation output.")]
    public async Task<string> QueryCharacterAsync(
        [Description("Character name (exact match from cohort members)")]
        string character,
        [Description("Query type: 'intention', 'response', or 'reflection'")]
        string queryType,
        [Description("What's happening that they're responding to")]
        string stimulus,
        [Description("What you're asking them")]
        string query,
        CancellationToken cancellationToken = default)
    {
        // Validate character exists in cohort
        if (!_sessions.TryGetValue(character, out var session))
        {
            var availableCharacters = string.Join(", ", _sessions.Keys);
            _logger.Warning("Character '{Character}' not found in cohort sessions", character);
            return $"Error: Character '{character}' not found in cohort. Available characters: {availableCharacters}";
        }

        if (!Enum.TryParse<CharacterQueryType>(queryType, true, out var parsedType))
        {
            return $"Error: Invalid query_type '{queryType}'. Must be 'intention', 'response', or 'reflection'.";
        }

        var characterContext = Context!.Characters.FirstOrDefault(c => c.Name == character);
        if (characterContext == null)
        {
            return $"Error: Character '{character}' context not found.";
        }

        _logger.Information(
            "Querying character {Character} with {QueryType}: {Query}",
            character, parsedType, query.Length > 100 ? query[..100] + "..." : query);

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

            if (parsedType == CharacterQueryType.Reflection && response.SubmittedReflection != null)
            {
                session.SubmittedReflection = response.SubmittedReflection;
                _logger.Information(
                    "Character {Character} submitted reflection with {SceneCount} scenes",
                    character, response.SubmittedReflection.Scenes.Count);
            }

            return response.ProseResponse;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error querying character {Character}", character);
            return $"Error: Failed to query character '{character}': {ex.Message}";
        }
    }

    /// <summary>
    /// Get all sessions for post-simulation processing.
    /// </summary>
    public IReadOnlyDictionary<string, CharacterSimulationSession> GetSessions() => _sessions;

    /// <summary>
    /// Get all completed reflections (characters who submitted their reflection).
    /// </summary>
    public Dictionary<string, StandaloneSimulationOutput> GetCompletedReflections()
    {
        return _sessions
            .Where(s => s.Value.ReflectionSubmitted)
            .ToDictionary(
                s => s.Key,
                s => s.Value.SubmittedReflection!);
    }

    /// <summary>
    /// Check if all characters have submitted their reflections.
    /// </summary>
    public bool AllReflectionsSubmitted => _sessions.Values.All(s => s.ReflectionSubmitted);

    /// <summary>
    /// Get list of characters who haven't submitted reflections yet.
    /// </summary>
    public IEnumerable<string> GetPendingReflectionCharacters()
    {
        return _sessions
            .Where(s => !s.Value.ReflectionSubmitted)
            .Select(s => s.Key);
    }
}
