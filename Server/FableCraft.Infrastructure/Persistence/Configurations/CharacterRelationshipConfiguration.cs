using System.Text.Json;

using FableCraft.Infrastructure.Persistence.Entities.Adventure;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FableCraft.Infrastructure.Persistence.Configurations;

public class CharacterRelationshipConfiguration : IEntityTypeConfiguration<CharacterRelationship>
{
    public void Configure(EntityTypeBuilder<CharacterRelationship> builder)
    {
        var options = JsonExtensions.JsonSerializerOptions;

        builder.HasIndex(x => new { x.CharacterId, x.TargetCharacterName, x.SequenceNumber });
        builder.Property(x => x.StoryTracker).HasConversion<string>(
            x => JsonSerializer.Serialize(x, options),
            x => JsonSerializer.Deserialize<StoryTracker>(x, options)!);

        builder.HasOne(x => x.Scene)
            .WithMany(x => x.CharacterRelationships)
            .HasForeignKey(x => x.SceneId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
