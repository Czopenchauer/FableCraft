using FableCraft.Infrastructure.Persistence.Entities;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;

using Microsoft.EntityFrameworkCore;

namespace FableCraft.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Adventure> Adventures { get; set; }

    public DbSet<GenerationProcess> GenerationProcesses { get; set; }

    public DbSet<Scene> Scenes { get; set; }

    public DbSet<LorebookEntry> LorebookEntries { get; set; }

    public DbSet<MainCharacterAction> CharacterActions { get; set; }

    public DbSet<MainCharacter> MainCharacters { get; set; }

    public DbSet<Chunk> Chunks { get; set; }

    public DbSet<TrackerDefinition> TrackerDefinitions { get; set; }

    public DbSet<LlmLog> LlmCallLogs { get; set; }

    public DbSet<LlmPreset> LlmPresets { get; set; }

    public DbSet<Worldbook> Worldbooks { get; set; }

    public DbSet<Lorebook> Lorebooks { get; set; }

    public DbSet<AdventureAgentLlmPreset> AdventureAgentLlmPresets { get; set; }

    public DbSet<Character> Characters { get; set; }

    public DbSet<CharacterState> CharacterStates { get; set; }

    public DbSet<CharacterMemory> CharacterMemories { get; set; }

    public DbSet<CharacterRelationship> CharacterRelationships { get; set; }

    public DbSet<CharacterSceneRewrite> CharacterSceneRewrites { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}