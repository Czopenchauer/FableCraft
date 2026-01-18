using FableCraft.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

using Testcontainers.PostgreSql;

using TUnit.Core.Interfaces;

namespace FableCraft.Tests.Integration.SceneRegeneration.Fixtures;

public class PostgresContainerFixture : IAsyncInitializer, IAsyncDisposable
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("fablecraft_test")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        await using var context = CreateDbContext();
        await context.Database.MigrateAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    public ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        return new ApplicationDbContext(options);
    }

    public async Task ResetDatabaseAsync()
    {
        await using var context = CreateDbContext();

        // Delete all data from tables instead of dropping/recreating
        await context.Database.ExecuteSqlRawAsync(@"
            TRUNCATE TABLE
                ""CharacterEvents"",
                ""CharacterSceneRewrites"",
                ""CharacterRelationships"",
                ""CharacterMemories"",
                ""CharacterStates"",
                ""LlmCallLogs"",
                ""LorebookEntries"",
                ""CharacterActions"",
                ""Scenes"",
                ""GenerationProcesses"",
                ""Characters"",
                ""MainCharacters"",
                ""AdventureAgentLlmPresets"",
                ""Lorebooks"",
                ""Adventures"",
                ""Chunks"",
                ""TrackerDefinitions"",
                ""LlmPresets"",
                ""Worldbooks""
            CASCADE;
        ");
    }
}
