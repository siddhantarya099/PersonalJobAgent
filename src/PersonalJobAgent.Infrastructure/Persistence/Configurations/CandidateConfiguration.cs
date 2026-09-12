using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PersonalJobAgent.Domain.Entities;

namespace PersonalJobAgent.Infrastructure.Persistence.Configurations;

internal sealed class CandidateConfiguration : IEntityTypeConfiguration<Candidate>
{
    public void Configure(EntityTypeBuilder<Candidate> builder)
    {
        builder.ToTable("candidates");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name).HasMaxLength(200).IsRequired();
        builder.Property(c => c.CurrentRole).HasMaxLength(200);
        builder.Property(c => c.CurrentCompany).HasMaxLength(200);
        builder.Property(c => c.CurrentLocation).HasMaxLength(200);

        builder.Property(c => c.TotalExperienceYears).HasColumnType("numeric(5,2)");
        builder.Property(c => c.MinimumSalaryLpa).HasColumnType("numeric(10,2)");
        builder.Property(c => c.TargetSalaryLpa).HasColumnType("numeric(10,2)");

        builder.HasMany(c => c.Skills)
            .WithOne()
            .HasForeignKey(s => s.CandidateId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.Metadata.FindNavigation(nameof(Candidate.Skills))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
