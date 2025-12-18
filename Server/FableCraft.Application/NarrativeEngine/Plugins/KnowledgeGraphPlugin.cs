using System.ComponentModel;
using System.Text;

using FableCraft.Infrastructure.Clients;

using Microsoft.SemanticKernel;

namespace FableCraft.Application.NarrativeEngine.Plugins;

/// <summary>
///     Plugin providing knowledge graph search capabilities to all agents
/// </summary>
internal class KnowledgeGraphPlugin
{
    private readonly CallerContext _callerContext;
    private readonly List<string> _datasets;
    private readonly IRagSearch _ragSearch;
    private const int MaxQueries = 5;
    private int _queryCount;

    public KnowledgeGraphPlugin(IRagSearch ragSearch, CallerContext callerContext, List<string> datasets)
    {
        _ragSearch = ragSearch;
        _callerContext = callerContext;
        _datasets = datasets;
    }

    [KernelFunction("search_knowledge_graph")]
    [Description(
        "Search the knowledge graph for entities, relationships, and narrative data. Use this to query existing locations, characters, lore, items, events, and their relationships.")]
    public async Task<string> SearchKnowledgeGraphAsync(
        [Description("List of queries what information to retrieve from the knowledge graph")]
        string[] query,
        [Description("Level of details to include in the response (e.g., brief, detailed, comprehensive)")]
        string levelOfDetails)
    {
        if (_queryCount >= MaxQueries)
        {
            return $"Maximum number of knowledge graph queries ({MaxQueries}) reached. You cannot perform more searches!";
        }

        var queryCombined = query.Select(x => $"{x}, level of details: {levelOfDetails}").ToArray();
        var results = await _ragSearch.SearchAsync(_callerContext, _datasets, queryCombined);

        if (!results.Any())
        {
            return "Knowledge graph does not contain any data yet.";
        }

        _queryCount++;
        var format = results.SelectMany(x => x.Response.Results).GroupBy(x => x.DatasetName);

        var response = new StringBuilder();
        foreach (var searchResultItems in format)
        {
            if (searchResultItems.Key == RagClientExtensions.GetWorldDatasetName(_callerContext.AdventureId))
            {
                response.AppendLine($"""
                                     World Knowledge:
                                     {string.Join("\n", searchResultItems.Select(x => $"- {x.Text}"))}
                                     """);
            }
            else
            {
                response.AppendLine($"""
                                     Character Knowledge:
                                     {string.Join("\n", searchResultItems.Select(x => $"- {x.Text}"))}
                                     """);
            }
        }

        return response.ToString();
    }
}