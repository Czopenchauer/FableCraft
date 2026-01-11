using FableCraft.Infrastructure.Persistence.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FableCraft.Infrastructure.Persistence.Configurations;

public class LorebookConfiguration : IEntityTypeConfiguration<Lorebook>
{
    public void Configure(EntityTypeBuilder<Lorebook> builder)
    {
        builder.HasIndex(x => new { x.WorldbookId, x.Title }).IsUnique();
        builder.Property(c => c.ContentType).HasConversion<string>();
    }
}