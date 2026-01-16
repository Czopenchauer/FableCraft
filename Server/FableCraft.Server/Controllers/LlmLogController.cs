using FableCraft.Application.Model;
using FableCraft.Infrastructure.Persistence;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FableCraft.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LlmLogController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;

    public LlmLogController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    ///     Get LLM logs by scene ID with pagination
    /// </summary>
    [HttpGet("scene/{sceneId:guid}")]
    [ProducesResponseType(typeof(LlmLogListResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<LlmLogListResponseDto>> GetBySceneId(
        Guid sceneId,
        [FromQuery] int offset = 0,
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.LlmCallLogs
            .Where(l => l.SceneId == sceneId)
            .OrderByDescending(l => l.ReceivedAt);

        var totalCount = await query.CountAsync(cancellationToken);

        var logs = await query
            .Skip(offset)
            .Take(limit)
            .Select(l => new LlmLogResponseDto
            {
                Id = l.Id,
                AdventureId = l.AdventureId,
                SceneId = l.SceneId,
                CallerName = l.CallerName,
                RequestContent = l.RequestContent,
                ResponseContent = l.ResponseContent,
                ReceivedAt = l.ReceivedAt,
                InputToken = l.InputToken,
                OutputToken = l.OutputToken,
                TotalToken = l.TotalToken,
                Duration = l.Duration
            })
            .ToListAsync(cancellationToken);

        return Ok(new LlmLogListResponseDto
        {
            Items = logs,
            TotalCount = totalCount
        });
    }
}
