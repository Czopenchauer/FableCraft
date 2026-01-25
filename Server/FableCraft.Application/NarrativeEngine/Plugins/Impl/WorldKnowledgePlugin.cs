using System.ComponentModel;
using System.Text;

using FableCraft.Infrastructure;
using FableCraft.Infrastructure.Clients;

using Microsoft.SemanticKernel;

using Serilog;

namespace FableCraft.Application.NarrativeEngine.Plugins.Impl;

/// <summary>
///     Plugin providing world knowledge graph search capabilities.
///     Use this to search for locations, lore, items, world events, and general setting information.
/// </summary>
internal class WorldKnowledgePlugin : PluginBase
{
    private const int MaxQueries = 10;
    private readonly ILogger _logger;
    private readonly IRagSearch _ragSearch;
    private int _queryCount;

    public WorldKnowledgePlugin(IRagSearch ragSearch, ILogger logger)
    {
        _ragSearch = ragSearch;
        _logger = logger;
    }

    [KernelFunction("search_world_knowledge")]
    [Description(
        "Search the world knowledge graph for locations, lore, items, events, and world-building information. Use this to query existing world data like places, historical events, magical systems, and setting details.")]
    public async Task<string> SearchWorldKnowledgeAsync(
        [Description("List of queries for world information to retrieve (locations, lore, items, events, etc.)")]
        string[] query,
        [Description("Level of details to include in the response (e.g., brief, detailed, comprehensive)")]
        string levelOfDetails)
    {
        ProcessExecutionContext.SceneId.Value = CallerContext!.SceneId;
        ProcessExecutionContext.AdventureId.Value = CallerContext.AdventureId;
        if (_queryCount >= MaxQueries)
        {
            _logger.Warning("Maximum number of world knowledge queries reached for AdventureId: {AdventureId} and caller {caller}",
                CallerContext?.AdventureId,
                CallerContext?.CallerType);
            return $"Maximum number of world knowledge queries ({MaxQueries}) reached. You cannot perform more searches!";
        }

        var datasets = new List<string>
        {
            RagClientExtensions.GetWorldDatasetName()
        };

        var queryCombined = query.Select(x => $"{x}, level of details: {levelOfDetails}").ToArray();
        var results = await _ragSearch.SearchAsync(CallerContext!, datasets, queryCombined);

        if (!results.Any() || results.All(r => !r.Response.Results.Any()))
        {
            return "World knowledge graph does not contain any data yet.";
        }

        _queryCount++;
        var allResults = results.SelectMany(x => x.Response.Results).ToList();

        var response = new StringBuilder();
        response.AppendLine("World Knowledge:");
        foreach (var result in allResults)
        {
            response.AppendLine($"- {result.Text}");
        }

        return response.ToString();
    }
}