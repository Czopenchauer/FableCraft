using System.Text.Json;

using FableCraft.Infrastructure.Persistence.Entities.Adventure;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FableCraft.Infrastructure.Persistence.Configurations;

public class SceneConfiguration : IEntityTypeConfiguration<Scene>
{
    public void Configure(EntityTypeBuilder<Scene> builder)
    {
        var options = JsonExtensions.JsonSerializerOptions;

        builder.Property(c => c.CommitStatus).HasConversion<string>();
        builder.Property(x => x.Metadata).HasConversion<string>(
            x => JsonSerializer.Serialize(x, options),
            x => JsonSerializer.Deserialize<Metadata>(x, options)!);
        builder.HasIndex(x => new { x.AdventureId, x.SequenceNumber }).IsUnique();
        builder.HasIndex(x => new { x.Id, x.SequenceNumber, x.CommitStatus });
    }
}
