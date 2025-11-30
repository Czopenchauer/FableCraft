using FableCraft.Application.Model;
using FableCraft.Infrastructure.Persistence;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FableCraft.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CharacterController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;

    public CharacterController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Get all characters grouped by CharacterId for a specific adventure
    /// </summary>
    [HttpGet("adventure/{adventureId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<CharacterGroupDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<CharacterGroupDto>>> GetCharactersByAdventure(
        Guid adventureId, 
        CancellationToken cancellationToken)
    {
        var characterGroups = await _dbContext
            .Characters
            .Where(x => x.AdventureId == adventureId)
            .GroupBy(x => x.CharacterId)
            .ToListAsync(cancellationToken);

        var result = characterGroups.Select(group => new CharacterGroupDto
        {
            CharacterId = group.Key,
            Versions = group.Select(character => new CharacterVersionDto
            {
                Id = character.Id,
                SceneId = character.SceneId,
                SequenceNumber = character.SequenceNumber,
                Description = character.Description,
                CharacterStats = character.CharacterStats,
                Tracker = character.Tracker
            }).ToList()
        });

        return Ok(result);
    }
}

