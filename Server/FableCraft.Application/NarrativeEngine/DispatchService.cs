using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;

using Microsoft.EntityFrameworkCore;

namespace FableCraft.Application.NarrativeEngine;

/// <summary>
///     Service for managing character dispatch queue operations.
///     Handles sending, delivering, and resolving dispatches for remote character communication.
/// </summary>
internal sealed class DispatchService(
    IDbContextFactory<ApplicationDbContext> dbContextFactory)
{
    /// <summary>
    ///     Gets dispatches that are ready for delivery to a recipient based on current time.
    ///     Note: Timing comparison is done at the agent level since it requires in-world time parsing.
    /// </summary>
    public async Task<List<Dispatch>> GetDeliverableForRecipientAsync(
        Guid adventureId,
        string recipientName,
        CancellationToken ct = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        return await dbContext.Dispatches
            .Where(d => d.AdventureId == adventureId
                        && d.ToCharacter == recipientName
                        && (d.Status == DispatchStatus.Pending || d.Status == DispatchStatus.Delivered))
            .OrderBy(d => d.CreatedUtc)
            .ToListAsync(ct);
    }
}
