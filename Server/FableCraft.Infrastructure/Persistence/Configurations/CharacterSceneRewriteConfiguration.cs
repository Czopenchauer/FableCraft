using System.Text.Json;

using FableCraft.Infrastructure.Persistence.Entities.Adventure;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FableCraft.Infrastructure.Persistence.Configurations;

public class CharacterSceneRewriteConfiguration : IEntityTypeConfiguration<CharacterSceneRewrite>
{
    public void Configure(EntityTypeBuilder<CharacterSceneRewrite> builder)
    {
        var options = JsonExtensions.JsonSerializerOptions;

        builder.HasIndex(x => new { x.CharacterId, x.SequenceNumber });
        builder.Property(x => x.SceneTracker).HasConversion<string>(
            x => JsonSerializer.Serialize(x, options),
            x => JsonSerializer.Deserialize<SceneTracker>(x, options)!);

        builder.HasOne(x => x.Scene)
            .WithMany(x => x.CharacterSceneRewrites)
            .HasForeignKey(x => x.SceneId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}