using System.ComponentModel;
using System.Text;

using FableCraft.Application.NarrativeEngine.Agents.Builders;
using FableCraft.Infrastructure;
using FableCraft.Infrastructure.Clients;

using Microsoft.SemanticKernel;

using Serilog;

namespace FableCraft.Application.NarrativeEngine.Plugins.Impl;

/// <summary>
///     Plugin providing tools for character agents during cohort simulation.
///     Includes knowledge graph queries.
/// </summary>
internal class CharacterSimulationToolsPlugin : CharacterPluginBase
{
    private const int MaxQueries = 10;
    private readonly ILogger _logger;
    private readonly IRagClientFactory _ragClientFactory;
    private int _queryCount;

    public CharacterSimulationToolsPlugin(IRagClientFactory ragClientFactory, ILogger logger)
    {
        _ragClientFactory = ragClientFactory;
        _logger = logger;
    }

    [KernelFunction("query_knowledge_graph")]
    [Description(
        "Query the knowledge graph for world information or your personal memories. Use 'world' for locations, lore, events, factions. Use 'personal' for your own memories, experiences, and conclusions.")]
    public async Task<string> QueryKnowledgeGraphAsync(
        [Description("Which graph to query: 'world' for world knowledge, 'personal' for your memories")]
        string graph,
        [Description("List of queries to execute (batch multiple related queries together)")]
        string[] queries)
    {
        ProcessExecutionContext.SceneId.Value = CallerContext!.SceneId;
        ProcessExecutionContext.AdventureId.Value = CallerContext.AdventureId;
        if (_queryCount >= MaxQueries)
        {
            _logger.Warning(
                "Maximum knowledge graph queries reached for character {CharacterId} in AdventureId: {AdventureId}",
                CharacterId,
                CallerContext?.AdventureId);
            return $"Maximum number of knowledge graph queries ({MaxQueries}) reached. You cannot perform more searches!";
        }

        var graphType = graph.ToLowerInvariant();
        List<string> datasets;

        switch (graphType)
        {
            case "world":
                datasets = [RagClientExtensions.GetWorldDatasetName()];
                break;
            case "personal":
                datasets = [RagClientExtensions.GetCharacterDatasetName(CharacterId)];
                break;
            default:
                return $"Invalid graph type '{graph}'. Use 'world' or 'personal'.";
        }

        var ragSearch = await _ragClientFactory.CreateSearchClientForAdventure(CallerContext.AdventureId, CancellationToken.None);
        var results = await ragSearch.SearchAsync(CallerContext!, datasets, queries);

        if (!results.Any() || results.All(r => !r.Response.Results.Any()))
        {
            return graphType == "world"
                ? "World knowledge graph does not contain relevant information for your queries."
                : "Your personal knowledge graph does not contain relevant information for your queries.";
        }

        _queryCount++;
        var allResults = results.SelectMany(x => x.Response.Results).ToList();

        var response = new StringBuilder();
        response.AppendLine(graphType == "world" ? "World Knowledge:" : "Personal Knowledge:");
        foreach (var result in allResults)
        {
            response.AppendLine($"- {result.Text}");
        }

        return response.ToString();
    }
}