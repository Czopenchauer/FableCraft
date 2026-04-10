using FableCraft.Infrastructure.Persistence.Entities.Adventure;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FableCraft.Infrastructure.Persistence.Configurations;

public class SceneImageConfiguration : IEntityTypeConfiguration<SceneImage>
{
    public void Configure(EntityTypeBuilder<SceneImage> builder)
    {
        builder.Property(x => x.Status).HasConversion<string>();

        // Ensure unique version per scene
        builder.HasIndex(x => new { x.SceneId, x.Version }).IsUnique();

        // Index for finding selected image quickly
        builder.HasIndex(x => new { x.SceneId, x.IsSelected });

        // Configure relationship
        builder.HasOne(x => x.Scene)
            .WithMany(s => s.Images)
            .HasForeignKey(x => x.SceneId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
