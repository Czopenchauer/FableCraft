using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using ILogger = Serilog.ILogger;

namespace FableCraft.Infrastructure.Persistence;

internal class MigratorApplier : IHostedService
{
    private readonly ILogger _logger;
    private readonly IServiceProvider _serviceProvider;

    public MigratorApplier(ILogger logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.Information("Starting database migration...");
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.MigrateAsync(cancellationToken);
        _logger.Information("Database migration completed.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}