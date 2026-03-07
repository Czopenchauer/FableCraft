using FableCraft.Application.NarrativeEngine.Agents;
using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Serilog;

namespace FableCraft.Application.NarrativeEngine;

public class CoLocationMaintenanceResult
{
    public required Guid SceneId { get; init; }

    public required int SequenceNumber { get; init; }

    public required CoLocatedCharacterResult[] CoLocatedCharacters { get; init; }
}

public class CoLocatedCharacterResult
{
    public required string Name { get; init; }

    public required string Reason { get; init; }
}

public class CoLocationMaintenanceService(
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    IServiceProvider serviceProvider,
    ILogger logger)
{
    /// <summary>
    ///     Populates co-location data for the current (last) scene of an adventure.
    ///     Runs the CoLocationAgent to determine which characters are at the scene location.
    ///     Uses the same context as the enrichment workflow (current scene context).
    /// </summary>
    public async Task<CoLocationMaintenanceResult?> PopulateCoLocationForCurrentSceneAsync(
        Guid adventureId,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var scene = await dbContext.Scenes
            .Where(x => x.AdventureId == adventureId)
            .OrderByDescending(x => x.SequenceNumber)
            .FirstOrDefaultAsync(cancellationToken);

        if (scene is null)
        {
            logger.Warning("CoLocationMaintenance: No scenes found for adventure {AdventureId}", adventureId);
            return null;
        }

        var contextBuilder = serviceProvider.GetRequiredService<IGenerationContextBuilder>();
        var coLocationAgent = serviceProvider.GetRequiredService<CoLocationAgent>();

        // BuildRegenerationContextAsync puts current scene's tracker in NewTracker (used for location)
        // and previous scenes in SceneContext (used for narrative context)
        var context = await contextBuilder.BuildRegenerationContextAsync(adventureId, scene, cancellationToken);

        context.CoLocationOutput = null;

        await coLocationAgent.Invoke(context, cancellationToken);

        if (context.CoLocationOutput is null)
        {
            logger.Information("CoLocationMaintenance: No output produced (no scene location available) for adventure {AdventureId}",
                adventureId);
            return new CoLocationMaintenanceResult
            {
                SceneId = scene.Id,
                SequenceNumber = scene.SequenceNumber,
                CoLocatedCharacters = []
            };
        }

        // Save the co-location output to scene metadata
        scene.Metadata.GatheredContext ??= new GatheredContext();
        scene.Metadata.GatheredContext.CoLocatedCharacters = context.CoLocationOutput.CoLocatedCharacters
            .Select(x => new GatheredCoLocatedCharacter
            {
                Name = x.Name,
                Reason = x.Reason
            }).ToArray();

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.Information("CoLocationMaintenance: Saved {Count} co-located characters for scene {SceneId}",
            context.CoLocationOutput.CoLocatedCharacters.Length, scene.Id);

        return new CoLocationMaintenanceResult
        {
            SceneId = scene.Id,
            SequenceNumber = scene.SequenceNumber,
            CoLocatedCharacters = context.CoLocationOutput.CoLocatedCharacters
                .Select(x => new CoLocatedCharacterResult
                {
                    Name = x.Name,
                    Reason = x.Reason
                }).ToArray()
        };
    }
}
