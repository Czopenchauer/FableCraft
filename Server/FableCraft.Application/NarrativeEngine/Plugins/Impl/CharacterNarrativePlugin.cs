using System.ComponentModel;
using System.Text;

using FableCraft.Infrastructure;
using FableCraft.Infrastructure.Clients;

using Microsoft.SemanticKernel;

namespace FableCraft.Application.NarrativeEngine.Plugins.Impl;

/// <summary>
///     Plugin providing NPC character narrative knowledge graph search capabilities.
///     Use this to search for NPC-specific information, memories, relationships, and narrative history.
///     This plugin is intended for CharacterReflectionAgent, CharacterTrackerAgent, and CharacterStateAgent only.
/// </summary>
internal class CharacterNarrativePlugin : CharacterPluginBase
{
    private const int MaxQueries = 10;
    private readonly IRagClientFactory _ragClientFactory;
    private int _queryCount;

    public CharacterNarrativePlugin(IRagClientFactory ragClientFactory)
    {
        _ragClientFactory = ragClientFactory;
    }

    [KernelFunction("search_character_narrative")]
    [Description(
        "Search the character knowledge graph for character-specific information including memories, relationships, personal history, motivations, and character development. Use this to understand character backgrounds and their narrative arcs.")]
    public async Task<string> SearchCharacterNarrativeAsync(
        [Description("List of queries for character information to retrieve (memories, relationships, history, motivations, etc.)")]
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
            return $"Maximum number of character narrative queries ({MaxQueries}) reached. You cannot perform more searches!";
        }

        var ragSearch = await _ragClientFactory.CreateSearchClientForAdventure(CallerContext.AdventureId, CancellationToken.None);
        var datasets = new List<string>
        {
            RagClientExtensions.GetCharacterDatasetName(CharacterId)
        };

        var queryCombined = query.Select(x =>
        {
            if (!string.IsNullOrEmpty(time))
            {
                return $"{x}, level of details: {levelOfDetails}. Current time: {time} - use it to retrieve fresh knowledge where possible.";
            }
            return $"{x}, level of details: {levelOfDetails}";
        }).ToArray();
        var results = await ragSearch.SearchAsync(CallerContext!, datasets, queryCombined);

        if (!results.Any() || results.All(r => !r.Response.Results.Any()))
        {
            return "Character knowledge graph does not contain any data yet.";
        }

        _queryCount++;
        var allResults = results.SelectMany(x => x.Response.Results).ToList();

        var response = new StringBuilder();
        response.AppendLine("Character Knowledge:");
        foreach (var result in allResults)
        {
            response.AppendLine($"- {result.Text}");
        }

        return response.ToString();
    }
}