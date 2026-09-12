namespace PersonalJobAgent.Application.DTOs;

public sealed class AnalyzeRawJobRequest
{
    public string ExternalId { get; set; } = string.Empty;

    public string Source { get; set; } = string.Empty;

    public string Company { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public decimal? SalaryMinLpa { get; set; }

    public decimal? SalaryMaxLpa { get; set; }

    public string JobUrl { get; set; } = string.Empty;

    public DateTime? PostedAtUtc { get; set; }

    public string JobDescription { get; set; } = string.Empty;
}