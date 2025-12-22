using System.ComponentModel;
using System.Text;

using FableCraft.Infrastructure.Clients;

using Microsoft.SemanticKernel;

namespace FableCraft.Application.NarrativeEngine.Plugins;

/// <summary>
///     Plugin providing main character (player character) narrative knowledge graph search capabilities.
///     Use this to search for main character-specific information, memories, goals, and narrative history.
/// </summary>
internal class MainCharacterNarrativePlugin
{
    private readonly CallerContext _callerContext;
    private readonly IRagSearch _ragSearch;
    private const int MaxQueries = 10;
    private int _queryCount;

    public MainCharacterNarrativePlugin(IRagSearch ragSearch, CallerContext callerContext)
    {
        _ragSearch = ragSearch;
        _callerContext = callerContext;
    }

    [KernelFunction("search_main_character_narrative")]
    [Description(
        "Search the main character (player character) knowledge graph for their personal history, memories, goals, relationships, and narrative progression. Use this to understand the protagonist's journey and development.")]
    public async Task<string> SearchMainCharacterNarrativeAsync(
        [Description("List of queries for main character information to retrieve (memories, goals, relationships, personal history, etc.)")]
        string[] query,
        [Description("Level of details to include in the response (e.g., brief, detailed, comprehensive)")]
        string levelOfDetails)
    {
        if (_queryCount >= MaxQueries)
        {
            return $"Maximum number of main character narrative queries ({MaxQueries}) reached. You cannot perform more searches!";
        }

        var datasets = new List<string>
        {
            RagClientExtensions.GetMainCharacterDatasetName(_callerContext.AdventureId)
        };

        var queryCombined = query.Select(x => $"{x}, level of details: {levelOfDetails}").ToArray();
        var results = await _ragSearch.SearchAsync(_callerContext, datasets, queryCombined);

        if (!results.Any() || results.All(r => !r.Response.Results.Any()))
        {
            return "Main character knowledge graph does not contain any data yet.";
        }

        _queryCount++;
        var allResults = results.SelectMany(x => x.Response.Results).ToList();

        var response = new StringBuilder();
        response.AppendLine("Main Character Knowledge:");
        foreach (var result in allResults)
        {
            response.AppendLine($"- {result.Text}");
        }

        return response.ToString();
    }
}
