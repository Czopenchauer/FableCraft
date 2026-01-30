using System.ComponentModel;
using System.Text;

using FableCraft.Infrastructure;
using FableCraft.Infrastructure.Clients;

using Microsoft.SemanticKernel;

namespace FableCraft.Application.NarrativeEngine.Plugins.Impl;

/// <summary>
///     Plugin providing main character (player character) narrative knowledge graph search capabilities.
///     Use this to search for main character-specific information, memories, goals, and narrative history.
/// </summary>
internal class MainCharacterNarrativePlugin : PluginBase
{
    private const int MaxQueries = 10;
    private readonly IRagClientFactory _ragClientFactory;
    private int _queryCount;

    public MainCharacterNarrativePlugin(IRagClientFactory ragClientFactory)
    {
        _ragClientFactory = ragClientFactory;
    }

    [KernelFunction("search_main_character_narrative")]
    [Description(
        "Search the main character (player character) knowledge graph for their personal history, memories, goals, relationships, and narrative progression. Use this to understand the protagonist's journey and development.")]
    public async Task<string> SearchMainCharacterNarrativeAsync(
        [Description("List of queries for main character information to retrieve (memories, goals, relationships, personal history, etc.)")]
        string[] query,
        [Description("Level of details to include in the response (e.g., brief, detailed, comprehensive)")]
        string levelOfDetails,
        [Description("Time when to look. Provide the time if you want to ask for specific period - for example when asking about current state, provide the current time. For history - provide the historical data. If time is not needed provide empty string or null.")]
        string? time)
    {
        ProcessExecutionContext.SceneId.Value = CallerContext!.SceneId;
        ProcessExecutionContext.AdventureId.Value = CallerContext.AdventureId;
        if (_queryCount >= MaxQueries)
        {
            return $"Maximum number of main character narrative queries ({MaxQueries}) reached. You cannot perform more searches!";
        }

        var datasets = new List<string>
        {
            RagClientExtensions.GetMainCharacterDatasetName()
        };

        var queryCombined = query.Select(x =>
        {
            if (!string.IsNullOrEmpty(time))
            {
                return $"{x}, level of details: {levelOfDetails}. Current time: {time} - use it to retrieve fresh knowledge where possible.";
            }
            return $"{x}, level of details: {levelOfDetails}";
        }).ToArray();
        var ragSearch = await _ragClientFactory.CreateSearchClientForAdventure(CallerContext.AdventureId, CancellationToken.None);
        var results = await ragSearch.SearchAsync(CallerContext!, datasets, queryCombined);

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