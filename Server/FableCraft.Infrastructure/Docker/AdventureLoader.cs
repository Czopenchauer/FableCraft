using FableCraft.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

using Serilog;

namespace FableCraft.Infrastructure.Docker;

internal sealed class AdventureLoader(IDbContextFactory<ApplicationDbContext> contextFactory, IGraphContainerRegistry containerRegistry, ILogger logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var lastPlayedAdventure = await context.Adventures.Where(x => x.LastPlayedAt != null).OrderByDescending(x => x.LastPlayedAt).FirstOrDefaultAsync(cancellationToken);
        if (lastPlayedAdventure is null)
        {
            logger.Information("No adventures found for preloading");
            return;
        }

        logger.Information("Preloading adventure...");
        await containerRegistry.EnsureAdventureContainerRunningAsync(lastPlayedAdventure.Id, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}