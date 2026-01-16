using System.ComponentModel;
using System.Text;

using FableCraft.Infrastructure;
using FableCraft.Infrastructure.Clients;

using Microsoft.SemanticKernel;

namespace FableCraft.Application.NarrativeEngine.Plugins.Impl;

/// <summary>
///     Plugin providing NPC character narrative knowledge graph search capabilities.
///     Use this to search for NPC-specific information, memories, relationships, and narrative history.
///     This plugin is intended for CharacterReflectionAgent, CharacterTrackerAgent, and CharacterStateAgent only.
/// </summary>
internal class CharacterNarrativePlugin : CharacterPluginBase
{
    private const int MaxQueries = 10;
    private readonly IRagSearch _ragSearch;
    private int _queryCount;

    public CharacterNarrativePlugin(IRagSearch ragSearch)
    {
        _ragSearch = ragSearch;
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
        ProcessExecutionContext.SceneId.Value = CallerContext!.SceneId;
        ProcessExecutionContext.AdventureId.Value = CallerContext.AdventureId;
        if (_queryCount >= MaxQueries)
        {
            return $"Maximum number of character narrative queries ({MaxQueries}) reached. You cannot perform more searches!";
        }

        var datasets = new List<string>
        {
            RagClientExtensions.GetCharacterDatasetName(CallerContext!.AdventureId, CharacterId)
        };

        var queryCombined = query.Select(x => $"{x}, level of details: {levelOfDetails}").ToArray();
        var results = await _ragSearch.SearchAsync(CallerContext!, datasets, queryCombined);

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