using PersonalJobAgent.Domain.Common;

namespace PersonalJobAgent.Domain.Entities;

public sealed class Job : BaseEntity
{
    public string ExternalId { get; private set; } = string.Empty;

    public string Source { get; private set; } = string.Empty;

    public string Company { get; private set; } = string.Empty;

    public string Title { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public string Location { get; private set; } = string.Empty;

    public decimal? SalaryMinLpa { get; private set; }

    public decimal? SalaryMaxLpa { get; private set; }

    public string JobUrl { get; private set; } = string.Empty;

    public DateTime? PostedAtUtc { get; private set; }

    public string ContentHash { get; private set; } = string.Empty;

    private Job()
    {
    }

    public Job(
        string externalId,
        string source,
        string company,
        string title,
        string description,
        string location,
        decimal? salaryMinLpa,
        decimal? salaryMaxLpa,
        string jobUrl,
        DateTime? postedAtUtc,
        string contentHash)
    {
        ExternalId = externalId;
        Source = source;
        Company = company;
        Title = title;
        Description = description;
        Location = location;
        SalaryMinLpa = salaryMinLpa;
        SalaryMaxLpa = salaryMaxLpa;
        JobUrl = jobUrl;
        PostedAtUtc = postedAtUtc;
        ContentHash = contentHash;
    }
}