using FableCraft.Infrastructure.Persistence.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FableCraft.Infrastructure.Persistence.Configurations;

public class ProjectFileConfiguration : IEntityTypeConfiguration<ProjectFile>
{
    public void Configure(EntityTypeBuilder<ProjectFile> builder)
    {
        builder.HasIndex(x => x.ProjectId);

        builder.HasOne(x => x.Project)
            .WithMany(p => p.Files)
            .HasForeignKey(x => x.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}