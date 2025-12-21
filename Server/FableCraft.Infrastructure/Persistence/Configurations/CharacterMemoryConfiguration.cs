using System.Text.Json;

using FableCraft.Infrastructure.Persistence.Entities.Adventure;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FableCraft.Infrastructure.Persistence.Configurations;

public class CharacterMemoryConfiguration : IEntityTypeConfiguration<CharacterMemory>
{
    public void Configure(EntityTypeBuilder<CharacterMemory> builder)
    {
        var options = JsonExtensions.JsonSerializerOptions;

        builder.HasIndex(x => new { x.CharacterId });
        builder.HasIndex(x => new { x.CharacterId, x.Salience });
        builder.Property(x => x.StoryTracker).HasConversion<string>(
            x => JsonSerializer.Serialize(x, options),
            x => JsonSerializer.Deserialize<StoryTracker>(x, options)!);
        
        builder.Property(x => x.Data).HasConversion<string>(
            x => JsonSerializer.Serialize(x, options),
            x => JsonSerializer.Deserialize<IDictionary<string, object>>(x, options)!);

        builder.HasOne(x => x.Scene)
            .WithMany(x => x.CharacterMemories)
            .HasForeignKey(x => x.SceneId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
