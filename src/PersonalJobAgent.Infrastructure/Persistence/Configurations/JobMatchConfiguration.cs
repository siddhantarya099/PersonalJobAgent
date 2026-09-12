using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PersonalJobAgent.Domain.Entities;

namespace PersonalJobAgent.Infrastructure.Persistence.Configurations;

internal sealed class JobMatchConfiguration : IEntityTypeConfiguration<JobMatch>
{
    public void Configure(EntityTypeBuilder<JobMatch> builder)
    {
        builder.ToTable("job_matches");

        builder.HasKey(jm => jm.Id);

        builder.Property(jm => jm.OverallScore).HasColumnType("numeric(5,2)");
        builder.Property(jm => jm.TechnicalScore).HasColumnType("numeric(5,2)");
        builder.Property(jm => jm.ExperienceScore).HasColumnType("numeric(5,2)");
        builder.Property(jm => jm.SalaryScore).HasColumnType("numeric(5,2)");
        builder.Property(jm => jm.CompanyScore).HasColumnType("numeric(5,2)");
        builder.Property(jm => jm.LocationScore).HasColumnType("numeric(5,2)");

        builder.Property(jm => jm.Recommendation).HasConversion<string>().HasMaxLength(50);
        
        builder.Property(jm => jm.MatchedSkillsJson).HasColumnType("jsonb");
        builder.Property(jm => jm.MissingSkillsJson).HasColumnType("jsonb");
        builder.Property(jm => jm.MandatoryGapsJson).HasColumnType("jsonb");
        builder.Property(jm => jm.ResumeStrategyJson).HasColumnType("jsonb");

        builder.Property(jm => jm.ModelVersion).HasMaxLength(50);

        builder.HasIndex(jm => jm.JobId);
        builder.HasIndex(jm => jm.CandidateId);
        
        // Ensure a candidate only has one active match per job
        builder.HasIndex(jm => new { jm.JobId, jm.CandidateId }).IsUnique();
    }
}
