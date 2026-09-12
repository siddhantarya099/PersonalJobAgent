using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PersonalJobAgent.Domain.Entities;

namespace PersonalJobAgent.Infrastructure.Persistence.Configurations;

internal sealed class ApplicationEventConfiguration : IEntityTypeConfiguration<ApplicationEvent>
{
    public void Configure(EntityTypeBuilder<ApplicationEvent> builder)
    {
        builder.ToTable("application_events");

        builder.HasKey(ae => ae.Id);

        builder.Property(ae => ae.OldStatus).HasConversion<string>().HasMaxLength(50);
        builder.Property(ae => ae.NewStatus).HasConversion<string>().HasMaxLength(50);
        builder.Property(ae => ae.Source).HasMaxLength(100);
        builder.Property(ae => ae.Description).HasMaxLength(1000);

        builder.HasIndex(ae => ae.ApplicationId);
    }
}
