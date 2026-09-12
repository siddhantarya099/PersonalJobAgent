using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PersonalJobAgent.Domain.Entities;

namespace PersonalJobAgent.Infrastructure.Persistence.Configurations;

internal sealed class CandidateSkillConfiguration : IEntityTypeConfiguration<CandidateSkill>
{
    public void Configure(EntityTypeBuilder<CandidateSkill> builder)
    {
        builder.ToTable("candidate_skills");

        builder.HasKey(cs => cs.Id);

        builder.Property(cs => cs.Name).HasMaxLength(100).IsRequired();
        builder.Property(cs => cs.Category).HasMaxLength(100);
        builder.Property(cs => cs.Proficiency).HasMaxLength(50);
        builder.Property(cs => cs.Evidence).HasMaxLength(1000);

        builder.Property(cs => cs.YearsOfExperience).HasColumnType("numeric(5,2)");

        builder.HasIndex(cs => cs.CandidateId);
    }
}
