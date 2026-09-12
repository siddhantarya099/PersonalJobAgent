using System.Reflection;
using Microsoft.EntityFrameworkCore;
using PersonalJobAgent.Domain.Entities;
using JobApplication = PersonalJobAgent.Domain.Entities.Application;

namespace PersonalJobAgent.Infrastructure.Persistence;

public class PersonalJobAgentDbContext : DbContext
{
    public DbSet<Candidate> Candidates => Set<Candidate>();
    public DbSet<CandidateSkill> CandidateSkills => Set<CandidateSkill>();
    public DbSet<Job> Jobs => Set<Job>();
    public DbSet<JobMatch> JobMatches => Set<JobMatch>();
    public DbSet<JobApplication> Applications => Set<JobApplication>();
    public DbSet<ApplicationEvent> ApplicationEvents => Set<ApplicationEvent>();
    public DbSet<Resume> Resumes => Set<Resume>();
    public DbSet<ResumeVersion> ResumeVersions => Set<ResumeVersion>();

    public PersonalJobAgentDbContext(DbContextOptions<PersonalJobAgentDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(modelBuilder);
    }
}
