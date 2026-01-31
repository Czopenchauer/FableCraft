using FableCraft.Infrastructure.Persistence.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FableCraft.Infrastructure.Persistence.Configurations;

public class GraphRagSettingsConfiguration : IEntityTypeConfiguration<GraphRagSettings>
{
    public void Configure(EntityTypeBuilder<GraphRagSettings> builder)
    {
        builder.HasIndex(x => x.Name).IsUnique();

        builder.HasMany(x => x.Worldbooks)
            .WithOne(x => x.GraphRagSettings)
            .HasForeignKey(x => x.GraphRagSettingsId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Adventures)
            .WithOne(x => x.GraphRagSettings)
            .HasForeignKey(x => x.GraphRagSettingsId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
