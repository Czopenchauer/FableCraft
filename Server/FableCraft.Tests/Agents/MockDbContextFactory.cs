using FableCraft.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace FableCraft.Tests.Agents;

internal class MockDbContextFactory : IDbContextFactory<ApplicationDbContext>
{
    private readonly DbContextOptions<ApplicationDbContext> _options;

    public MockDbContextFactory(DbContextOptions<ApplicationDbContext> options)
    {
        _options = options;
    }

    public ApplicationDbContext CreateDbContext()
    {
        return new ApplicationDbContext(_options);
    }
}

