using System.Text.Json;

using FableCraft.Infrastructure.Persistence.Entities.Adventure;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FableCraft.Infrastructure.Persistence.Configurations;

public class CharacterStateConfiguration : IEntityTypeConfiguration<CharacterState>
{
    public void Configure(EntityTypeBuilder<CharacterState> builder)
    {
        var options = JsonExtensions.JsonSerializerOptions;

        builder.Property(x => x.CharacterStats).HasConversion<string>(
            x => JsonSerializer.Serialize(x, options),
            x => JsonSerializer.Deserialize<CharacterStats>(x, options)!);
        builder.Property(x => x.Tracker).HasConversion<string>(
            x => JsonSerializer.Serialize(x, options),
            x => JsonSerializer.Deserialize<CharacterTracker>(x, options)!);
        builder.Property(x => x.SimulationMetadata).HasConversion<string>(
            x => JsonSerializer.Serialize(x, options),
            x => JsonSerializer.Deserialize<SimulationMetadata>(x, options));
        builder.HasIndex(x => x.SequenceNumber);
        builder.HasIndex(x => new { x.CharacterId, x.SequenceNumber });

        builder.HasOne(x => x.Scene)
            .WithMany(x => x.CharacterStates)
            .HasForeignKey(x => x.SceneId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
