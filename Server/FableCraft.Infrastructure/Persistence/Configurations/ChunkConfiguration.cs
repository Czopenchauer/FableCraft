using FableCraft.Infrastructure.Persistence.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FableCraft.Infrastructure.Persistence.Configurations;

public class ChunkConfiguration : IEntityTypeConfiguration<Chunk>
{
    public void Configure(EntityTypeBuilder<Chunk> builder)
    {
        builder.HasIndex(x => x.EntityId);
        builder.HasIndex(x =>
            new
            {
                x.EntityId,
                x.ContentHash,
                x.DatasetName
            }).IsUnique();
    }
}