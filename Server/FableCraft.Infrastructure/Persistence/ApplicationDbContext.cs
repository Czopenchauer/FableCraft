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
            p.Property(c => c.ProcessingStatus).HasConversion<string>();
        });

        modelBuilder.Entity<Adventure>(p =>
        {
            p.Property(c => c.ProcessingStatus).HasConversion<string>();
        });

        modelBuilder.Entity<TrackerDefinition>()
            .HasIndex(x => x.Name);

        modelBuilder.Entity<Character>()
            .HasIndex(x => x.Name);

        modelBuilder.Entity<CharacterState>()
            .HasIndex(x => x.SequenceNumber);

        modelBuilder.Entity<Scene>()
            .HasIndex(x => x.SequenceNumber);
    }
}