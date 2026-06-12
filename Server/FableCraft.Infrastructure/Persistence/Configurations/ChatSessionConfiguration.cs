using FableCraft.Infrastructure.Persistence.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FableCraft.Infrastructure.Persistence.Configurations;

public class ChatSessionConfiguration : IEntityTypeConfiguration<ChatSession>
{
    public void Configure(EntityTypeBuilder<ChatSession> builder)
    {
        builder.HasIndex(x => x.AdventureId);
        builder.HasOne(x => x.Adventure)
            .WithMany()
            .HasForeignKey(x => x.AdventureId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.LlmPreset)
            .WithMany()
            .HasForeignKey(x => x.LlmPresetId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}