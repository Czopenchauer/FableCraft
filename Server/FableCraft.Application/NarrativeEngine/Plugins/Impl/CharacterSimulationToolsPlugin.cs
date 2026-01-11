using System.ComponentModel;
using System.Text;
using System.Text.Json;

using FableCraft.Application.NarrativeEngine.Agents.Builders;
using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Infrastructure.Clients;

using Microsoft.SemanticKernel;

using Serilog;

namespace FableCraft.Application.NarrativeEngine.Plugins.Impl;

/// <summary>
///     Plugin providing tools for character agents during cohort simulation.
///     Includes knowledge graph queries and reflection submission.
/// </summary>
internal class CharacterSimulationToolsPlugin : CharacterPluginBase
{
    private const int MaxQueries = 10;
    private readonly ILogger _logger;
    private readonly IRagSearch _ragSearch;
    private int _queryCount;

    public CharacterSimulationToolsPlugin(IRagSearch ragSearch, ILogger logger)
    {
        _ragSearch = ragSearch;
        _logger = logger;
    }

    /// <summary>
    ///     The submitted reflection output from the character.
    ///     Set when submit_reflection is called.
    /// </summary>
    public StandaloneSimulationOutput? SubmittedReflection { get; private set; }

    [KernelFunction("query_knowledge_graph")]
    [Description(
        "Query the knowledge graph for world information or your personal memories. Use 'world' for locations, lore, events, factions. Use 'personal' for your own memories, experiences, and conclusions.")]
    public async Task<string> QueryKnowledgeGraphAsync(
        [Description("Which graph to query: 'world' for world knowledge, 'personal' for your memories")]
        string graph,
        [Description("List of queries to execute (batch multiple related queries together)")]
        string[] queries)
    {
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
                datasets = [RagClientExtensions.GetWorldDatasetName(CallerContext!.AdventureId)];
                break;
            case "personal":
                datasets = [RagClientExtensions.GetCharacterDatasetName(CallerContext!.AdventureId, CharacterId)];
                break;
            default:
                return $"Invalid graph type '{graph}'. Use 'world' or 'personal'.";
        }

        var results = await _ragSearch.SearchAsync(CallerContext!, datasets, queries);

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

    [KernelFunction("submit_reflection")]
    [Description(
        "Submit your complete simulation output. Call this only during the reflection query to provide your scenes, state updates, and other outputs.")]
    public string SubmitReflection(
        [Description("Your complete reflection output as JSON (scenes, relationship_updates, profile_updates, tracker_updates, etc.)")]
        string outputJson)
    {
        try
        {
            var options = PromptSections.GetJsonOptions(true);
            var reflection = JsonSerializer.Deserialize<StandaloneSimulationOutput>(outputJson, options);

            if (reflection == null)
            {
                _logger.Error("Failed to parse reflection output - deserialize returned null");
                return "Error: Failed to parse reflection output. Please ensure it's valid JSON.";
            }

            if (reflection.Scenes.Count == 0)
            {
                _logger.Warning("Reflection submitted with no scenes for character {CharacterId}", CharacterId);
                return "Warning: Reflection submitted but contains no scenes. Please include at least one scene.";
            }

            SubmittedReflection = reflection;

            _logger.Information(
                "Reflection submitted for character {CharacterId}: {SceneCount} scenes, {RelCount} relationship updates",
                CharacterId,
                reflection.Scenes.Count,
                reflection.RelationshipUpdates?.Count ?? 0);

            return "Reflection submitted successfully.";
        }
        catch (JsonException ex)
        {
            _logger.Error(ex, "JSON parse error in reflection submission for character {CharacterId}", CharacterId);
            return $"Error: Invalid JSON in reflection output. {ex.Message}";
        }
    }
}