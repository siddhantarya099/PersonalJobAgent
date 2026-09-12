using System.Text.RegularExpressions;

namespace PersonalJobAgent.Application.JobMatching.Services;

public sealed partial class SkillCanonicalizer : ISkillCanonicalizer
{
    private sealed record CanonicalSkillDefinition(string CanonicalKey, string DisplayName, IReadOnlyCollection<string> Aliases);

    private readonly Dictionary<string, CanonicalSkillDefinition> _aliasLookup;

    public SkillCanonicalizer()
    {
        _aliasLookup = BuildLookupTable();
    }

    public string GetCanonicalKey(string skill)
    {
        if (string.IsNullOrWhiteSpace(skill))
        {
            return string.Empty;
        }

        var sanitized = Sanitize(skill);
        if (_aliasLookup.TryGetValue(sanitized, out var definition))
        {
            return definition.CanonicalKey;
        }

        return sanitized;
    }

    public string GetCanonicalName(string skill)
    {
        if (string.IsNullOrWhiteSpace(skill))
        {
            return string.Empty;
        }

        var sanitized = Sanitize(skill);
        if (_aliasLookup.TryGetValue(sanitized, out var definition))
        {
            return definition.DisplayName;
        }

        return skill.Trim();
    }

    public bool AreMatching(string skillA, string skillB)
    {
        if (string.IsNullOrWhiteSpace(skillA) || string.IsNullOrWhiteSpace(skillB))
        {
            return false;
        }

        return string.Equals(GetCanonicalKey(skillA), GetCanonicalKey(skillB), StringComparison.Ordinal);
    }

    public IReadOnlySet<string> GetCanonicalKeys(IEnumerable<string> skills)
    {
        return skills
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(GetCanonicalKey)
            .Where(k => !string.IsNullOrEmpty(k))
            .ToHashSet(StringComparer.Ordinal);
    }

    public static string Sanitize(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        return NonAlphaNumericRegex().Replace(input.Trim().ToLowerInvariant(), string.Empty);
    }

