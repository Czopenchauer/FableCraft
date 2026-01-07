using System.Text;

using FableCraft.Application.NarrativeEngine.Agents;
using FableCraft.Infrastructure.Clients;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Server.Models;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FableCraft.Server.Controllers;

[ApiController]
[Route("api/[controller]/{adventureId:guid}")]
public class CharacterController(IRagSearch ragSearch, MainCharacterEmulatorAgent mainCharacterEmulatorAgent, ApplicationDbContext dbContext) : ControllerBase
{
    [HttpGet("all")]
    [ProducesResponseType(typeof(CharacterDto[]), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CharacterDto[]>> GetAllCharacters(
        Guid adventureId,
        CancellationToken cancellationToken)
    {
        var characters = await dbContext.Characters
            .AsSplitQuery()
            .Where(c => c.AdventureId == adventureId)
            .Include(c => c.CharacterStates.OrderByDescending(x => x.SequenceNumber).Take(1))
            .Include(m => m.CharacterMemories.OrderByDescending(x => x.Salience).Take(30))
            .Select(x => new CharacterDto(
                x.Id,
                x.Name,
                x.CharacterStates.Single().Description,
                x.CharacterStates.Single().CharacterStats,
                x.CharacterStates.Single().Tracker,
                x.CharacterMemories.Select(m => new CharacterMemoryDto(
                    m.Summary,
                    m.SceneTracker,
                    m.Salience,
                    m.Data
                )).ToList(),
                x.CharacterRelationships.Select(r => new CharacterRelationshipDto(
                    r.TargetCharacterName,
                    r.Data,
                    r.SequenceNumber,
                    r.UpdateTime
                )).ToList()
            ))
            .ToArrayAsync(cancellationToken);

        return Ok(characters);
    }

    [HttpPost("knowledge-graph/search")]
    [ProducesResponseType(typeof(KnowledgeGraphSearchResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<KnowledgeGraphSearchResponse>> SearchKnowledgeGraph(
        Guid adventureId,
        [FromBody] KnowledgeGraphSearchRequest request,
        CancellationToken cancellationToken)
    {
        if (request is { IsMainCharacter: false, CharacterId: null })
        {
            return BadRequest(new { error = "CharacterId is required when IsMainCharacter is false" });
        }

        var datasetName = request.IsMainCharacter
            ? RagClientExtensions.GetMainCharacterDatasetName(adventureId)
            : RagClientExtensions.GetCharacterDatasetName(adventureId, request.CharacterId!.Value);

        string[] datasets = [RagClientExtensions.GetWorldDatasetName(adventureId), datasetName];
        var context = new CallerContext(typeof(CharacterController), adventureId);
        var results = await ragSearch.SearchAsync(
            context,
            datasets,
            [request.Query],
            SearchType.GraphCompletion,
            cancellationToken);

        var response = new StringBuilder();
        var format = results.SelectMany(x => x.Response.Results).GroupBy(x => x.DatasetName);
        foreach (var searchResultItems in format)
        {
            if (searchResultItems.Key == RagClientExtensions.GetWorldDatasetName(adventureId))
            {
                response.AppendLine($"""
                                     World Knowledge:
                                     {string.Join("\n", searchResultItems.Select(x => $"- {x.Text}"))}
                                     """);
            }
            else
            {
                response.AppendLine($"""
                                     MainCharacter Knowledge:
                                     {string.Join("\n", searchResultItems.Select(x => $"- {x.Text}"))}
                                     """);
            }
        }

        return Ok(new KnowledgeGraphSearchResultItem(response.ToString()));
    }

    /// <summary>
    ///     Chat with RAG knowledge graph for a specific dataset type (world or main_character)
    /// </summary>
    [HttpPost("rag-chat")]
    [ProducesResponseType(typeof(RagChatResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RagChatResponse>> RagChat(
        Guid adventureId,
        [FromBody] RagChatRequest request,
        CancellationToken cancellationToken)
    {
        var datasetName = request.DatasetType.ToLower() switch
        {
            "world" => RagClientExtensions.GetWorldDatasetName(adventureId),
            "main_character" => RagClientExtensions.GetMainCharacterDatasetName(adventureId),
            _ => null
        };

        if (datasetName == null)
        {
            return BadRequest(new { error = "Invalid dataset type. Use 'world' or 'main_character'" });
        }

        var context = new CallerContext(typeof(CharacterController), adventureId);
        var results = await ragSearch.SearchAsync(
            context,
            [datasetName],
            [request.Query],
            SearchType.GraphCompletion,
            cancellationToken);

        var sources = results
            .SelectMany(x => x.Response.Results)
            .Select(x => new RagChatSource(x.DatasetName, x.Text))
            .ToList();

        var answer = string.Join("\n\n", sources.Select(s => s.Text));

        return Ok(new RagChatResponse(answer, sources));
    }

    /// <summary>
    ///     Emulate the main character to generate text from their perspective
    /// </summary>
    [HttpPost("emulate-main-character")]
    [ProducesResponseType(typeof(MainCharacterEmulationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MainCharacterEmulationResponse>> EmulateMainCharacter(
        Guid adventureId,
        [FromBody] EmulateMainCharacterRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await mainCharacterEmulatorAgent.InvokeAsync(
                new MainCharacterEmulationRequest
                {
                    AdventureId = adventureId,
                    Instruction = request.Instruction
                },
                cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}