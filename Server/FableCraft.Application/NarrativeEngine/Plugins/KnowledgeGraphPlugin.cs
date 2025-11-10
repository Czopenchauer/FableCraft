using System.ComponentModel;
using FableCraft.Application.NarrativeEngine.Interfaces;
using FableCraft.Infrastructure.Clients;
using Microsoft.SemanticKernel;

namespace FableCraft.Application.NarrativeEngine.Plugins;

/// <summary>
/// Plugin providing knowledge graph search capabilities to all agents
/// </summary>
public class KnowledgeGraphPlugin : IKnowledgeGraphPlugin
{
    private readonly IRagSearch _ragSearch;
    private readonly string _adventureId;

    public KnowledgeGraphPlugin(IRagSearch ragSearch, string adventureId)
    {
        _ragSearch = ragSearch;
        _adventureId = adventureId;
    }

    [KernelFunction("search_knowledge_graph")]
    [Description("Search the knowledge graph for entities, relationships, and narrative data. Use this to query existing locations, characters, lore, items, events, and their relationships.")]
    public async Task<string> SearchKnowledgeGraphAsync(
        [Description("The search query describing what information to retrieve from the knowledge graph")]
        string query,
        CancellationToken cancellationToken = default)
    {
        var result = await _ragSearch.SearchAsync(_adventureId, query, cancellationToken);
        return result.Content ?? string.Empty;
    }
}
