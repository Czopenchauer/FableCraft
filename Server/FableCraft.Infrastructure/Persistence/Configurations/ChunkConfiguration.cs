using System.Text.Json;

using FableCraft.Infrastructure.Persistence.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FableCraft.Infrastructure.Persistence.Configurations;

public class ChunkConfiguration : IEntityTypeConfiguration<Chunk>
{
    public void Configure(EntityTypeBuilder<Chunk> builder)
    {
        var options = JsonExtensions.JsonSerializerOptions;

        builder.HasIndex(x => x.EntityId);
        builder.HasIndex(x =>
            new
            {
                x.EntityId,
                x.ContentHash
            }).IsUnique();

        builder.Property(x => x.ChunkLocation).HasConversion<string>(
            x => JsonSerializer.Serialize(x, options),
            x => JsonSerializer.Deserialize<List<ChunkLocation>>(x, options)!);
    }
}
