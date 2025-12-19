using FableCraft.Infrastructure.Persistence.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FableCraft.Infrastructure.Persistence.Configurations;

public class LlmLogConfiguration : IEntityTypeConfiguration<LlmLog>
{
    public void Configure(EntityTypeBuilder<LlmLog> builder)
    {
        builder.HasIndex(x => x.AdventureId);
    }
}
