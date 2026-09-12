using System.Security.Cryptography;
using System.Text;
using PersonalJobAgent.Domain.Entities;
using PersonalJobAgent.Domain.Enums;
using JobApplication = PersonalJobAgent.Domain.Entities.Application;

namespace PersonalJobAgent.Infrastructure.Persistence;

public static class DataSeeder
{
    public static async Task SeedInitialCandidateAsync(PersonalJobAgentDbContext dbContext)
    {
        var candidate = dbContext.Candidates.FirstOrDefault();
        if (candidate == null)
        {
            candidate = CreateCandidate();
            dbContext.Candidates.Add(candidate);
        }

        if (!dbContext.Jobs.Any())
        {
            foreach (var job in CreateSampleJobs())
            {
                dbContext.Jobs.Add(job);
            }
        }

        await dbContext.SaveChangesAsync();

        if (!dbContext.Applications.Any())
        {
            var job = dbContext.Jobs
                .OrderByDescending(item => item.SalaryMaxLpa)
                .First();
            var application = new JobApplication(
                candidate.Id,
                job.Id,
                ApplicationStatus.Applied,
                job.JobUrl);
            var applicationEvent = new ApplicationEvent(
                application.Id,
                ApplicationStatus.Unknown,
                ApplicationStatus.Applied,
                "Seeder",
                "Sample application created for dashboard preview.");
            application.RecordEvent(applicationEvent);
            dbContext.Applications.Add(application);
            dbContext.ApplicationEvents.Add(applicationEvent);
            await dbContext.SaveChangesAsync();
        }
    }

    private static Candidate CreateCandidate()
    {
        var candidate = new Candidate(
            name: "Candidate",
            currentRole: "Data Engineer",
            currentCompany: "Current Company",
            totalExperienceYears: 3,
            currentLocation: "Gurugram",
            minimumSalaryLpa: 14,
            targetSalaryLpa: 16,
            noticePeriodMonths: 2,
            isActivelyLooking: true);

        foreach (var skillName in new[] { "Python", "SQL", "PySpark", "Azure Databricks", "Azure Data Factory", "ADLS", "dbt", "Apache Airflow" })
        {
            candidate.AddSkill(new CandidateSkill(
                candidate.Id,
                skillName,
                "Technical",
                3,
                "Intermediate",
                "Stored in candidate profile."));
        }

        return candidate;
    }

    private static IReadOnlyCollection<Job> CreateSampleJobs()
    {
        var jobs = new[]
        {
            ("Atlas Cloud", "Senior Data Engineer", "Gurugram", 16m, 24m, "Python, SQL, PySpark, Azure Databricks"),
            ("Northstar Labs", "Data Platform Engineer", "Remote", 14m, 20m, "Python, Kafka, Airflow, PostgreSQL"),
            ("Riverbank Digital", "Analytics Engineer", "Noida", 12m, 18m, "SQL, dbt, Snowflake, Python"),
            ("Kite Systems", "Data Engineer", "Bengaluru", 18m, 28m, "Python, Spark, AWS, Kubernetes"),
            ("Greenfield AI", "Cloud Data Engineer", "Delhi NCR", 15m, 22m, "Python, Azure, Data Factory, ADLS")
        };

        return jobs.Select((item, index) => new Job(
            $"sample-job-{index + 1}",
            "Sample Feed",
            item.Item1,
            item.Item2,
            $"Build reliable data products with {item.Item6}. Work with engineering and analytics teams to deliver trusted pipelines.",
            item.Item3,
            item.Item4,
            item.Item5,
            $"https://example.com/jobs/sample-{index + 1}",
            DateTime.UtcNow.AddDays(-index),
            ComputeHash(item.Item1 + item.Item2 + item.Item6))).ToList();
    }

    private static string ComputeHash(string value) =>
        Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
