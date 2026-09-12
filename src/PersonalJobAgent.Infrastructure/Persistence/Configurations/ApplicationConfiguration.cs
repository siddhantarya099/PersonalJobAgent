using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using JobApplication = PersonalJobAgent.Domain.Entities.Application;

namespace PersonalJobAgent.Infrastructure.Persistence.Configurations;

internal sealed class ApplicationConfiguration : IEntityTypeConfiguration<JobApplication>
{
    public void Configure(EntityTypeBuilder<JobApplication> builder)
    {
        builder.ToTable("applications");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Status).HasConversion<string>().HasMaxLength(50);
        builder.Property(a => a.ApplicationUrl).HasMaxLength(1000);
        builder.Property(a => a.NextAction).HasMaxLength(500);

        builder.HasMany(a => a.Events)
            .WithOne()
            .HasForeignKey(e => e.ApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata.FindNavigation(nameof(JobApplication.Events))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(a => a.CandidateId);
        builder.HasIndex(a => a.JobId);
    }
}
