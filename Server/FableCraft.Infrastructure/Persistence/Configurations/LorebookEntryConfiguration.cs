using FableCraft.Infrastructure.Persistence.Entities.Adventure;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FableCraft.Infrastructure.Persistence.Configurations;

public class LorebookEntryConfiguration : IEntityTypeConfiguration<LorebookEntry>
{
    public void Configure(EntityTypeBuilder<LorebookEntry> builder)
    {
        builder.HasOne(x => x.Scene)
            .WithMany(x => x.Lorebooks)
            .HasForeignKey(x => x.SceneId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
