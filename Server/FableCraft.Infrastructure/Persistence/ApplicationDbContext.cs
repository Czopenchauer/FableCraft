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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Character>()
            .Property(c => c.ProcessingStatus)
            .HasConversion<string>();

        modelBuilder.Entity<Scene>()
            .Property(s => s.ProcessingStatus)
            .HasConversion<string>();

        modelBuilder.Entity<LorebookEntry>()
            .Property(l => l.ProcessingStatus)
            .HasConversion<string>();
    }
}