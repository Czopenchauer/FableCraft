using System.Text.Json;

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

    public DbSet<Character> Characters { get; set; }

    public DbSet<Scene> Scenes { get; set; }

    public DbSet<LorebookEntry> LorebookEntries { get; set; }

    public DbSet<MainCharacterAction> CharacterActions { get; set; }

    public DbSet<Chunk> Chunks { get; set; }

    public DbSet<TrackerDefinition> TrackerDefinitions { get; set; }

    public DbSet<LlmLog> LlmCallLogs { get; set; }

    public DbSet<LlmPreset> LlmPresets { get; set; }

    public DbSet<Worldbook> Worldbooks { get; set; }

    public DbSet<Lorebook> Lorebooks { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true
        };
        modelBuilder.Entity<Chunk>(p =>
        {
            p.HasIndex(x => x.EntityId);
            p.HasIndex(x =>
                new
                {
                    x.EntityId,
                    x.ContentHash
                }).IsUnique();
        });
        
        modelBuilder.Entity<Worldbook>(p =>
        {
            p.HasIndex(x => x.Name).IsUnique();
            p.HasMany(x => x.Lorebooks)
                .WithOne(x => x.Worldbook)
                .HasForeignKey(x => x.WorldbookId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Lorebook>(p =>
        {
            p.HasIndex(x => new { x.WorldbookId, x.Title }).IsUnique();
        });

        modelBuilder.Entity<Adventure>(p =>
        {
            p.Property(c => c.RagProcessingStatus).HasConversion<string>();
            p.Property(c => c.SceneGenerationStatus).HasConversion<string>();
            p.Property(x => x.TrackerStructure).HasConversion<string>(x => JsonSerializer.Serialize(x), x => JsonSerializer.Deserialize<TrackerStructure>(x, options)!);
            p.HasIndex(x => x.Name).IsUnique();

            p.HasOne(x => x.FastPreset)
                .WithMany()
                .HasForeignKey(x => x.FastPresetId)
                .OnDelete(DeleteBehavior.SetNull);

            p.HasOne(x => x.ComplexPreset)
                .WithMany()
                .HasForeignKey(x => x.ComplexPresetId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<LlmPreset>(p =>
        {
            p.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<TrackerDefinition>(p =>
        {
            p.Property(x => x.Structure).HasConversion<string>(x => JsonSerializer.Serialize(x), x => JsonSerializer.Deserialize<TrackerStructure>(x, options)!);
            p.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<Character>(p =>
        {
            p.Property(x => x.CharacterStats).HasConversion<string>(x => JsonSerializer.Serialize(x), x => JsonSerializer.Deserialize<CharacterStats>(x, options)!);
            p.Property(x => x.Tracker).HasConversion<string>(x => JsonSerializer.Serialize(x), x => JsonSerializer.Deserialize<CharacterTracker>(x, options)!);
            p.Property(x => x.DevelopmentTracker).HasConversion<string>(x => JsonSerializer.Serialize(x), x => JsonSerializer.Deserialize<CharacterDevelopmentTracker>(x, options)!);
            p.HasIndex(x => x.SequenceNumber);
        });

        modelBuilder.Entity<Scene>(p =>
        {
            p.Property(c => c.CommitStatus).HasConversion<string>();
            p.Property(x => x.Metadata).HasConversion<string>(x => JsonSerializer.Serialize(x), x => JsonSerializer.Deserialize<Metadata>(x, options)!);
            p.HasIndex(x => new { x.AdventureId, x.SequenceNumber });
            p.HasIndex(x => new { x.Id, x.SequenceNumber, x.CommitStatus });
        });

        modelBuilder.Entity<LlmLog>(p =>
        {
            p.HasIndex(x => x.AdventureId);
        });
    }
}