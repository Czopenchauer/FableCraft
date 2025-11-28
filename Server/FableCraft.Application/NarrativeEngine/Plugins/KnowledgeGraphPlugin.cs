using System.ComponentModel;

using FableCraft.Infrastructure.Clients;

using Microsoft.SemanticKernel;

using Serilog;

namespace FableCraft.Application.NarrativeEngine.Plugins;

/// <summary>
///     Plugin providing knowledge graph search capabilities to all agents
/// </summary>
public class KnowledgeGraphPlugin
{
    private readonly string _adventureId;
    private readonly IRagSearch _ragSearch;
    private readonly ILogger _logger;

    public KnowledgeGraphPlugin(IRagSearch ragSearch, string adventureId, ILogger logger)
    {
        _ragSearch = ragSearch;
        _adventureId = adventureId;
        _logger = logger;
    }

    [KernelFunction("search_knowledge_graph")]
    [Description(
        "Search the knowledge graph for entities, relationships, and narrative data. Use this to query existing locations, characters, lore, items, events, and their relationships.")]
    public async Task<string> SearchKnowledgeGraphAsync(
        [Description("Description what information to retrieve from the knowledge graph")]
        string query,
        [Description("Level of details to include in the response (e.g., brief, detailed, comprehensive)")]
        string levelOfDetails)
    {
        _logger.Information("Performing knowledge graph search with query: {query}", query);
        var results = await _ragSearch.SearchAsync(_adventureId, $"{query}, level of details: {levelOfDetails}");

        if (results.Results.Count == 0)
        {
            return "Knowledge graph does not contain any data yet.";
        }

        return string.Join("\n\n", results);
    }
}