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
    public Task<string> SearchKnowledgeGraphAsync(
        [Description("Short description what information to retrieve from the knowledge graph")]
        string query)
    {
        _logger.Information("Performing knowledge graph search with query: {query}", query);
        // SearchResult result = await _ragSearch.SearchAsync(_adventureId, query);
        return Task.FromResult("Knowledge graph does not contain any data yet.");
    }
}