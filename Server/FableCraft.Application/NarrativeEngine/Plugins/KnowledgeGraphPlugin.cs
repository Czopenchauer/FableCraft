using System.ComponentModel;

using FableCraft.Infrastructure.Clients;

using Microsoft.SemanticKernel;

namespace FableCraft.Application.NarrativeEngine.Plugins;

/// <summary>
///     Plugin providing knowledge graph search capabilities to all agents
/// </summary>
internal class KnowledgeGraphPlugin
{
    private readonly CallerContext _callerContext;
    private readonly IRagSearch _ragSearch;

    public KnowledgeGraphPlugin(IRagSearch ragSearch, CallerContext callerContext)
    {
        _ragSearch = ragSearch;
        _callerContext = callerContext;
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
        var queryCombined = query.Select(x => $"{x}, level of details: {levelOfDetails}").ToArray();
        var results = await _ragSearch.SearchAsync(_callerContext, queryCombined);

        if (!results.Any())
        {
            return "Knowledge graph does not contain any data yet.";
        }

        return string.Join("\n\n", results.SelectMany(x => x.Response.Results));
    }
}