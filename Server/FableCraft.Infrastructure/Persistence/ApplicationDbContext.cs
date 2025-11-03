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

    public DbSet<Scene> Scenes { get; set; }

    public DbSet<LorebookEntry> LorebookEntries { get; set; }

    public DbSet<CharacterAction> CharacterActions { get; set; }

    public DbSet<ChunkBase> Chunks { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Table Per Hierarchy (TPH) for chunks
        modelBuilder.Entity<ChunkBase>()
            .ToTable("Chunks")
            .HasDiscriminator<string>("ChunkType")
            .HasValue<LorebookEntryChunk>("LorebookEntry")
            .HasValue<CharacterChunk>("Character")
            .HasValue<SceneChunk>("Scene");

        modelBuilder.Entity<ChunkBase>()
            .Property(c => c.ProcessingStatus)
            .HasConversion<string>();

        // Configure relationships for each chunk type
        modelBuilder.Entity<LorebookEntryChunk>()
            .HasOne(c => c.LorebookEntry)
            .WithMany(e => e.Chunks)
            .HasForeignKey(c => c.EntityId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CharacterChunk>()
            .HasOne(c => c.Character)
            .WithMany(e => e.Chunks)
            .HasForeignKey(c => c.EntityId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SceneChunk>()
            .HasOne(c => c.Scene)
            .WithMany(e => e.Chunks)
            .HasForeignKey(c => c.EntityId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}