using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PersonalJobAgent.Domain.Entities;

namespace PersonalJobAgent.Infrastructure.Persistence.Configurations;

internal sealed class ResumeVersionConfiguration : IEntityTypeConfiguration<ResumeVersion>
{
    public void Configure(EntityTypeBuilder<ResumeVersion> builder)
    {
        builder.ToTable("resume_versions");
        builder.HasKey(version => version.Id);
        builder.Property(version => version.Content).IsRequired();
        builder.Property(version => version.Source).HasMaxLength(100).IsRequired();
        builder.HasIndex(version => new { version.ResumeId, version.VersionNumber }).IsUnique();
    }
}