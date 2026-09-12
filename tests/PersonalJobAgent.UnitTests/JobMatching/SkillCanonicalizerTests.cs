using PersonalJobAgent.Application.JobMatching.Services;

namespace PersonalJobAgent.UnitTests.JobMatching;

public sealed class SkillCanonicalizerTests
{
    private readonly SkillCanonicalizer _canonicalizer = new();

    [Theory]
    [InlineData("Apache Airflow", "Airflow")]
    [InlineData("Airflow", "Apache-Airflow")]
    [InlineData("Cloud Composer", "Airflow")]
    [InlineData("Azure Data Factory", "ADF")]
    [InlineData("Azure ADF", "ADF")]
    [InlineData("Azure Data Factory", "Data Factory")]
    [InlineData("Azure Databricks", "Databricks")]
    [InlineData("AWS Databricks", "Databricks")]
    [InlineData("ADLS", "Azure Data Lake Storage")]
    [InlineData("ADLS Gen2", "ADLS")]
    [InlineData("dbt", "data build tool")]
    [InlineData("dbt Core", "dbt")]
    [InlineData("PySpark", "Python Spark")]
    [InlineData("PySpark", "pyspark")]
    [InlineData("Spark SQL", "SparkSQL")]
    [InlineData("Apache Kafka", "Kafka")]
    [InlineData("PostgreSQL", "Postgres")]
    [InlineData("Postgres SQL", "PostgreSQL")]
    [InlineData("SQL Server", "MSSQL")]
    [InlineData("Microsoft SQL Server", "SQL Server")]
    [InlineData("AWS", "Amazon Web Services")]
    [InlineData("GCP", "Google Cloud Platform")]
    [InlineData("Google Cloud", "GCP")]
    [InlineData("Snowflake", "Snowflake DB")]
    [InlineData("BigQuery", "Google BigQuery")]
    [InlineData("Azure Synapse", "Synapse")]
    [InlineData("Power BI", "PowerBI")]
    [InlineData("Docker", "Containerization")]
    [InlineData("Kubernetes", "K8s")]
    [InlineData("CI/CD", "CICD")]
    [InlineData("C#", ".NET")]
    [InlineData("Golang", "Go")]
    public void AreMatching_KnownAliases_ShouldReturnTrue(string skillA, string skillB)
    {
        var matches = _canonicalizer.AreMatching(skillA, skillB);

        Assert.True(matches, $"Expected '{skillA}' and '{skillB}' to match as aliases.");
    }

    [Theory]
    [InlineData("Python", "Java")]
    [InlineData("Kafka", "Airflow")]
    [InlineData("SQL", "NoSQL")]
    [InlineData("Docker", "Kubernetes")]
    [InlineData("Azure Data Factory", "Azure Databricks")]
    public void AreMatching_DifferentSkills_ShouldReturnFalse(string skillA, string skillB)
    {
        var matches = _canonicalizer.AreMatching(skillA, skillB);

        Assert.False(matches, $"Expected '{skillA}' and '{skillB}' NOT to match.");
    }

    [Theory]
    [InlineData("CustomTool-V1", "custom tool v1")]
    [InlineData("Some_Library", "Some Library")]
    [InlineData("SpecialFramework.Net", "SpecialFrameworkNet")]
    public void AreMatching_UnknownSkills_ShouldMatchOnSanitizedNames(string skillA, string skillB)
    {
        var matches = _canonicalizer.AreMatching(skillA, skillB);

        Assert.True(matches, $"Expected unknown skills '{skillA}' and '{skillB}' to match via sanitized fallback.");
    }

    [Theory]
    [InlineData("ADF", "Azure Data Factory")]
    [InlineData("Airflow", "Apache Airflow")]
    [InlineData("pyspark", "PySpark")]
    [InlineData("Postgres", "PostgreSQL")]
    [InlineData("K8s", "Kubernetes")]
    public void GetCanonicalName_ShouldReturnDisplayName(string input, string expectedDisplayName)
    {
        var displayName = _canonicalizer.GetCanonicalName(input);

        Assert.Equal(expectedDisplayName, displayName);
    }

    [Fact]
    public void GetCanonicalKeys_ShouldReturnDeduplicatedSet()
    {
        var skills = new[]
        {
            "Apache Airflow",
            "Airflow",
            "ADF",
            "Azure Data Factory",
            "Python",
            "Python 3"
        };

        var keys = _canonicalizer.GetCanonicalKeys(skills);

        // Expected 3 unique technologies: Airflow, ADF, Python
        Assert.Equal(3, keys.Count);
        Assert.Contains("airflow", keys);
        Assert.Contains("azure_data_factory", keys);
        Assert.Contains("python", keys);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetCanonicalKey_NullOrEmpty_ReturnsEmpty(string? skill)
    {
        var key = _canonicalizer.GetCanonicalKey(skill!);

        Assert.Equal(string.Empty, key);
    }
}

