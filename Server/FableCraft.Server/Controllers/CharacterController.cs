using System.Text;

using FableCraft.Application.NarrativeEngine.Agents;
using FableCraft.Infrastructure.Clients;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;
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
                x.Description,
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
        var context = new CallerContext(typeof(CharacterController), adventureId, null);
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

        var context = new CallerContext(typeof(CharacterController), adventureId, null);
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

    #region Character Management Endpoints

    /// <summary>
    ///     Get lightweight list of all characters for sidebar display
    /// </summary>
    [HttpGet("list")]
    [ProducesResponseType(typeof(CharacterListItemDto[]), StatusCodes.Status200OK)]
    public async Task<ActionResult<CharacterListItemDto[]>> GetCharacterList(
        Guid adventureId,
        CancellationToken cancellationToken)
    {
        var characters = await dbContext.Characters
            .Where(c => c.AdventureId == adventureId)
            .Select(c => new CharacterListItemDto(
                c.Id,
                c.Name,
                c.Importance.Value))
            .ToArrayAsync(cancellationToken);

        return Ok(characters);
    }

    /// <summary>
    ///     Get full character detail with paginated memories and scene rewrites
    /// </summary>
    [HttpGet("{characterId:guid}")]
    [ProducesResponseType(typeof(CharacterDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CharacterDetailDto>> GetCharacterDetail(
        Guid adventureId,
        Guid characterId,
        [FromQuery] int memoriesLimit = 20,
        [FromQuery] int rewritesLimit = 20,
        CancellationToken cancellationToken = default)
    {
        var character = await dbContext.Characters
            .AsSplitQuery()
            .Where(c => c.AdventureId == adventureId && c.Id == characterId)
            .Include(c => c.CharacterStates.OrderByDescending(x => x.SequenceNumber).Take(1))
            .Include(c => c.CharacterRelationships)
            .Include(c => c.CharacterMemories.OrderByDescending(x => x.Salience).Take(memoriesLimit))
            .Include(c => c.CharacterSceneRewrites.OrderByDescending(x => x.SequenceNumber).Take(rewritesLimit))
            .FirstOrDefaultAsync(cancellationToken);

        if (character == null)
        {
            return NotFound(new { error = "Character not found" });
        }

        var latestState = character.CharacterStates.FirstOrDefault();
        if (latestState == null)
        {
            return NotFound(new { error = "Character has no state data" });
        }

        // Get total counts for pagination
        var totalMemoriesCount = await dbContext.Set<CharacterMemory>()
            .CountAsync(m => m.CharacterId == characterId, cancellationToken);
        var totalSceneRewritesCount = await dbContext.Set<CharacterSceneRewrite>()
            .CountAsync(r => r.CharacterId == characterId, cancellationToken);

        var dto = new CharacterDetailDto(
            character.Id,
            character.Name,
            character.Importance.Value,
            character.Description,
            latestState.CharacterStats,
            latestState.Tracker,
            character.CharacterMemories.Select(m => new CharacterMemoryDetailDto(
                m.Id,
                m.Summary,
                m.SceneTracker,
                m.Salience,
                m.Data
            )).ToList(),
            character.CharacterRelationships.GroupBy(x => x.TargetCharacterName).Select(r =>
            {
                var rel = r.OrderByDescending(x => x.SequenceNumber).First();
                return new CharacterRelationshipDetailDto(
                    rel.Id,
                    rel.TargetCharacterName,
                    rel.Dynamic,
                    rel.Data,
                    rel.SequenceNumber,
                    rel.UpdateTime
                );
            }).ToList(),
            character.CharacterSceneRewrites.Select(r => new CharacterSceneRewriteDto(
                r.Id,
                r.Content,
                r.SequenceNumber,
                r.SceneTracker
            )).ToList(),
            totalMemoriesCount,
            totalSceneRewritesCount);

        return Ok(dto);
    }

    /// <summary>
    ///     Get paginated memories for a character
    /// </summary>
    [HttpGet("{characterId:guid}/memories")]
    [ProducesResponseType(typeof(PaginatedResponse<CharacterMemoryDetailDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResponse<CharacterMemoryDetailDto>>> GetCharacterMemories(
        Guid adventureId,
        Guid characterId,
        [FromQuery] int offset = 0,
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var totalCount = await dbContext.Set<CharacterMemory>()
            .CountAsync(m => m.CharacterId == characterId, cancellationToken);

        var memories = await dbContext.Set<CharacterMemory>()
            .Where(m => m.CharacterId == characterId)
            .OrderByDescending(m => m.Salience)
            .Skip(offset)
            .Take(limit)
            .Select(m => new CharacterMemoryDetailDto(
                m.Id,
                m.Summary,
                m.SceneTracker,
                m.Salience,
                m.Data))
            .ToListAsync(cancellationToken);

        return Ok(new PaginatedResponse<CharacterMemoryDetailDto>(memories, totalCount, offset));
    }

    /// <summary>
    ///     Get paginated scene rewrites for a character
    /// </summary>
    [HttpGet("{characterId:guid}/scene-rewrites")]
    [ProducesResponseType(typeof(PaginatedResponse<CharacterSceneRewriteDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResponse<CharacterSceneRewriteDto>>> GetCharacterSceneRewrites(
        Guid adventureId,
        Guid characterId,
        [FromQuery] int offset = 0,
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var totalCount = await dbContext.Set<CharacterSceneRewrite>()
            .CountAsync(r => r.CharacterId == characterId, cancellationToken);

        var rewrites = await dbContext.Set<CharacterSceneRewrite>()
            .Where(r => r.CharacterId == characterId)
            .OrderByDescending(r => r.SequenceNumber)
            .Skip(offset)
            .Take(limit)
            .Select(r => new CharacterSceneRewriteDto(
                r.Id,
                r.Content,
                r.SequenceNumber,
                r.SceneTracker))
            .ToListAsync(cancellationToken);

        return Ok(new PaginatedResponse<CharacterSceneRewriteDto>(rewrites, totalCount, offset));
    }

    /// <summary>
    ///     Update character importance (only arc_important to significant transitions allowed)
    /// </summary>
    [HttpPatch("{characterId:guid}/importance")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCharacterImportance(
        Guid adventureId,
        Guid characterId,
        [FromBody] UpdateCharacterImportanceRequest request,
        CancellationToken cancellationToken)
    {
        var character = await dbContext.Characters
            .FirstOrDefaultAsync(c => c.AdventureId == adventureId && c.Id == characterId, cancellationToken);

        if (character == null)
        {
            return NotFound(new { error = "Character not found" });
        }

        // Validate importance transition - only arc_important <-> significant allowed
        var currentImportance = character.Importance.Value;
        var newImportance = request.Importance;

        var validImportances = new[] { "arc_important", "significant" };
        if (!validImportances.Contains(currentImportance) || !validImportances.Contains(newImportance))
        {
            return BadRequest(new { error = "Can only change importance between 'arc_important' and 'significant'" });
        }

        character.Importance = CharacterImportanceConverter.FromString(newImportance);
        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    /// <summary>
    ///     Update character profile (description and character stats in latest state)
    /// </summary>
    [HttpPatch("{characterId:guid}/profile")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCharacterProfile(
        Guid adventureId,
        Guid characterId,
        [FromBody] UpdateCharacterProfileRequest request,
        CancellationToken cancellationToken)
    {
        var latestState = await dbContext.Set<CharacterState>()
            .Where(s => s.CharacterId == characterId)
            .OrderByDescending(s => s.SequenceNumber)
            .FirstOrDefaultAsync(cancellationToken);

        if (latestState == null)
        {
            return NotFound(new { error = "Character state not found" });
        }

        latestState.CharacterStats = request.CharacterStats;
        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    /// <summary>
    ///     Update character tracker in latest state
    /// </summary>
    [HttpPatch("{characterId:guid}/tracker")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCharacterTracker(
        Guid adventureId,
        Guid characterId,
        [FromBody] UpdateCharacterTrackerRequest request,
        CancellationToken cancellationToken)
    {
        var latestState = await dbContext.Set<CharacterState>()
            .Where(s => s.CharacterId == characterId)
            .OrderByDescending(s => s.SequenceNumber)
            .FirstOrDefaultAsync(cancellationToken);

        if (latestState == null)
        {
            return NotFound(new { error = "Character state not found" });
        }

        latestState.Tracker = request.Tracker;
        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    /// <summary>
    ///     Update a specific character memory
    /// </summary>
    [HttpPatch("{characterId:guid}/memories/{memoryId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCharacterMemory(
        Guid adventureId,
        Guid characterId,
        Guid memoryId,
        [FromBody] UpdateCharacterMemoryRequest request,
        CancellationToken cancellationToken)
    {
        var memory = await dbContext.Set<CharacterMemory>()
            .FirstOrDefaultAsync(m => m.Id == memoryId && m.CharacterId == characterId, cancellationToken);

        if (memory == null)
        {
            return NotFound(new { error = "Memory not found" });
        }

        memory.Summary = request.Summary;
        memory.Salience = request.Salience;
        memory.Data = request.Data;
        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    /// <summary>
    ///     Update a specific character relationship
    /// </summary>
    [HttpPatch("{characterId:guid}/relationships/{relationshipId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCharacterRelationship(
        Guid adventureId,
        Guid characterId,
        Guid relationshipId,
        [FromBody] UpdateCharacterRelationshipRequest request,
        CancellationToken cancellationToken)
    {
        var relationship = await dbContext.Set<CharacterRelationship>()
            .FirstOrDefaultAsync(r => r.Id == relationshipId && r.CharacterId == characterId, cancellationToken);

        if (relationship == null)
        {
            return NotFound(new { error = "Relationship not found" });
        }

        relationship.Dynamic = request.Dynamic;
        relationship.Data = request.Data;
        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    #endregion
}