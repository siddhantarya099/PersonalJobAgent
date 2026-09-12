using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PersonalJobAgent.Domain.Entities;

namespace PersonalJobAgent.Infrastructure.Persistence.Configurations;

internal sealed class ResumeConfiguration : IEntityTypeConfiguration<Resume>
{
    public void Configure(EntityTypeBuilder<Resume> builder)
    {
        builder.ToTable("resumes");
        builder.HasKey(resume => resume.Id);
        builder.Property(resume => resume.Name).HasMaxLength(200).IsRequired();
        builder.HasIndex(resume => resume.CandidateId).IsUnique();
        builder.HasMany(resume => resume.Versions)
              .WithOne(version => version.Resume)
            .HasForeignKey(version => version.ResumeId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Metadata.FindNavigation(nameof(Resume.Versions))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}