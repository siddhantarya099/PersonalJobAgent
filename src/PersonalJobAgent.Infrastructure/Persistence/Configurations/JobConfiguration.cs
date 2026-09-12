using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PersonalJobAgent.Domain.Entities;

namespace PersonalJobAgent.Infrastructure.Persistence.Configurations;

internal sealed class JobConfiguration : IEntityTypeConfiguration<Job>
{
    public void Configure(EntityTypeBuilder<Job> builder)
    {
        builder.ToTable("jobs");

        builder.HasKey(j => j.Id);

        builder.Property(j => j.ExternalId).HasMaxLength(100);
        builder.Property(j => j.Source).HasMaxLength(100);
        builder.Property(j => j.Company).HasMaxLength(200);
        builder.Property(j => j.Title).HasMaxLength(200).IsRequired();
        builder.Property(j => j.Location).HasMaxLength(200);
        builder.Property(j => j.JobUrl).HasMaxLength(1000);
        builder.Property(j => j.ContentHash).HasMaxLength(200);

        builder.Property(j => j.SalaryMinLpa).HasColumnType("numeric(10,2)");
        builder.Property(j => j.SalaryMaxLpa).HasColumnType("numeric(10,2)");

        builder.HasIndex(j => j.ExternalId);
        builder.HasIndex(j => j.ContentHash);
    }
}
