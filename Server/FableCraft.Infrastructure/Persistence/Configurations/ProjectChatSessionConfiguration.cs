using FableCraft.Infrastructure.Persistence.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FableCraft.Infrastructure.Persistence.Configurations;

public class ProjectChatSessionConfiguration : IEntityTypeConfiguration<ProjectChatSession>
{
    public void Configure(EntityTypeBuilder<ProjectChatSession> builder)
    {
        builder.HasIndex(x => x.ProjectId);

        builder.HasOne(x => x.Project)
            .WithMany(p => p.ChatSessions)
            .HasForeignKey(x => x.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.LlmPreset)
            .WithMany()
            .HasForeignKey(x => x.LlmPresetId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}