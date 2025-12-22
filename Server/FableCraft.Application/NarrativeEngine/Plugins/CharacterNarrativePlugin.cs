using System.ComponentModel;
using System.Text;

using FableCraft.Infrastructure.Clients;

using Microsoft.SemanticKernel;

namespace FableCraft.Application.NarrativeEngine.Plugins;

/// <summary>
///     Plugin providing NPC character narrative knowledge graph search capabilities.
///     Use this to search for NPC-specific information, memories, relationships, and narrative history.
///     This plugin is intended for CharacterReflectionAgent, CharacterTrackerAgent, and CharacterStateAgent only.
/// </summary>
internal class CharacterNarrativePlugin
{
    private readonly CallerContext _callerContext;
    private readonly Guid _characterId;
    private readonly IRagSearch _ragSearch;
    private const int MaxQueries = 10;
    private int _queryCount;

    /// <summary>
    ///     Creates a plugin for searching NPC character narrative data.
    /// </summary>
    /// <param name="ragSearch">The RAG search service</param>
    /// <param name="callerContext">The caller context with adventure information</param>
    /// <param name="characterId">The character ID to search narrative data for</param>
    public CharacterNarrativePlugin(IRagSearch ragSearch, CallerContext callerContext, Guid characterId)
    {
        _ragSearch = ragSearch;
        _callerContext = callerContext;
        _characterId = characterId;
    }

    [KernelFunction("search_character_narrative")]
    [Description(
        "Search the character knowledge graph for character-specific information including memories, relationships, personal history, motivations, and character development. Use this to understand character backgrounds and their narrative arcs.")]
    public async Task<string> SearchCharacterNarrativeAsync(
        [Description("List of queries for character information to retrieve (memories, relationships, history, motivations, etc.)")]
        string[] query,
        [Description("Level of details to include in the response (e.g., brief, detailed, comprehensive)")]
        string levelOfDetails)
    {
        if (_queryCount >= MaxQueries)
        {
            return $"Maximum number of character narrative queries ({MaxQueries}) reached. You cannot perform more searches!";
        }

        var datasets = new List<string>
        {
            RagClientExtensions.GetCharacterDatasetName(_callerContext.AdventureId, _characterId)
        };

        var queryCombined = query.Select(x => $"{x}, level of details: {levelOfDetails}").ToArray();
        var results = await _ragSearch.SearchAsync(_callerContext, datasets, queryCombined);

        if (!results.Any() || results.All(r => !r.Response.Results.Any()))
        {
            return "Character knowledge graph does not contain any data yet.";
        }

        _queryCount++;
        var allResults = results.SelectMany(x => x.Response.Results).ToList();

        var response = new StringBuilder();
        response.AppendLine("Character Knowledge:");
        foreach (var result in allResults)
        {
            response.AppendLine($"- {result.Text}");
        }

        return response.ToString();
    }
}
