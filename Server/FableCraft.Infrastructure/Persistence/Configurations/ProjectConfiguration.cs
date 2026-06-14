using FableCraft.Infrastructure.Persistence.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FableCraft.Infrastructure.Persistence.Configurations;

public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.HasIndex(x => x.Name);

        builder.Property(x => x.IndexingStatus)
            .HasConversion<string>();

        builder.HasOne(x => x.GraphRagSettings)
            .WithMany()
            .HasForeignKey(x => x.GraphRagSettingsId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.LlmPreset)
            .WithMany()
            .HasForeignKey(x => x.LlmPresetId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}