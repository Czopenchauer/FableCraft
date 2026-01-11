using System.Text.Json;

using FableCraft.Infrastructure.Persistence.Entities.Adventure;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FableCraft.Infrastructure.Persistence.Configurations;

public class TrackerDefinitionConfiguration : IEntityTypeConfiguration<TrackerDefinition>
{
    public void Configure(EntityTypeBuilder<TrackerDefinition> builder)
    {
        var options = JsonExtensions.JsonSerializerOptions;

        builder.Property(x => x.Structure).HasConversion<string>(
            x => JsonSerializer.Serialize(x, options),
            x => JsonSerializer.Deserialize<TrackerStructure>(x, options)!);
        builder.HasIndex(x => x.Name).IsUnique();
    }
}