using FableCraft.Infrastructure.Persistence.Entities.Adventure;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FableCraft.Infrastructure.Persistence.Configurations;

public class CharacterConfiguration : IEntityTypeConfiguration<Character>
{
    public void Configure(EntityTypeBuilder<Character> builder)
    {
        builder.Property(x => x.Importance)
            .HasConversion(
                v => v.Value,
                v => CharacterImportanceConverter.FromString(v))
            .HasMaxLength(50);

        builder.HasMany(x => x.CharacterStates)
            .WithOne()
            .HasForeignKey(x => x.CharacterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.CharacterMemories)
            .WithOne()
            .HasForeignKey(x => x.CharacterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.CharacterRelationships)
            .WithOne()
            .HasForeignKey(x => x.CharacterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.CharacterSceneRewrites)
            .WithOne()
            .HasForeignKey(x => x.CharacterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.AdventureId);
    }
}
