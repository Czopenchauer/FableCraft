using System.ComponentModel;
using System.Text;

using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Infrastructure.Clients;

using Microsoft.SemanticKernel;

using Serilog;

namespace FableCraft.Application.NarrativeEngine.Plugins;

/// <summary>
///     Plugin providing knowledge graph search capabilities to all agents
/// </summary>
internal class CharacterGraphPlugin
{
    private readonly CallerContext _callerContext;
    private readonly List<string> _datasets;
    private readonly IRagSearch _ragSearch;
    private readonly ILogger _logger;
    private const int MaxQueries = 5;
    private int _queryCount;

    public CharacterGraphPlugin(IRagSearch ragSearch, CallerContext callerContext, CharacterContext characterContext, ILogger logger)
    {
        _ragSearch = ragSearch;
        _callerContext = callerContext;
        _datasets = [
            RagClientExtensions.GetCharacterDatasetName(callerContext.AdventureId, characterContext.CharacterId),
            RagClientExtensions.GetWorldDatasetName(callerContext.AdventureId)
        ];
        _logger = logger;
    }

    [KernelFunction("search_knowledge_graph")]
    [Description(
        "Search the knowledge graph for character memories, event and world knowledge relevant to the provided queries. Always verify if character should know about the world info!")]
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
            _logger.Error($"No knowledge graph found for {string.Join(",", queryCombined)}");
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