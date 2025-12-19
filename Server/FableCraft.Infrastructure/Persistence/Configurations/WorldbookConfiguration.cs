using FableCraft.Infrastructure.Persistence.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FableCraft.Infrastructure.Persistence.Configurations;

public class WorldbookConfiguration : IEntityTypeConfiguration<Worldbook>
{
    public void Configure(EntityTypeBuilder<Worldbook> builder)
    {
        builder.HasIndex(x => x.Name).IsUnique();
        builder.HasMany(x => x.Lorebooks)
            .WithOne(x => x.Worldbook)
            .HasForeignKey(x => x.WorldbookId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
