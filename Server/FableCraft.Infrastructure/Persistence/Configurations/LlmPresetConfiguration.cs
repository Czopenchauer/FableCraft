using FableCraft.Infrastructure.Persistence.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FableCraft.Infrastructure.Persistence.Configurations;

public class LlmPresetConfiguration : IEntityTypeConfiguration<LlmPreset>
{
    public void Configure(EntityTypeBuilder<LlmPreset> builder)
    {
        builder.HasIndex(x => x.Name).IsUnique();
    }
}
