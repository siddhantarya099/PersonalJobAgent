using PersonalJobAgent.Application.JobMatching.Models;
using PersonalJobAgent.Application.JobMatching.Services;

namespace PersonalJobAgent.UnitTests.JobMatching;

public sealed class JobMatchingEngineTests
{
    private readonly JobMatchingEngine _engine = new();

    private static CandidateProfile CreateCandidate(
        decimal experience = 3,
        decimal minimumSalary = 14,
        decimal targetSalary = 16)
    {
        return new CandidateProfile
        {
            ExperienceYears = experience,
            MinimumSalaryLpa = minimumSalary,
            TargetSalaryLpa = targetSalary,
            CurrentLocation = "Gurugram",
            OpenToRemote = true,
            OpenToRelocation = false,
            NoticePeriodMonths = 2,
            Skills =
            [
                "Python",
                "SQL",
                "PySpark",
                "Spark SQL",
                "Azure Databricks",
                "Azure Data Factory",
                "ADLS",
                "dbt",
                "Apache Airflow"
            ]
        };
    }

    [Fact]
    public void StronglyMatchingJob_ShouldBeRecommended()
    {
        var candidate = CreateCandidate();

        var job = new JobProfile
        {
            Title = "Data Engineer",
            Company = "Example",
            Location = "Gurugram",
            SalaryMinLpa = 16,
            SalaryMaxLpa = 20,
            MinimumExperienceYears = 3,
            MaximumExperienceYears = 5,
            RequiredSkills =
            [
                "Python",
                "SQL",
                "PySpark",
                "Azure Databricks"
            ],
            PreferredSkills =
            [
                "dbt"
            ],
            MandatorySkills =
            [
                "Python",
                "SQL"
            ]
        };

        var result = _engine.Evaluate(candidate, job);

        Assert.True(result.OverallScore >= 85);
        Assert.Equal(
            "STRONGLY_RECOMMENDED",
            result.Recommendation);
        Assert.Empty(result.MandatoryGaps);
    }

    [Fact]
    public void JobWithSkillAliases_ShouldMatchAllCanonicalSkills()
    {
        // Candidate has: Apache Airflow, Azure Data Factory, Azure Databricks, ADLS, dbt, Python, SQL, PySpark, Spark SQL
        var candidate = CreateCandidate();

        // Job lists aliases: Airflow, ADF, Databricks, Azure Data Lake Storage
        var job = new JobProfile
        {
            Title = "Data Engineer",
            Company = "Tech Corp",
            Location = "Gurugram",
            SalaryMinLpa = 16,
            SalaryMaxLpa = 22,
            MinimumExperienceYears = 3,
            MaximumExperienceYears = 5,
            RequiredSkills =
            [
                "Python",
                "SQL",
                "Airflow",
                "ADF",
                "Databricks",
                "Azure Data Lake Storage"
            ],
            PreferredSkills =
            [
                "dbt",
                "Cloud Composer"
            ],
            MandatorySkills =
            [
                "Python",
                "SQL",
                "Airflow"
            ]
        };

        var result = _engine.Evaluate(candidate, job);

        Assert.Equal(100m, result.TechnicalScore);
        Assert.Empty(result.MissingSkills);
        Assert.Empty(result.MandatoryGaps);
        Assert.Equal(6, result.MatchedSkills.Count);
        Assert.Equal("STRONGLY_RECOMMENDED", result.Recommendation);
    }

    [Fact]
    public void CaseAndFormattingVariations_ShouldMatchSuccessfully()
    {
        var candidate = new CandidateProfile
        {
            ExperienceYears = 3,
            MinimumSalaryLpa = 14,
            TargetSalaryLpa = 16,
            CurrentLocation = "Gurugram",
            OpenToRemote = true,
            Skills =
            [
                "pyspark",
                "spark sql",
                "azure data factory",
                "apache airflow",
                "k8s"
            ]
        };

        var job = new JobProfile
        {
            Title = "Data Engineer",
            Company = "Example",
            Location = "Gurugram",
            RequiredSkills =
            [
                "Python-Spark",
                "SparkSQL",
                "Azure ADF",
                "Airflow",
                "Kubernetes"
            ]
        };

        var result = _engine.Evaluate(candidate, job);

        Assert.Equal(100m, result.TechnicalScore);
        Assert.Equal(5, result.MatchedSkills.Count);
        Assert.Empty(result.MissingSkills);
    }

    [Fact]
    public void MissingMandatorySkill_ShouldCreateHardBlocker()
    {
        var candidate = CreateCandidate();

        var job = new JobProfile
        {
            Title = "Data Engineer",
            Company = "Example",
            Location = "Gurugram",
            MinimumExperienceYears = 3,
            RequiredSkills =
            [
                "Python",
                "SQL",
                "Kafka"
            ],
            MandatorySkills =
            [
                "Kafka"
            ]
        };

        var result = _engine.Evaluate(candidate, job);

        Assert.True(result.HasHardBlocker);
        Assert.Contains(
            "Kafka",
            result.MandatoryGaps);
    }

    [Fact]
    public void LargeExperienceGap_ShouldBeLowPriority()
    {
        var candidate = CreateCandidate(experience: 3);

        var job = new JobProfile
        {
            Title = "Senior Data Engineer",
            Company = "Example",
            Location = "Gurugram",
            MinimumExperienceYears = 6,
            RequiredSkills =
            [
                "Python",
                "SQL"
            ]
        };

        var result = _engine.Evaluate(candidate, job);

        Assert.True(result.HasHardBlocker);
        Assert.Equal(
            "LOW_PRIORITY",
            result.Recommendation);
    }

    [Fact]
    public void JobBelowMinimumSalary_ShouldHaveLowSalaryScore()
    {
        var candidate = CreateCandidate();

        var job = new JobProfile
        {
            Title = "Data Engineer",
            Company = "Example",
            Location = "Gurugram",
            SalaryMinLpa = 10,
            SalaryMaxLpa = 12,
            MinimumExperienceYears = 3,
            RequiredSkills =
            [
                "Python",
                "SQL"
            ]
        };

        var result = _engine.Evaluate(candidate, job);

        Assert.True(result.SalaryScore < 50);
    }

    [Fact]
    public void DelhiNcrJob_ShouldMatchLocation()
    {
        var candidate = CreateCandidate();

        var job = new JobProfile
        {
            Title = "Data Engineer",
            Company = "Example",
            Location = "Noida",
            RequiredSkills =
            [
                "Python"
            ]
        };

        var result = _engine.Evaluate(candidate, job);

        Assert.Equal(100, result.LocationScore);
    }

    [Fact]
    public void RemoteJob_WhenCandidateOpenToRemote_ShouldScoreFullLocationScore()
    {
        var candidate = CreateCandidate(); // OpenToRemote = true, Location = Gurugram

        var job = new JobProfile
        {
            Title = "Data Engineer",
            Company = "Remote Corp",
            Location = "Bangalore",
            RemoteAvailable = true,
            RequiredSkills = ["Python"]
        };

        var result = _engine.Evaluate(candidate, job);

        Assert.Equal(100, result.LocationScore);
        Assert.False(result.HasHardBlocker);
    }
}