    private static Dictionary<string, CanonicalSkillDefinition> BuildLookupTable()
    {
        var definitions = new List<CanonicalSkillDefinition>
        {
            // Orchestration & Pipelines
            new(
                CanonicalKey: "airflow",
                DisplayName: "Apache Airflow",
                Aliases: ["Airflow", "Apache Airflow", "Apache-Airflow", "Cloud Composer", "Composer"]
            ),
            new(
                CanonicalKey: "azure_data_factory",
                DisplayName: "Azure Data Factory",
                Aliases: ["Azure Data Factory", "ADF", "Azure ADF", "Data Factory"]
            ),
            new(
                CanonicalKey: "dbt",
                DisplayName: "dbt",
                Aliases: ["dbt", "data build tool", "dbt core", "dbt cloud"]
            ),
            new(
                CanonicalKey: "nifi",
                DisplayName: "Apache NiFi",
                Aliases: ["NiFi", "Apache NiFi"]
            ),
            new(
                CanonicalKey: "luigi",
                DisplayName: "Luigi",
                Aliases: ["Luigi", "Spotify Luigi"]
            ),
            new(
                CanonicalKey: "prefect",
                DisplayName: "Prefect",
                Aliases: ["Prefect", "Prefect Core", "Prefect Cloud"]
            ),
            new(
                CanonicalKey: "dagster",
                DisplayName: "Dagster",
                Aliases: ["Dagster"]
            ),

            // Big Data, Processing & Streaming
            new(
                CanonicalKey: "pyspark",
                DisplayName: "PySpark",
                Aliases: ["PySpark", "Python Spark", "Python-Spark", "pyspark"]
            ),
            new(
                CanonicalKey: "spark_sql",
                DisplayName: "Spark SQL",
                Aliases: ["Spark SQL", "SparkSQL", "Spark-SQL"]
            ),
            new(
                CanonicalKey: "spark",
                DisplayName: "Apache Spark",
                Aliases: ["Spark", "Apache Spark", "Apache-Spark"]
            ),
            new(
                CanonicalKey: "databricks",
                DisplayName: "Databricks",
                Aliases: ["Databricks", "Azure Databricks", "AWS Databricks", "Databricks Lakehouse"]
            ),
            new(
                CanonicalKey: "kafka",
                DisplayName: "Apache Kafka",
                Aliases: ["Kafka", "Apache Kafka", "Kafka Streams", "Confluent Kafka"]
            ),
            new(
                CanonicalKey: "flink",
                DisplayName: "Apache Flink",
                Aliases: ["Flink", "Apache Flink"]
            ),
            new(
                CanonicalKey: "hadoop",
                DisplayName: "Apache Hadoop",
                Aliases: ["Hadoop", "Apache Hadoop", "HDFS", "MapReduce"]
            ),
            new(
                CanonicalKey: "hive",
                DisplayName: "Apache Hive",
                Aliases: ["Hive", "Apache Hive", "HiveQL"]
            ),
            new(
                CanonicalKey: "delta_lake",
                DisplayName: "Delta Lake",
                Aliases: ["Delta Lake", "Delta Tables", "DeltaLake", "Delta"]
            ),

            // Cloud Platforms & Storage
            new(
                CanonicalKey: "adls",
                DisplayName: "Azure Data Lake Storage",
                Aliases: ["ADLS", "ADLS Gen2", "ADLS Gen 2", "Azure Data Lake Storage", "Azure Data Lake", "Azure Blob Storage", "Azure Storage"]
            ),
            new(
                CanonicalKey: "aws",
                DisplayName: "AWS",
                Aliases: ["AWS", "Amazon Web Services"]
            ),
            new(
                CanonicalKey: "gcp",
                DisplayName: "Google Cloud Platform",
                Aliases: ["GCP", "Google Cloud", "Google Cloud Platform"]
            ),
            new(
                CanonicalKey: "azure",
                DisplayName: "Microsoft Azure",
                Aliases: ["Azure", "Microsoft Azure", "MS Azure"]
            ),
            new(
                CanonicalKey: "s3",
                DisplayName: "Amazon S3",
                Aliases: ["S3", "Amazon S3", "AWS S3", "Simple Storage Service"]
            ),
            new(
                CanonicalKey: "gcs",
                DisplayName: "Google Cloud Storage",
                Aliases: ["GCS", "Google Cloud Storage"]
            ),

            // Data Warehouses & Analytics Engines
            new(
                CanonicalKey: "snowflake",
                DisplayName: "Snowflake",
                Aliases: ["Snowflake", "Snowflake DB", "Snowflake Data Warehouse"]
            ),
            new(
                CanonicalKey: "bigquery",
                DisplayName: "Google BigQuery",
                Aliases: ["BigQuery", "Google BigQuery", "GCP BigQuery"]
            ),
            new(
                CanonicalKey: "redshift",
                DisplayName: "Amazon Redshift",
                Aliases: ["Redshift", "Amazon Redshift", "AWS Redshift"]
            ),
            new(
                CanonicalKey: "azure_synapse",
                DisplayName: "Azure Synapse Analytics",
                Aliases: ["Synapse", "Azure Synapse", "Azure Synapse Analytics", "SQL DW"]
            ),
            new(
                CanonicalKey: "trino_presto",
                DisplayName: "Trino / Presto",
                Aliases: ["Trino", "Presto", "PrestoDB", "PrestoSQL"]
            ),

            // Relational & NoSQL Databases
            new(
                CanonicalKey: "sql",
                DisplayName: "SQL",
                Aliases: ["SQL", "Structured Query Language", "ANSI SQL"]
            ),
            new(
                CanonicalKey: "postgresql",
                DisplayName: "PostgreSQL",
                Aliases: ["PostgreSQL", "Postgres", "Postgres SQL", "PostgreSql", "Postgre"]
            ),
            new(
                CanonicalKey: "mysql",
                DisplayName: "MySQL",
                Aliases: ["MySQL", "My SQL"]
            ),
            new(
                CanonicalKey: "sql_server",
                DisplayName: "Microsoft SQL Server",
                Aliases: ["SQL Server", "MSSQL", "MS SQL", "Microsoft SQL Server", "MS SQL Server", "T-SQL", "TSQL"]
            ),
            new(
                CanonicalKey: "oracle",
                DisplayName: "Oracle Database",
                Aliases: ["Oracle", "Oracle DB", "Oracle Database", "PL/SQL", "PLSQL"]
            ),
            new(
                CanonicalKey: "mongodb",
                DisplayName: "MongoDB",
                Aliases: ["MongoDB", "Mongo", "Mongo DB"]
            ),
            new(
                CanonicalKey: "cassandra",
                DisplayName: "Apache Cassandra",
                Aliases: ["Cassandra", "Apache Cassandra", "CQL"]
            ),
            new(
                CanonicalKey: "redis",
                DisplayName: "Redis",
                Aliases: ["Redis"]
            ),
            new(
                CanonicalKey: "elasticsearch",
                DisplayName: "Elasticsearch",
                Aliases: ["Elasticsearch", "Elastic Search", "ELK", "ELK Stack"]
            ),

            // Programming Languages
            new(
                CanonicalKey: "python",
                DisplayName: "Python",
                Aliases: ["Python", "Python3", "Python 3", "Python 2"]
            ),
            new(
                CanonicalKey: "scala",
                DisplayName: "Scala",
                Aliases: ["Scala"]
            ),
            new(
                CanonicalKey: "java",
                DisplayName: "Java",
                Aliases: ["Java", "Java 8", "Java 11", "Java 17", "Java 21", "Core Java"]
            ),
            new(
                CanonicalKey: "csharp",
                DisplayName: "C#",
                Aliases: ["C#", "CSharp", "C#.NET", "C# .NET", ".NET", "DotNet", "dotnet", "ASP.NET", "ASP.NET Core"]
            ),
            new(
                CanonicalKey: "golang",
                DisplayName: "Go",
                Aliases: ["Go", "Golang"]
            ),
            new(
                CanonicalKey: "r_lang",
                DisplayName: "R",
                Aliases: ["R", "R Programming", "R Language"]
            ),

            // DevOps, Containers & CI/CD
            new(
                CanonicalKey: "docker",
                DisplayName: "Docker",
                Aliases: ["Docker", "Docker Container", "Containers", "Containerization"]
            ),
            new(
                CanonicalKey: "kubernetes",
                DisplayName: "Kubernetes",
                Aliases: ["Kubernetes", "K8s", "k8s"]
            ),
            new(
                CanonicalKey: "git",
                DisplayName: "Git",
                Aliases: ["Git", "GitHub", "GitLab", "Bitbucket"]
            ),
            new(
                CanonicalKey: "cicd",
                DisplayName: "CI/CD",
                Aliases: ["CI/CD", "CICD", "CI-CD", "CI CD", "Continuous Integration", "Continuous Delivery", "Azure DevOps", "ADO", "Jenkins", "GitHub Actions"]
            ),
            new(
                CanonicalKey: "terraform",
                DisplayName: "Terraform",
                Aliases: ["Terraform", "IaC", "Infrastructure as Code", "HCL"]
            ),

            // BI & Visualization
            new(
                CanonicalKey: "power_bi",
                DisplayName: "Power BI",
                Aliases: ["Power BI", "PowerBI", "Power-BI", "MS Power BI", "DAX"]
            ),
            new(
                CanonicalKey: "tableau",
                DisplayName: "Tableau",
                Aliases: ["Tableau", "Tableau Desktop", "Tableau Server"]
            ),
            new(
                CanonicalKey: "looker",
                DisplayName: "Looker",
                Aliases: ["Looker", "Looker Studio", "Google Data Studio"]
            )
        };

        var lookup = new Dictionary<string, CanonicalSkillDefinition>(StringComparer.OrdinalIgnoreCase);

        foreach (var def in definitions)
        {
            // Register canonical key
            lookup[Sanitize(def.CanonicalKey)] = def;
            // Register display name
            lookup[Sanitize(def.DisplayName)] = def;

            // Register all aliases
            foreach (var alias in def.Aliases)
            {
                var sanitizedAlias = Sanitize(alias);
                if (!string.IsNullOrEmpty(sanitizedAlias))
                {
                    lookup[sanitizedAlias] = def;
                }
            }
        }

        return lookup;
    }

    [GeneratedRegex(@"[^a-zA-Z0-9#+]", RegexOptions.Compiled)]
    private static partial Regex NonAlphaNumericRegex();
}

