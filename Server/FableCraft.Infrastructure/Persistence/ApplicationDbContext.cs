using System.Text.Json;

using FableCraft.Infrastructure.Persistence.Entities;

using Microsoft.EntityFrameworkCore;

namespace FableCraft.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Adventure> Adventures { get; set; }

    public DbSet<Character> Characters { get; set; }

    public DbSet<CharacterState> CharacterStates { get; set; }

    public DbSet<Scene> Scenes { get; set; }

    public DbSet<LorebookEntry> LorebookEntries { get; set; }

    public DbSet<MainCharacterAction> CharacterActions { get; set; }

    public DbSet<Chunk> Chunks { get; set; }

    public DbSet<TrackerDefinition> TrackerDefinitions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Chunk>(p =>
        {
            p.HasIndex(x => x.EntityId);
        });

        modelBuilder.Entity<Adventure>(p =>
        {
            p.Property(c => c.ProcessingStatus).HasConversion<string>();
            p.Property(x => x.TrackerStructure).HasConversion<string>(x => JsonSerializer.Serialize(x), x => JsonSerializer.Deserialize<TrackerStructure>(x)!);
            p.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<TrackerDefinition>(p =>
        {
            p.Property(x => x.Structure).HasConversion<string>(x => JsonSerializer.Serialize(x), x => JsonSerializer.Deserialize<TrackerStructure>(x)!);
            p.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<Character>()
            .HasIndex(x => x.Name);

        modelBuilder.Entity<Chunk>()
            .HasIndex(x =>
                new
                {
                    x.EntityId,
                    x.ContentHash
                })
            .IsUnique();

        modelBuilder.Entity<CharacterState>(p =>
        {
            p.ComplexProperty(c => c.CharacterStats, d => d.ToJson());
            p.Property(x => x.Tracker).HasConversion<string>(x => JsonSerializer.Serialize(x), x => JsonSerializer.Deserialize<CharacterTracker>(x)!).Metadata
                .SetProviderClrType(null);
            p.HasIndex(x => x.SequenceNumber);
        });

        modelBuilder.Entity<Scene>(p =>
        {
            p.Property(c => c.CommitStatus).HasConversion<string>();
            p.Property(x => x.Metadata).HasConversion<string>(x => JsonSerializer.Serialize(x), x => JsonSerializer.Deserialize<Metadata>(x)!).Metadata
                .SetProviderClrType(null);
            p.HasIndex(x => new { x.AdventureId, x.SequenceNumber });
            p.HasIndex(x => new { x.Id, x.SequenceNumber, x.CommitStatus });
        });
    }
}