using System.Text.Json;

using FableCraft.Infrastructure.Persistence.Entities.Adventure;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FableCraft.Infrastructure.Persistence.Configurations;

public class AdventureConfiguration : IEntityTypeConfiguration<Adventure>
{
    public void Configure(EntityTypeBuilder<Adventure> builder)
    {
        var options = JsonExtensions.JsonSerializerOptions;

        builder.Property(c => c.RagProcessingStatus).HasConversion<string>();
        builder.Property(c => c.SceneGenerationStatus).HasConversion<string>();
        builder.Property(x => x.TrackerStructure).HasConversion<string>(
            x => JsonSerializer.Serialize(x, options),
            x => JsonSerializer.Deserialize<TrackerStructure>(x, options)!);
        builder.HasIndex(x => x.Name).IsUnique();
    }
}
