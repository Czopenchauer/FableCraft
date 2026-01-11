using FableCraft.Infrastructure.Persistence.Entities.Adventure;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FableCraft.Infrastructure.Persistence.Configurations;

public class AdventureAgentLlmPresetConfiguration : IEntityTypeConfiguration<AdventureAgentLlmPreset>
{
    public void Configure(EntityTypeBuilder<AdventureAgentLlmPreset> builder)
    {
        builder.Property(x => x.AgentName).HasConversion<string>();

        builder.HasOne(x => x.Adventure)
            .WithMany(x => x.AgentLlmPresets)
            .HasForeignKey(x => x.AdventureId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.LlmPreset)
            .WithMany()
            .HasForeignKey(x => x.LlmPresetId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.AdventureId, x.AgentName }).IsUnique();
    }
}